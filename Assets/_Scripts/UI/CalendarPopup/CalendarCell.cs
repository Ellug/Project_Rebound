using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

// 월간 달력의 날짜 셀 한 칸
// CalendarDayData를 받아 숫자 색상, 배지 표시, 오늘 강조를 렌더링
public class CalendarCell : MonoBehaviour
{
    [Header("날짜 숫자")]
    [SerializeField] private TMP_Text _txtDay;
    [SerializeField] private Image _todayHighlight;

    [Header("배지")]
    [SerializeField] private CalendarBadge _badge; // 셀 내부에 미리 배치된 배지 1개

    [Header("색상 설정")]
    [SerializeField] private Color _colorDefault = Color.white;
    [SerializeField] private Color _colorRed = new Color(1f, 0.3f, 0.3f);
    [SerializeField] private Color _colorBlue = new Color(0.3f, 0.6f, 1f);
    [SerializeField] private Color _colorDimmed = new Color(0.5f, 0.5f, 0.5f, 0.5f); // 이월 날짜
    [SerializeField] private Color _colorDimmedRed = new Color32(0x3F, 0x11, 0x00, 0xFF);   // 이월 날짜 중 일요일/공휴일
    [SerializeField] private Color _colorDimmedBlue = new Color32(0x07, 0x24, 0x3F, 0xFF);  // 이월 날짜 중 토요일

    private Button _button;
    private Action<CalendarDayData> _onClick;
    private CalendarDayData _data;

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

        // 날짜 숫자 & 색상
        _txtDay.text = data.Date.Day.ToString();
        _txtDay.color = GetDayTextColor(data);

        // 오늘 강조
        if (_todayHighlight != null)
            _todayHighlight.enabled = data.IsToday;

        // 배지 갱신 — 현재 월 날짜에만 표시
        UpdateBadge(data);

        // 클릭은 현재 월 날짜만 허용
        if (_button != null)
            _button.interactable = data.IsCurrentMonth;
    }

    private Color GetDayTextColor(CalendarDayData data)
    {
        if (data.IsCurrentMonth)
        {
            return data.DayColor switch
            {
                CalendarDayData.DayColorType.Red => _colorRed,
                CalendarDayData.DayColorType.Blue => _colorBlue,
                _ => _colorDefault,
            };
        }

        return data.DayColor switch
        {
            CalendarDayData.DayColorType.Red => _colorDimmedRed,
            CalendarDayData.DayColorType.Blue => _colorDimmedBlue,
            _ => _colorDimmed,
        };
    }

    private void UpdateBadge(CalendarDayData data)
    {
        if (_badge == null) return;

        if (!data.IsCurrentMonth || data.Entries == null || data.Entries.Count == 0)
        {
            _badge.gameObject.SetActive(false);
            return;
        }

        _badge.gameObject.SetActive(true);
        _badge.Render(data.Entries[0]); // 첫 번째 일정만 표시
    }

    private void HandleClick() => _onClick?.Invoke(_data);
}