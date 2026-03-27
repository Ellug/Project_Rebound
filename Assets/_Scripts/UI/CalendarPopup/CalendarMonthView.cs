using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

// 월간 캘린더 팝업 UI 컨트롤러
public class CalendarMonthView : UIBase
{
    [Header("헤더")]
    [SerializeField] private TMP_Text _txtMonthYear;
    [SerializeField] private Button _btnPrevMonth;
    [SerializeField] private Button _btnNextMonth;

    [Header("셀 풀")]
    [SerializeField] private Transform _gridRoot;
    [SerializeField] private CalendarCell _cellPrefab;

    private readonly List<CalendarCell> _cells = new();

    private int _viewYear;
    private int _viewMonth;

    private static readonly DateTime MinViewDate = new DateTime(2026, 3, 1);
    private static readonly DateTime MaxViewDate = CalendarManager.MaxDate;

    public override void Init()
    {
        base.Init();
        _btnPrevMonth.onClick.AddListener(OnPrevMonth);
        _btnNextMonth.onClick.AddListener(OnNextMonth);
        BuildCellPool();
    }

    // 팝업이 열릴 때 현재 날짜로 초기화하여 달력 표시
    public override void Open()
    {
        base.Open();

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

        Refresh();
    }

    // 이전 달로 이동 (최소 MinViewDate까지)
    private void OnPrevMonth()
    {
        var prev = new DateTime(_viewYear, _viewMonth, 1).AddMonths(-1);
        if (prev < MinViewDate) return;
        _viewYear = prev.Year;
        _viewMonth = prev.Month;
        Refresh();
    }

    // 다음 달로 이동 (최대 MaxViewDate까지)
    private void OnNextMonth()
    {
        var next = new DateTime(_viewYear, _viewMonth, 1).AddMonths(1);
        if (next > MaxViewDate) return;
        _viewYear = next.Year;
        _viewMonth = next.Month;
        Refresh();
    }

    // 현재 보고 있는 연월에 맞춰 달력 셀을 갱신
    private void Refresh()
    {
        _txtMonthYear.text = $"{_viewYear}년 {_viewMonth}월";

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
            if (i >= days.Count) { _cells[i].gameObject.SetActive(false); continue; }

            DateTime cellDate = days[i];
            bool isCurrent = cellDate.Year == _viewYear && cellDate.Month == _viewMonth;

            var dayEntries = new List<CalendarEntry>();
            foreach (var e in entries)
                if (e.Date == cellDate.Date) dayEntries.Add(e);
            dayEntries.Sort((a, b) => a.DisplayPriority.CompareTo(b.DisplayPriority));

            var dayData = new CalendarDayData(cellDate, isCurrent, cellDate.Date == today.Date, dayEntries);

            _cells[i].gameObject.SetActive(true);
            _cells[i].Render(dayData, OnCellClicked);
        }
    }

    // 6주 × 7일(월요일 시작) 날짜 배열을 생성
    // 일요일=0 → 오프셋 6, 월요일=1 → 오프셋 0
    private static List<DateTime> BuildDayGrid(int year, int month)
    {
        var grid = new List<DateTime>(42);
        DateTime first = new DateTime(year, month, 1);
        int dow = (int)first.DayOfWeek;
        int offset = dow == 0 ? 6 : dow - 1;

        DateTime start = first.AddDays(-offset);
        for (int i = 0; i < 42; i++) grid.Add(start.AddDays(i));
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
        foreach (var c in _cells) Destroy(c.gameObject);
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