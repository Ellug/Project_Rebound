using System;
using System.Collections.Generic;

//턴 한 사이클 동안 모듈 간 공유되는 컨텍스트
public class TurnContext
{
    public int TurnIndex { get; set; }
    public DateTime CurrentDate { get; set; }
    public int DayNumber => TurnIndex + 1;      //표시용 (1-based)
    public int CurrentSemester { get; set; }    //0=방학, 1=1학기, 2=2학기
    public GamePhase CurrentPhase { get; set; } = GamePhase.DailyTraining;
    public TurnActionType SelectedAction { get; set; } = TurnActionType.Rest;
    public bool IsMatchDay { get; set; }

    //요일 관련 편의 프로퍼티
    public DayOfWeek DayOfWeek => CurrentDate.DayOfWeek;
    public bool IsFriday  => CurrentDate.DayOfWeek == DayOfWeek.Friday;
    public bool IsSaturday => CurrentDate.DayOfWeek == DayOfWeek.Saturday;
    public bool IsSunday  => CurrentDate.DayOfWeek == DayOfWeek.Sunday;
    public bool IsWeekend => IsSaturday || IsSunday;

    //모듈 간 임시 데이터 공유용
    public Dictionary<string, object> ExtraData { get; set; } = new Dictionary<string, object>();
}