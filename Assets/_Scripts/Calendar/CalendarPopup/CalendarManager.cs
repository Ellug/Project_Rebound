using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class CalendarManager : Singleton<CalendarManager>
{
    // 게임 허용 최대 날짜 (4년 + 2월 = 2030년 2월)
    public static readonly DateTime MaxDate = new DateTime(2030, 2, 28);

    private readonly Dictionary<int, List<CalendarEntry>> _monthCache = new(); // key: yyyyMM

    [SerializeField] private HolidayDatabase _holidayDb;
    private TurnManager _turnManager;
    public event Action OnCalendarChanged;
    private Dictionary<string, (DateTime min, DateTime max)> _lastGroupRanges;

    protected override void OnSingletonAwake() { }

    // Lobby 씬 초기화 시 TurnManager를 연결하여 턴 완료 이벤트를 구독
    public void Bind(TurnManager turnManager)
    {
        Unbind();
        _turnManager = turnManager;

        InvalidateAll();

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
        OnCalendarChanged?.Invoke();
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

    // 공휴일 수집
    // 설날·추석 등 연휴는 이름 기준 그룹키로 묶어 기간 Detail을 표시
    // 월 경계를 넘는 연휴(추석 9월 말 ~ 10월 초 등) 처리를 위해 전월·익월도 수집
    // 토너먼트(방학) 기간에 겹치는 공휴일은 무시
    private void CollectHolidays(int year, int month, List<CalendarEntry> result)
    {
        if (_holidayDb == null)
        {
            Debug.LogWarning("[CalendarManager] _holidayDb가 연결되지 않았습니다. Inspector에서 HolidayDatabase를 연결해주세요.");
            return;
        }

        var prevMonth = new DateTime(year, month, 1).AddMonths(-1);
        var nextMonth = new DateTime(year, month, 1).AddMonths(1);
        var nextMonth2 = new DateTime(year, month, 1).AddMonths(2);

        var allEntries = new List<HolidayEntry>();
        allEntries.AddRange(_holidayDb.GetHolidaysInMonth(prevMonth.Year, prevMonth.Month));
        allEntries.AddRange(_holidayDb.GetHolidaysInMonth(year, month));
        allEntries.AddRange(_holidayDb.GetHolidaysInMonth(nextMonth.Year, nextMonth.Month));
        allEntries.AddRange(_holidayDb.GetHolidaysInMonth(nextMonth2.Year, nextMonth2.Month));

        if (allEntries.Count == 0) return;

        allEntries.Sort((a, b) => a.date.CompareTo(b.date));

        foreach (var e in allEntries)
            if (GetHolidayGroupKey(e.name) != null)
                Debug.Log($"[Holiday] {e.date} {e.name} → {GetHolidayGroupKey(e.name)}");

        // 그룹키별 날짜 min~max 범위 계산
        var groupRanges = new Dictionary<string, (DateTime min, DateTime max)>();

        foreach (var e in allEntries)
        {
            string groupKey = GetHolidayGroupKey(e.name);
            if (groupKey == null) continue;

            DateTime date = ParseHolidayDate(e.date);

            if (groupRanges.TryGetValue(groupKey, out var range))
            {
                groupRanges[groupKey] = (
                    date < range.min ? date : range.min,
                    date > range.max ? date : range.max
                );
            }
            else
            {
                groupRanges[groupKey] = (date, date);
            }
        }

        // 그룹 범위에 주말 포함
        // 공휴일이 2일 이상인 그룹(연휴)만 주말 확장
        // 단일 공휴일(min == max)은 대체공휴일 없는 해이므로 확장 안 함
        foreach (var key in groupRanges.Keys.ToList())
        {
            var (min, max) = groupRanges[key];
            if (min == max) continue; // 단일 공휴일 — 확장 불필요

            // min 이전 연속 주말 확장
            DateTime extMin = min;
            while (extMin.AddDays(-1).DayOfWeek == DayOfWeek.Saturday ||
                   extMin.AddDays(-1).DayOfWeek == DayOfWeek.Sunday)
                extMin = extMin.AddDays(-1);

            // max 이후 연속 주말 확장
            DateTime extMax = max;
            while (extMax.AddDays(1).DayOfWeek == DayOfWeek.Saturday ||
                   extMax.AddDays(1).DayOfWeek == DayOfWeek.Sunday)
                extMax = extMax.AddDays(1);

            groupRanges[key] = (extMin, extMax);
        }

        // 토너먼트(방학) 기간 날짜 집합 — 해당 날짜의 공휴일은 무시
        var tournamentDates = GetTournamentDatesInMonth(year, month);

        // 당월 항목만 result에 추가
        // DB 등록 공휴일 + 연휴 범위 내 주말도 함께 추가
        var currentEntries = _holidayDb.GetHolidaysInMonth(year, month);

        // 그룹키 없는 단일 공휴일이 그룹 공휴일보다 먼저 오도록 정렬
        currentEntries.Sort((a, b) =>
        {
            bool aIsGroup = GetHolidayGroupKey(a.name) != null;
            bool bIsGroup = GetHolidayGroupKey(b.name) != null;
            if (aIsGroup != bIsGroup) return aIsGroup ? -1 : 1;
            return a.date.CompareTo(b.date);
        });

        var addedDates = new HashSet<DateTime>();

        // 공휴일 추가
        foreach (var e in currentEntries)
        {
            DateTime date = ParseHolidayDate(e.date);
            if (tournamentDates.Contains(date.Date)) continue;

            string groupKey = GetHolidayGroupKey(e.name);
            string detail = null;

            if (groupKey != null &&
                groupRanges.TryGetValue(groupKey, out var range) &&
                range.min != range.max)
            {
                detail = $"{range.min:M월 d일} ~ {range.max:M월 d일}";
            }

            if (addedDates.Add(date.Date))
                result.Add(new CalendarEntry(date, CalendarEntry.EntryType.Holiday, e.name, detail));
        }

        // 연휴 범위 내 주말 추가 — 그룹별로 한 번만 실행
        foreach (var kv in groupRanges)
        {
            string groupKey = kv.Key;
            var (min, max) = kv.Value;
            if (min == max) continue;

            string detail = $"{min:M월 d일} ~ {max:M월 d일}";

            // 그룹의 대표 이름 (전월·당월·익월 중 해당 그룹의 첫 번째 이름 사용)
            // currentEntries에만 의존하면 본일이 주말이라 당월에 없는 경우 label이 비어있음
            string label = string.Empty;
            foreach (var e in allEntries)
            {
                if (GetHolidayGroupKey(e.name) == groupKey)
                {
                    label = e.name;
                    break;
                }
            }

            for (DateTime wd = min; wd <= max; wd = wd.AddDays(1))
            {
                if (wd.Month != month || wd.Year != year) continue;
                if (wd.DayOfWeek != DayOfWeek.Saturday && wd.DayOfWeek != DayOfWeek.Sunday) continue;
                if (tournamentDates.Contains(wd.Date)) continue;
                if (addedDates.Add(wd.Date))
                    result.Add(new CalendarEntry(wd, CalendarEntry.EntryType.Holiday, label, detail));
            }
        }

        _lastGroupRanges = groupRanges;
    }

    // 해당 월의 토너먼트(vacation 타입) 기간 날짜 집합 반환
    private static HashSet<DateTime> GetTournamentDatesInMonth(int year, int month)
    {
        var result = new HashSet<DateTime>();

        DateTime from = new DateTime(year, month, 1);
        DateTime to = new DateTime(year, month, DateTime.DaysInMonth(year, month));

        var rows = AlwaysEventManager.GetRowsInRange(from, to);
        foreach (var row in rows)
        {
            if (row == null) continue;
            if (!AlwaysEventManager.IsLeagueBreakEvent(row)) continue;

            if (!AlwaysEventDateUtil.TryParseTableDate(row.termStart, out DateTime termStart)) continue;
            if (!AlwaysEventDateUtil.TryParseTableDate(row.termEnd, out DateTime termEnd)) continue;

            DateTime start = termStart.Date < from.Date ? from.Date : termStart.Date;
            DateTime end = termEnd.Date > to.Date ? to.Date : termEnd.Date;

            for (DateTime d = start; d <= end; d = d.AddDays(1))
                result.Add(d.Date);
        }

        return result;
    }

    // 공휴일 이름 → 연휴 그룹 키
    // 단일 공휴일이라도 대체공휴일과 묶이면 기간 표시
    // 연휴가 없는 해에는 range.min == range.max 이므로 Detail null 처리됨
    private static string GetHolidayGroupKey(string name)
    {
        switch (name)
        {
            case "설날":
            case "설날 연휴":
            case "대체공휴일(설날)":
                return "seollal";

            case "추석":
            case "추석 연휴":
            case "대체공휴일(추석)":
                return "chuseok";

            case "광복절":
            case "광복절 연휴":
            case "대체공휴일(광복절)":
                return "liberation";

            case "석가탄신일":
            case "대체공휴일(석가탄신일)":
                return "buddha";

            case "현충일":
            case "대체공휴일(현충일)":
                return "memorial";

            case "어린이날":
            case "대체공휴일(어린이날)":
                return "children";

            case "개천절":
            case "대체공휴일(개천절)":
                return "foundation";

            case "성탄절":
            case "대체공휴일(성탄절)":
                return "christmas";

            case "개헌절":
            case "대체공휴일(개헌절)":
                return "constitution";

            case "삼일절":
            case "대체공휴일(삼일절)":
                return "independence";

            case "한글날":
            case "대체공휴일(한글날)":
                return "hangul";

            default:
                return null;
        }
    }

    // yyyymmdd 정수 → DateTime 변환
    private static DateTime ParseHolidayDate(int yyyymmdd)
    {
        int y = yyyymmdd / 10000;
        int m = (yyyymmdd / 100) % 100;
        int d = yyyymmdd % 100;
        return new DateTime(y, m, d);
    }

    private void CollectAlwaysEvents(DateTime from, DateTime to, List<CalendarEntry> result)
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
                if (entryType == CalendarEntry.EntryType.Holiday &&
                    !seen.Add((d, entryType)))
                    continue;

                // AlwaysEventTable holiday는 단일 날짜 detail=null이므로
                // _lastGroupRanges에서 연휴 기간 detail을 보정
                string finalDetail = detail;
                if (entryType == CalendarEntry.EntryType.Holiday && finalDetail == null)
                {
                    string gk = GetAlwaysEventGroupKey(row.name);
                    if (gk != null &&
                        _lastGroupRanges != null &&
                        _lastGroupRanges.TryGetValue(gk, out var gr) &&
                        gr.min != gr.max)
                    {
                        finalDetail = $"{gr.min:M월 d일} ~ {gr.max:M월 d일}";
                    }
                }

                result.Add(new CalendarEntry(d, entryType, label, finalDetail));
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

        // 메신저 채팅 예약
        if (FriendlyMatchManager.Instance == null) return;

        var schedule = new Dictionary<DateTime, string>(
            FriendlyMatchManager.Instance.GetBookedMatchSchedule());

        if (to.Year != from.Year)
        {
            foreach (var kvp in FriendlyMatchManager.Instance.GetBookedMatchSchedule())
                if (!schedule.ContainsKey(kvp.Key))
                    schedule[kvp.Key] = kvp.Value;
        }

        foreach (var kvp in schedule)
        {
            DateTime matchDate = kvp.Key;

            // 날짜 범위 체크 (연도 포함)
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
        return termStart.Date == termEnd.Date
            ? null
            : $"{termStart:M월 d일} ~ {termEnd:M월 d일}";
    }

    // GetHolidayGroupKey와 대응되는 AlwaysEventRow.name → 그룹키 매핑 (연휴 기간 Detail 보정용)
    private static string GetAlwaysEventGroupKey(string rowName) => rowName switch
    {
        "holiday_chuseok" => "chuseok",
        "holiday_seollal" => "seollal",
        "holiday_buddha" => "buddha",
        "holiday_liberation_day" or "holiday_liberation_Day" => "liberation",
        "holiday_children_day" => "children",
        "holiday_memorial_day" => "memorial",
        "holiday_foundation_day" => "foundation",
        "holiday_hangul_day" => "hangul",
        "holiday_christmas" => "christmas",
        "holiday_independence" => "independence",
        _ => null
    };
}

