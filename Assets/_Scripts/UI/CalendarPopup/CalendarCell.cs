using System;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

// 월간 달력의 날짜 셀 한 칸
// CalendarDayData를 받아 숫자 색상,배지,오늘 강조를 렌더링
public class CalendarCell : MonoBehaviour
{
    [Header("날짜 숫자")]
    [SerializeField] private TMP_Text _txtDay;                                       // 날짜 숫자 텍스트
    [SerializeField] private Image _todayHighlight;                                  // 오늘 날짜 강조용 이미지

    [Header("배지 (최대 3개)")]
    [SerializeField] private CalendarBadge[] _badges;                                // 날짜별 이벤트 배지 (최대 3개, 넘칠 경우 ... 표시)

    [Header("색상 설정")]
    [SerializeField] private Color _colorDefault = Color.white;                      // 평일
    [SerializeField] private Color _colorRed = new Color(1f, 0.3f, 0.3f);            // 일요일 및 공휴일
    [SerializeField] private Color _colorBlue = new Color(0.3f, 0.6f, 1f);           // 토요일
    [SerializeField] private Color _colorDimmed = new Color(0.5f, 0.5f, 0.5f, 0.5f); // 이월 날짜

    private Button _button;
    private Action<CalendarDayData> _onClick; // 셀 클릭 시 CalendarDayData 전달 콜백
    private CalendarDayData _data;            // 현재 렌더링된 날짜 데이터 (클릭 시 전달용)

    // 셀 클릭 시 팝업에 날짜 데이터 전달
    private void Awake()
    {
        _button = GetComponent<Button>();
        if (_button != null)
            _button.onClick.AddListener(HandleClick);
    }

    // CalendarMonthView에서 매 Refresh마다 호출, 셀 전체를 갱신
    public void Render(CalendarDayData data, Action<CalendarDayData> onClick)
    {
        _data = data;
        _onClick = onClick;

        // 날짜 숫자, 색상
        _txtDay.text = data.Date.Day.ToString();
        _txtDay.color = !data.IsCurrentMonth
            ? _colorDimmed
            : data.DayColor switch
            {
                CalendarDayData.DayColorType.Red => _colorRed,
                CalendarDayData.DayColorType.Blue => _colorBlue,
                _ => _colorDefault,
            };

        // 오늘 강조
        if (_todayHighlight != null)
            _todayHighlight.enabled = data.IsToday;

        // 배지 렌더링 (현재 월 날짜에만 표시)
        int badgeLen = _badges?.Length ?? 0;
        for (int i = 0; i < badgeLen; i++)
        {
            bool show = i < data.Entries.Count && data.IsCurrentMonth;
            _badges[i].gameObject.SetActive(show);
            if (show) _badges[i].Render(data.Entries[i]);
        }

        // 클릭은 현재 월 날짜만 허용
        if (_button != null)
            _button.interactable = data.IsCurrentMonth;
    }

    // 셀 클릭 시 CalendarDayData를 전달하여 팝업 표시
    private void HandleClick() => _onClick?.Invoke(_data);
}