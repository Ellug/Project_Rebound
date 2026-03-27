using System;
using System.Collections.Generic;
using System.Linq;

public class CalendarManager : Singleton<CalendarManager>
{
    // 게임 허용 최대 날짜 (4년 + 2월 = 2030년 2월)
    public static readonly DateTime MaxDate = new DateTime(2030, 2, 28);

    private readonly Dictionary<int, List<CalendarEntry>> _monthCache = new(); // key: yyyyMM

    private HolidayDatabase _holidayDb;
    private TurnManager _turnManager;

    protected override void OnSingletonAwake()
    {
        _holidayDb = CachedSOData.Get<HolidayDatabase>();
    }

    // Lobby 씬 초기화 시 TurnManager를 연결하여 턴 완료 이벤트를 구독
    public void Bind(TurnManager turnManager)
    {
        Unbind();
        _turnManager = turnManager;

        if (_turnManager != null)
            _turnManager.OnTurnCompleted += HandleTurnCompleted;
    }

    public void Unbind()
    {
        if (_turnManager != null)
            _turnManager.OnTurnCompleted -= HandleTurnCompleted;

        _turnManager = null;
    }

    private void HandleTurnCompleted(TurnContext context)
    {
        // 현재 달 + 다음 달 무효화 (친선전이 다음 달에 예약될 수 있음)
        InvalidateMonth(context.CurrentDate);
        InvalidateMonth(context.CurrentDate.AddMonths(1));
    }

    // 특정 월 캐시를 제거 (친선전 예약·취소 직후 호출)
    public void InvalidateMonth(DateTime date)
    {
        _monthCache.Remove(date.Year * 100 + date.Month);
    }

    // 전체 캐시를 제거 (씬 전환,게임 리셋 시 호출)
    public void InvalidateAll() => _monthCache.Clear();

    // 지정 날짜의 CalendarEntry 목록을 DisplayPriority 오름차순으로 반환
    public IReadOnlyList<CalendarEntry> GetEntriesForDay(DateTime date)
    {
        return GetEntriesForMonth(date.Year, date.Month)
            .Where(e => e.Date == date.Date)
            .OrderBy(e => e.DisplayPriority)
            .ToList();
    }

    // 지정 월의 CalendarEntry 목록을 반환 (캐시 히트 시 재사용)
    public IReadOnlyList<CalendarEntry> GetEntriesForMonth(int year, int month)
    {
        int key = year * 100 + month;
        if (_monthCache.TryGetValue(key, out var cached)) return cached;

        DateTime from = new DateTime(year, month, 1);
        DateTime to = new DateTime(year, month, DateTime.DaysInMonth(year, month));

        var result = new List<CalendarEntry>();

        CollectHolidays(year, month, result);
        CollectAlwaysEvents(from, to, result);
        CollectFriendlyMatches(from, to, result);

        _monthCache[key] = result;
        return result;
    }

    // 셀 색상 결정용 — 지정 날짜의 최우선 EntryType을 반환
    public CalendarEntry.EntryType? GetTopEntryType(DateTime date)
    {
        var entries = GetEntriesForDay(date);
        return entries.Count > 0 ? entries[0].Type : (CalendarEntry.EntryType?)null;
    }

    // 날짜 팝업용 — 지정 날짜의 최우선 CalendarEntry를 반환
    public CalendarEntry GetTopEntry(DateTime date)
    {
        var entries = GetEntriesForDay(date);
        return entries.Count > 0 ? entries[0] : null;
    }

    // 지정 날짜가 플레이 가능 범위(2026.03.01 ~ MaxDate)인지 검사
    public static bool IsDateInPlayRange(DateTime date)
        => date >= new DateTime(2026, 3, 1) && date <= MaxDate;

    // HolidayDatabase.GetHolidaysInMonth()로 해당 월 공휴일을 한 번에 읽음
    private void CollectHolidays(int year, int month, List<CalendarEntry> result)
    {
        if (_holidayDb == null) return;

        foreach (var entry in _holidayDb.GetHolidaysInMonth(year, month))
        {
            int d = entry.date % 100;
            var date = new DateTime(year, month, d);
            result.Add(new CalendarEntry(date, CalendarEntry.EntryType.Holiday, entry.name));
        }
    }

    // AlwaysEventManager.GetRowsInRange()로 해당 월과 겹치는 row를 읽어
    // type에 따라 AcademicExam / AcademicFestival / Holiday / Tournament 엔트리를 생성
    private static void CollectAlwaysEvents(DateTime from, DateTime to, List<CalendarEntry> result)
    {
        var seen = new HashSet<(DateTime, CalendarEntry.EntryType)>();

        // 이미 CollectHolidays에서 추가된 Holiday 날짜를 seen에 등록
        foreach (var e in result)
        {
            if (e.Type == CalendarEntry.EntryType.Holiday)
                seen.Add((e.Date, e.Type));
        }

        var rows = AlwaysEventManager.GetRowsInRange(from, to);

        foreach (var row in rows)
        {
            if (row == null) continue;

            string type = row.type ?? string.Empty;

            CalendarEntry.EntryType entryType;
            switch (type)
            {
                case "exam": entryType = CalendarEntry.EntryType.AcademicExam; break;
                case "festival": entryType = CalendarEntry.EntryType.AcademicFestival; break;
                case "holiday": entryType = CalendarEntry.EntryType.Holiday; break;
                case "vacation":
                    if (!AlwaysEventManager.IsLeagueBreakEvent(row)) continue;
                    entryType = CalendarEntry.EntryType.Tournament;
                    break;
                default: continue; // roster 등 캘린더 미표시 타입
            }

            if (!AlwaysEventDateUtil.TryParseTableDate(row.termStart, out DateTime termStart)) continue;
            if (!AlwaysEventDateUtil.TryParseTableDate(row.termEnd, out DateTime termEnd)) continue;

            string label = ResolveLabel(row.name, type);
            string detail = BuildDetail(type, termStart, termEnd);

            DateTime start = termStart.Date < from.Date ? from.Date : termStart.Date;
            DateTime end = termEnd.Date > to.Date ? to.Date : termEnd.Date;

            for (DateTime d = start; d <= end; d = d.AddDays(1))
            {
                // Holiday 중복 제거 (HolidayDatabase 항목 우선)
                if (entryType == CalendarEntry.EntryType.Holiday &&
                    !seen.Add((d, entryType)))
                    continue;

                result.Add(new CalendarEntry(d, entryType, label, detail));
            }
        }
    }

    // GameManager·FriendlyMatchManager에서 확정된 친선경기 일정을 읽음
    // GameManager 단일 예약과 채팅 예약이 같은 날짜면 GameManager 예약을 우선
    private static void CollectFriendlyMatches(DateTime from, DateTime to, List<CalendarEntry> result)
    {
        if (GameManager.Instance == null) return;

        // GameManager 단일 예약
        if (GameManager.Instance.IsFriendlyMatchConfirmed)
        {
            DateTime matchDate = GameManager.Instance.FriendlyMatchDate;
            if (matchDate >= from && matchDate <= to)
            {
                string opp = GameManager.Instance.FriendlyOpponentName;
                string label = string.IsNullOrWhiteSpace(opp) ? "친선 경기" : $"친선전 vs {opp}";
                result.Add(new CalendarEntry(matchDate, CalendarEntry.EntryType.FriendlyMatch, label));
            }
        }

        // 메신저 채팅 예약 (GameManager 예약 날짜 중복 제외)
        if (FriendlyMatchManager.Instance == null) return;

        var schedule = FriendlyMatchManager.Instance.GetBookedMatchSchedule(from.Year);
        foreach (var kvp in schedule)
        {
            DateTime matchDate = kvp.Key;
            if (matchDate < from || matchDate > to) continue;

            if (GameManager.Instance.IsFriendlyMatchConfirmed &&
                GameManager.Instance.FriendlyMatchDate.Date == matchDate.Date) continue;

            string label = string.IsNullOrWhiteSpace(kvp.Value)
                ? "친선 경기"
                : $"친선전 vs {kvp.Value}";

            result.Add(new CalendarEntry(matchDate, CalendarEntry.EntryType.FriendlyMatch, label));
        }
    }

    // AlwaysEventManager.GetEventDescription의 name 분기와 동일한 표시 이름 매핑
    private static string ResolveLabel(string name, string type)
    {
        switch (name)
        {
            case "first_midterm_exam": return "1학기 중간고사";
            case "first_final_exam": return "1학기 기말고사";
            case "second_midterm_exam": return "2학기 중간고사";
            case "second_final_exam": return "2학기 기말고사";
            case "festival_sports_day": return "체육대회";
            case "festival_school": return "학교 축제";
            case "holiday_children_day": return "어린이날";
            case "holiday_buddha": return "석가탄신일";
            case "holiday_memorial_day": return "현충일";
            case "holiday_liberation_Day":
            case "holiday_liberation_day": return "광복절";
            case "holiday_chuseok": return "추석";
            case "holiday_foundation_day": return "개천절";
            case "holiday_hangul_day": return "한글날";
            case "holiday_christmas": return "성탄절";
            case "holiday_independence": return "삼일절";
            case "vacation_summer": return "여름 방학 (토너먼트)";
            case "vacation_winter": return "겨울 방학 (토너먼트)";
            default:
                return type switch
                {
                    "exam" => "시험 기간",
                    "festival" => "학교 행사",
                    "holiday" => "공휴일",
                    "vacation" => "공식 토너먼트",
                    _ => "이벤트",
                };
        }
    }

    // 기간이 하루 이상이면 "M월 d일 ~ M월 d일" 형식의 Detail 문자열을 생성
    private static string BuildDetail(string type, DateTime termStart, DateTime termEnd)
    {
        if (type == "holiday") return null; // 공휴일은 단일 날짜이므로 기간 불필요

        return termStart.Date == termEnd.Date
            ? null
            : $"{termStart:M월 d일} ~ {termEnd:M월 d일}";
    }
}