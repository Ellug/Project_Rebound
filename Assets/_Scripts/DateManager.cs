using System;

//날짜 시스템 관리, n일차 단위 턴 진행, 학기/졸업/입학 판정
public class DateManager
{
    private const int SEMESTER_ONE_START_MONTH = 3;    //1학기 시작 월
    private const int SEMESTER_ONE_END_MONTH = 7;      //1학기 종료 월
    private const int SEMESTER_TWO_START_MONTH = 9;    //2학기 시작 월
    private const int SEMESTER_TWO_END_MONTH = 12;     //2학기 종료 월
    private const int GRADUATION_MONTH = 2;            //졸업 월
    private const int ENROLLMENT_MONTH = 3;            //입학 월

    private DateTime _currentDate;
    private int _dayIndex;                             //게임 시작 이후 총 경과일
    private int _currentYear;                          //게임 내 연차 (n년차)

    public DateTime CurrentDate => _currentDate;
    public int DayIndex => _dayIndex;
    public int CurrentYear => _currentYear;
    public int DayInYear => _currentDate.DayOfYear;
    public string FormattedDate => _currentDate.ToString("yyyy. MM. dd");

    //0 = 방학, 1 = 1학기, 2 = 2학기
    public int CurrentSemester
    {
        get
        {
            int month = _currentDate.Month;
            if (month >= SEMESTER_ONE_START_MONTH && month <= SEMESTER_ONE_END_MONTH) return 1;
            if (month >= SEMESTER_TWO_START_MONTH && month <= SEMESTER_TWO_END_MONTH) return 2;
            return 0;
        }
    }

    public bool IsVacation => CurrentSemester == 0;

    public event Action<DateTime, int> OnDateAdvanced;       //날짜 전진 시
    public event Action<int> OnSemesterStarted;              //학기 시작 시
    public event Action<int> OnYearChanged;                  //연도 전환 시
    public event Action OnGraduationTriggered;               //졸업 시점
    public event Action OnEnrollmentTriggered;               //입학 시점

    public DateManager(DateTime startDate)
    {
        _currentDate = startDate;
        _dayIndex = 0;
        _currentYear = 1;
    }

    public DateManager() : this(new DateTime(2026, 3, 1)) { }

    //하루 전진 (내부에서 학기/연도/졸업/입학 전환을 판정하고 이벤트 발행)
    public void AdvanceDay()
    {
        int prevMonth = _currentDate.Month;
        int prevSemester = CurrentSemester;

        _currentDate = _currentDate.AddDays(1);
        _dayIndex++;

        int newMonth = _currentDate.Month;

        //연도 전환 (12월 -> 1월)
        if (newMonth == 1 && prevMonth == 12)
        {
            _currentYear++;
            OnYearChanged?.Invoke(_currentYear);
        }

        //졸업 판정
        if (newMonth == GRADUATION_MONTH && prevMonth != newMonth)
        {
            OnGraduationTriggered?.Invoke();
        }

        //입학 판정
        if (newMonth == ENROLLMENT_MONTH && prevMonth != newMonth)
        {
            OnEnrollmentTriggered?.Invoke();
        }

        //학기 전환 판정
        int newSemester = CurrentSemester;
        if (newSemester != 0 && newSemester != prevSemester)
        {
            OnSemesterStarted?.Invoke(newSemester);
        }

        OnDateAdvanced?.Invoke(_currentDate, _dayIndex);
    }

    //특정 날짜까지 남은 일수 (D-Day 계산용)
    public int GetDaysUntil(DateTime targetDate)
    {
        return (targetDate - _currentDate).Days;
    }

    public bool CheckIsToday(int month, int day)
    {
        return _currentDate.Month == month && _currentDate.Day == day;
    }
}