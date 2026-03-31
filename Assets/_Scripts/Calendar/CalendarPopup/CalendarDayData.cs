using System;
using System.Collections.Generic;

// UI가 캘린더 셀을 렌더링할 때 필요한 데이터를 담는 readonly struct
// CalendarMonthView에서 생성하여 CalendarCell.Render()로 전달
public readonly struct CalendarDayData
{
    public readonly DateTime Date;                          // 날짜 (시각 제거된 .Date 기준)
    public readonly bool IsCurrentMonth;                    // 이월 날짜 여부 (현재 보고 있는 연월과 다른 날짜)
    public readonly bool IsToday;                           // 오늘 날짜 여부 (게임 내 날짜 기준)
    public readonly bool IsWeekend;                         // 주말 여부 (일요일,토요일 포함)
    public readonly bool IsSunday;                          // 주말 여부 (일요일,토요일 별도)
    public readonly bool IsSaturday;                        // 주말 여부 (일요일,토요일 별도)
    public readonly IReadOnlyList<CalendarEntry> Entries;   // DisplayPriority 오름차순 정렬
    public readonly DayColorType DayColor;                  // 셀 숫자 색상

    public enum DayColorType { Default, Red, Blue }         // 셀 숫자 색상 유형


    // 생성자: 날짜, 이월 여부, 오늘 여부, 일정 목록을 받아 나머지 필드 계산
    public CalendarDayData(
        DateTime date,
        bool isCurrentMonth,
        bool isToday,
        IReadOnlyList<CalendarEntry> entries)
    {
        Date = date;
        IsCurrentMonth = isCurrentMonth;
        IsToday = isToday;
        IsSunday = date.DayOfWeek == DayOfWeek.Sunday;
        IsSaturday = date.DayOfWeek == DayOfWeek.Saturday;
        IsWeekend = IsSunday || IsSaturday;
        Entries = entries;

        // 일요일,공휴일 → 빨간색 / 토요일 → 파란색 / 평일 → 기본색
        bool hasHoliday = false;
        foreach (var e in entries)
        {
            if (e.Type == CalendarEntry.EntryType.Holiday)
            {
                hasHoliday = true;
                break;
            }
        }

        if (IsSunday || hasHoliday) DayColor = DayColorType.Red;
        else if (IsSaturday) DayColor = DayColorType.Blue;
        else DayColor = DayColorType.Default;
    }
}