using System;

// 캘린더에 표시될 단일 일정 항목
[Serializable]
public class CalendarEntry
{
    public enum EntryType
    {
        Holiday,          // 공휴일 / 대체공휴일
        AcademicExam,     // 시험 기간 (중간·기말)
        AcademicFestival, // 축제 기간
        Vacation,         // 방학 (여름·겨울)
        Tournament,       // 공식 토너먼트
        FriendlyMatch,    // 친선 경기
    }

    public DateTime Date { get; }  // .Date 기준 (시각 제거)
    public EntryType Type { get; }
    public string Label { get; }  // 셀 배지·팝업 표시용
    public string Detail { get; }  // 기간 상세 ("3월 1일 ~ 3월 2일"), nullable

    public CalendarEntry(DateTime date, EntryType type, string label, string detail = null)
    {
        Date = date.Date;
        Type = type;
        Label = label;
        Detail = detail;
    }

    // 셀 표시 우선순위 (낮을수록 먼저)
    // 주말에 친선경기·공휴일 겹칠 시 친선경기 우선 표기
    public int DisplayPriority => Type switch
    {
        EntryType.FriendlyMatch => 0,
        EntryType.Holiday => 1,
        EntryType.Tournament => 2,
        EntryType.AcademicExam => 3,
        EntryType.AcademicFestival => 3,
        EntryType.Vacation => 4,
        _ => 99,
    };
}