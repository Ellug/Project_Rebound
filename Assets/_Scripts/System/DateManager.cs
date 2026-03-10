using System;

//날짜 시스템 관리, n일차 단위 턴 진행, 연도 전환 판정
public class DateManager
{
    private DateTime _currentDate;
    private int _dayIndex;                             //게임 시작 이후 총 경과일
    private int _currentYear;                          //게임 내 연차 (n년차)

    public DateTime CurrentDate => _currentDate;
    public int DayIndex => _dayIndex;
    public int CurrentYear => _currentYear;
    public int DayInYear => _currentDate.DayOfYear;
    public string FormattedDate => _currentDate.ToString("yyyy. MM. dd");

    public event Action<DateTime, int> OnDateAdvanced;       //날짜 전진 시
    public event Action<int> OnYearChanged;                  //연도 전환 시

    public DateManager(DateTime startDate)
    {
        _currentDate = startDate;
        _dayIndex = 0;
        _currentYear = 1;
    }

    public DateManager() : this(new DateTime(2026, 3, 1)) { }

    //하루 전진 (내부에서 연도 전환 판정 후 날짜 이벤트 발행)
    public void AdvanceDay()
    {
        int prevMonth = _currentDate.Month;

        _currentDate = _currentDate.AddDays(1);
        _dayIndex++;

        int newMonth = _currentDate.Month;

        //연도 전환 (12월 -> 1월)
        if (newMonth == 1 && prevMonth == 12)
        {
            _currentYear++;
            OnYearChanged?.Invoke(_currentYear);
        }

        OnDateAdvanced?.Invoke(_currentDate, _dayIndex);
    }

    // Date 데이터 TM에서 세팅용
    public void SetState(DateTime currentDate, int dayIndex, int currentYear)
    {
        _currentDate = currentDate;
        _dayIndex = dayIndex < 0 ? 0 : dayIndex;
        _currentYear = currentYear < 1 ? 1 : currentYear;
    }
}
