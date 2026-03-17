using System;
using System.Globalization;

// AlwaysEventTable 기반 날짜 조회 유틸 — AlwaysEventManager 없이 직접 SO를 읽음
// GameManager가 AlwaysEventManager에 의존하지 않고 리그 날짜를 계산할 수 있도록 분리
public static class AlwaysEventDateUtil
{
    // 현재 날짜 기준으로 다음 리그(vacation 타입) 시작일 반환
    public static bool TryGetNextLeagueDate(DateTime currentDate, out DateTime nextLeagueDate)
    {
        nextLeagueDate = default;

        var table = CachedSOData.Get<AlwaysEventTableSO>();
        if (table == null || table.Rows == null || table.Rows.Count == 0)
            return false;

        DateTime baseDate = currentDate.Date;
        bool found = false;
        DateTime bestDate = default;

        var rows = table.Rows;
        for (int i = 0; i < rows.Count; i++)
        {
            var row = rows[i];
            if (row == null) continue;
            if (!AlwaysEventManager.IsLeagueBreakEvent(row)) continue;

            if (!TryParseTableDate(row.termStart, out DateTime termStartDate))
                continue;

            DateTime candidate = termStartDate.Date;
            if (candidate < baseDate)               // 이미 지난 날짜는 제외
                continue;

            if (!found || candidate < bestDate)     // 가장 가까운 날짜로 갱신
            {
                bestDate = candidate;
                found = true;
            }
        }

        if (!found) return false;

        nextLeagueDate = bestDate;
        return true;
    }

    // AlwaysEventTable에서 첫 겨울방학 기간(termStart ~ termEnd)을 반환
    public static bool TryGetFirstWinterVacationTerm(out DateTime termStartDate, out DateTime termEndDate)
    {
        termStartDate = default;
        termEndDate = default;

        var table = CachedSOData.Get<AlwaysEventTableSO>();
        if (table == null || table.Rows == null || table.Rows.Count == 0)
            return false;

        bool found = false;
        DateTime bestStart = default;
        DateTime bestEnd = default;

        var rows = table.Rows;
        for (int i = 0; i < rows.Count; i++)
        {
            var row = rows[i];
            if (row == null) continue;
            if (row.type != "vacation") continue;
            if (row.name != "vacation_winter") continue;

            if (!TryParseTableDate(row.termStart, out DateTime start))
                continue;
            if (!TryParseTableDate(row.termEnd, out DateTime end))
                continue;

            if (!found || start.Date < bestStart.Date)
            {
                found = true;
                bestStart = start.Date;
                bestEnd = end.Date;
            }
        }

        if (!found)
            return false;

        termStartDate = bestStart;
        termEndDate = bestEnd;
        return true;
    }

    public static bool TryParseTableDate(string value, out DateTime date)
    {
        date = default;
        string s = (value ?? "").Trim();
        if (string.IsNullOrEmpty(s) || s == "-")
            return false;

        // yyMMdd (예: 260720 => 2026-07-20)
        if (s.Length == 6 &&
            int.TryParse(s.Substring(0, 2), NumberStyles.Integer, CultureInfo.InvariantCulture, out int yy) &&
            int.TryParse(s.Substring(2, 2), NumberStyles.Integer, CultureInfo.InvariantCulture, out int mm) &&
            int.TryParse(s.Substring(4, 2), NumberStyles.Integer, CultureInfo.InvariantCulture, out int dd))
        {
            return TryMakeDate(2000 + yy, mm, dd, out date);
        }

        // yyyyMMdd
        if (s.Length == 8 &&
            int.TryParse(s.Substring(0, 4), NumberStyles.Integer, CultureInfo.InvariantCulture, out int yyyy) &&
            int.TryParse(s.Substring(4, 2), NumberStyles.Integer, CultureInfo.InvariantCulture, out int month) &&
            int.TryParse(s.Substring(6, 2), NumberStyles.Integer, CultureInfo.InvariantCulture, out int day))
        {
            return TryMakeDate(yyyy, month, day, out date);
        }

        // 기타 포맷 fallback
        return DateTime.TryParse(s, CultureInfo.InvariantCulture, DateTimeStyles.None, out date);
    }

    private static bool TryMakeDate(int year, int month, int day, out DateTime date)
    {
        date = default;
        try
        {
            date = new DateTime(year, month, day);
            return true;
        }
        catch
        {
            return false;
        }
    }
}
