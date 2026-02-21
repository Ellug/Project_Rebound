using System;
using System.Collections.Generic;
using System.Globalization;
using UnityEngine;

public class AlwaysEventManager : MonoBehaviour
{
    private TurnManager _turnManager;
    private GameState _gameState;
    private HashSet<string> _activeEventIds; // GameFlowData.ActiveEventIds 참조

    // TurnManager와 연동
    public void Bind(TurnManager turnManager, GameState gameState, HashSet<string> activeEventIds)
    {
        UnsubscribeTurnManager();

        _turnManager = turnManager;
        _gameState = gameState;
        _activeEventIds = activeEventIds;

        SubscribeTurnManager();
    }

    // TurnManager 연동 해제
    public void Unbind()
    {
        UnsubscribeTurnManager();

        _turnManager = null;
        _gameState = null;
        _activeEventIds = null;
    }

    void OnDestroy()
    {
        UnsubscribeTurnManager();
    }

    private void SubscribeTurnManager()
    {
        if (_turnManager == null)
            return;

        _turnManager.OnTurnCompleted -= HandleTurnCompleted;
        _turnManager.OnTurnCompleted += HandleTurnCompleted;
    }

    private void UnsubscribeTurnManager()
    {
        if (_turnManager != null)
            _turnManager.OnTurnCompleted -= HandleTurnCompleted;
    }

    // 턴 완료 후 현재 날짜 기준으로 상시 이벤트 실행
    private void HandleTurnCompleted(TurnContext context)
    {
        if (_turnManager == null)
            return;

        DateTime today = _turnManager.DateManager.CurrentDate.Date;
        _gameState?.SyncState(_turnManager.DateManager.CurrentDate, _turnManager.TurnIndex);
        CheckEvents(today);
    }

    private void CheckEvents(DateTime today)
    {
        if (_activeEventIds == null) return;
        if (!TryGetAlwaysEventTable(out var table)) return;

        var rows = table.Rows;

        // 1. 활성 중인 이벤트 중 termEnd를 초과한 것 제거
        for (int i = 0; i < rows.Count; i++)
        {
            var row = rows[i];
            if (row == null) continue;

            string id = GetRowId(row);
            if (!_activeEventIds.Contains(id)) continue; // 활성 중이 아니면 스킵

            if (!TryParseTableDate(row.termEnd, out DateTime termEndDate)) continue;

            if (today > termEndDate.Date)
            {
                _activeEventIds.Remove(id);
                Debug.Log($"[AlwaysEvent] Ended: {id} ({today:yyyy-MM-dd})");
            }
        }

        // 2. term 범위 내 새 이벤트 활성화
        for (int i = 0; i < rows.Count; i++)
        {
            var row = rows[i];
            if (row == null) continue;

            if (!TryParseTableDate(row.termStart, out DateTime termStartDate)) continue;
            if (!TryParseTableDate(row.termEnd, out DateTime termEndDate)) continue;

            if (today < termStartDate.Date || today > termEndDate.Date) continue; // term 범위 밖이면 스킵

            string id = GetRowId(row);
            if (!_activeEventIds.Add(id)) continue; // 이미 활성 중이면 스킵

            // 신규 활성화 처리
            if (IsLeagueBreakEvent(row))
            {
                if (GameManager.Instance != null)
                    GameManager.Instance.OpenLeague();
                Debug.Log($"[AlwaysEvent] League started: {id} ({today:yyyy-MM-dd})");
            }
            else
                Debug.Log($"[AlwaysEvent] Started: {id} ({today:yyyy-MM-dd}) | type={row.type} | effect={row.effectId}");
        }
    }

    // GameManager가 다음 리그 날짜를 계산하기 위해 호출
    public bool TryGetNextLeagueDate(DateTime currentDate, out DateTime nextLeagueDate)
    {
        nextLeagueDate = default;

        if (!TryGetAlwaysEventTable(out var table))
            return false;

        DateTime baseDate = currentDate.Date;
        bool found = false;
        DateTime bestDate = default;

        var rows = table.Rows;
        for (int i = 0; i < rows.Count; i++)
        {
            var row = rows[i];
            if (row == null) continue;
            if (!IsLeagueBreakEvent(row)) continue;

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

    private static string GetRowId(AlwaysEventRow row)
        => string.IsNullOrWhiteSpace(row.id) ? "(no-id)" : row.id.Trim();

    private bool TryGetAlwaysEventTable(out AlwaysEventTableSO table)
    {
        table = CachedSOData.AlwaysEventTable;
        if (table != null && table.Rows != null && table.Rows.Count > 0)
            return true;

        Debug.LogWarning("[AlwaysEventManager] CachedSOData.AlwaysEventTable이 비어 있어 AlwaysEvent 처리를 건너 뜀.");
        return false;
    }

    private static bool IsLeagueBreakEvent(AlwaysEventRow row)
        => row.id == "summer_break" || row.id == "winter_break";

    private bool TryParseTableDate(string value, out DateTime date)
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
            int year = 2000 + yy;
            return TryMakeDate(year, mm, dd, out date);
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

    private bool TryMakeDate(int year, int month, int day, out DateTime date)
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
