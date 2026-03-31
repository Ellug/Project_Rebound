using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

// 월간 캘린더 팝업 UI 컨트롤러
public class CalendarMonthView : UIBase
{
    [Header("헤더")]
    [SerializeField] private TMP_Text _txtYear;
    [SerializeField] private TMP_Text _txtMonth;
    [SerializeField] private Button _btnPrevMonth;
    [SerializeField] private Button _btnNextMonth;
    [SerializeField] private Button _btnClose;

    [Header("셀 풀")]
    [SerializeField] private Transform _gridRoot;
    [SerializeField] private CalendarCell _cellPrefab;

    [Header("연출")]
    [SerializeField] private PopupAnimator _popAnimator;   // 팝업 등장/퇴장 (Pop 또는 Slide 타입)
    [SerializeField] private PopupAnimator _slideAnimator; // 월 전환 슬라이드 (Swipe 타입)
    [SerializeField] private CalendarSwipeHandler _swipeHandler;
    [SerializeField] private float _slideOffsetX = 600f;
    [SerializeField] private float _slideDuration = 0.2f;

    [Header("날짜 팝업")]
    [SerializeField] private CalendarDayPopup _dayPopup;

    private readonly List<CalendarCell> _cells = new();

    private int _viewYear;
    private int _viewMonth;
    private bool _isAnimating;

    private float _defaultSlideX;
    private bool _slideBasePosCached;

    private static readonly DateTime MinViewDate = new DateTime(2026, 3, 1);
    private static readonly DateTime MaxViewDate = CalendarManager.MaxDate;

    public override void Init()
    {
        base.Init();

        _btnPrevMonth.onClick.AddListener(OnPrevMonth);
        _btnNextMonth.onClick.AddListener(OnNextMonth);

        if (_btnClose != null)
            _btnClose.onClick.AddListener(OnCloseClicked);

        if (_swipeHandler != null)
        {
            _swipeHandler.OnSwipeLeft += OnNextMonth;
            _swipeHandler.OnSwipeRight += OnPrevMonth;
        }
        CalendarDayDetailPopup.Bind(_dayPopup);
        BuildCellPool();
    }

    // 팝업 등장 — Pop 애니메이션 후 달력 표시
    public override void Open()
    {
        base.Open();

        if (CalendarManager.Instance != null)
            CalendarManager.Instance.OnCalendarChanged += Refresh;

        TurnManager tm = FindFirstObjectByType<TurnManager>();
        if (tm != null)
        {
            _viewYear = tm.DateManager.CurrentDate.Year;
            _viewMonth = tm.DateManager.CurrentDate.Month;
        }
        else
        {
            _viewYear = DateTime.Today.Year;
            _viewMonth = DateTime.Today.Month;
        }

        if (_slideAnimator != null && !_slideBasePosCached)
        {
            _defaultSlideX = _slideAnimator.GetPositionX();
            _slideBasePosCached = true;
        }

        Refresh();

        if (_slideAnimator != null)
            _slideAnimator.SetPositionX(_defaultSlideX);

        if (_popAnimator != null)
            _popAnimator.PlayIn();
    }

    // 팝업 퇴장 — Pop 애니메이션 후 닫기
    public override void Close()
    {
        if (CalendarManager.Instance != null)
            CalendarManager.Instance.OnCalendarChanged -= Refresh;

        if (_popAnimator != null)
            _popAnimator.PlayOut(() => base.Close());
        else
            base.Close();
    }

    private void OnCloseClicked()
    {
        Close();
    }

    // 이전 달로 이동 (최소 MinViewDate까지)
    private void OnPrevMonth()
    {
        var prev = new DateTime(_viewYear, _viewMonth, 1).AddMonths(-1);
        if (prev < MinViewDate) return;
        NavigateTo(prev.Year, prev.Month, direction: -1);
    }

    // 다음 달로 이동 (최대 MaxViewDate까지)
    private void OnNextMonth()
    {
        var next = new DateTime(_viewYear, _viewMonth, 1).AddMonths(1);
        if (next > MaxViewDate) return;
        NavigateTo(next.Year, next.Month, direction: 1);
    }

    // 월 전환 — slideAnimator가 있으면 슬라이드 연출, 없으면 즉시 전환
    // direction: +1 = 다음 달(왼쪽으로 나감), -1 = 이전 달(오른쪽으로 나감)
    private void NavigateTo(int year, int month, int direction)
    {
        if (_isAnimating) return;

        if (_slideAnimator != null)
        {
            _isAnimating = true;

            float outOffsetX = direction > 0 ? -_slideOffsetX : _slideOffsetX;
            float inOffsetX = direction > 0 ? _slideOffsetX : -_slideOffsetX;

            _slideAnimator.StopSlide();
            _slideAnimator.SetPositionX(_defaultSlideX);
            _slideAnimator.SlideToX(_defaultSlideX + outOffsetX, _slideDuration, () =>
            {
                _viewYear = year;
                _viewMonth = month;
                Refresh();

                _slideAnimator.SetPositionX(_defaultSlideX + inOffsetX);
                _slideAnimator.SlideToX(_defaultSlideX, _slideDuration, () =>
                {
                    _isAnimating = false;
                });
            });
        }
        else
        {
            _viewYear = year;
            _viewMonth = month;
            Refresh();
        }
    }

    // 현재 보고 있는 연월에 맞춰 달력 셀을 갱신
    private void Refresh()
    {
        _txtYear.text = $"{_viewYear % 100}";
        _txtMonth.text = $"{_viewMonth}";

        var thisMonth = new DateTime(_viewYear, _viewMonth, 1);
        _btnPrevMonth.interactable = thisMonth.AddMonths(-1) >= MinViewDate;
        _btnNextMonth.interactable = thisMonth.AddMonths(1) <= MaxViewDate;

        DateTime today = GetIngameToday();
        var days = BuildDayGrid(_viewYear, _viewMonth);
        var entries = CalendarManager.Instance != null
            ? CalendarManager.Instance.GetEntriesForMonth(_viewYear, _viewMonth)
            : new List<CalendarEntry>();

        for (int i = 0; i < _cells.Count; i++)
        {
            if (i >= days.Count)
            {
                _cells[i].gameObject.SetActive(false);
                continue;
            }

            DateTime cellDate = days[i];
            bool isCurrent = cellDate.Year == _viewYear && cellDate.Month == _viewMonth;

            var dayEntries = new List<CalendarEntry>();
            foreach (var e in entries)
                if (e.Date == cellDate.Date)
                    dayEntries.Add(e);

            dayEntries.Sort((a, b) => a.DisplayPriority.CompareTo(b.DisplayPriority));

            var dayData = new CalendarDayData(
                cellDate,
                isCurrent,
                cellDate.Date == today.Date,
                dayEntries
            );

            _cells[i].gameObject.SetActive(true);
            _cells[i].Render(dayData, OnCellClicked);
        }
    }

    // 6주 × 7일(일요일 시작) 날짜 배열을 생성
    private static List<DateTime> BuildDayGrid(int year, int month)
    {
        var grid = new List<DateTime>(42);
        DateTime first = new DateTime(year, month, 1);
        int offset = (int)first.DayOfWeek; // 일요일=0 기준

        DateTime start = first.AddDays(-offset);
        for (int i = 0; i < 42; i++)
            grid.Add(start.AddDays(i));

        return grid;
    }

    // 셀 클릭 시 해당 날짜의 일정 팝업 표시
    private static void OnCellClicked(CalendarDayData data)
    {
        CalendarDayDetailPopup.Show(data);
    }

    // 셀 프리팹을 42개 인스턴스화하여 풀 구축 (최대 6주 × 7일)
    private void BuildCellPool()
    {
        foreach (var c in _cells)
            Destroy(c.gameObject);

        _cells.Clear();

        for (int i = 0; i < 42; i++)
            _cells.Add(Instantiate(_cellPrefab, _gridRoot));
    }

    // 게임 내 오늘 날짜를 반환 (TurnManager의 DateManager에서 가져옴, 없으면 시스템 날짜)
    private static DateTime GetIngameToday()
    {
        var tm = FindFirstObjectByType<TurnManager>();
        return tm != null ? tm.DateManager.CurrentDate : DateTime.Today;
    }
}