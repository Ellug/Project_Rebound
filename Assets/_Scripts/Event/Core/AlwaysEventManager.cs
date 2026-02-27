using System;
using System.Collections.Generic;
using UnityEngine;

public class AlwaysEventManager : MonoBehaviour
{
    // 이벤트가 새로 활성화될 때 발행 — 구독자가 row.type 또는 row.id로 분기 처리
    public event Action<AlwaysEventRow> OnEventActivated;
    // 이벤트가 만료될 때 발행
    public event Action<AlwaysEventRow> OnEventExpired;

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

        OnEventActivated = null;
        OnEventExpired = null;
    }

    void OnDestroy()
    {
        UnsubscribeTurnManager();
    }

    private void SubscribeTurnManager()
    {
        if (_turnManager == null) return;

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
        if (_turnManager == null) return;

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
                OnEventExpired?.Invoke(row);
                AlwaysEffectApplier.RevertEffect(row); // 효과 해제
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

            // 신규 활성화 — 구독자가 타입/ID 기반으로 처리
            OnEventActivated?.Invoke(row);
            ShowAlwaysEventPopup(row); // 팝업 표시 (roster 타입은 내부에서 스킵)
            Debug.Log($"[AlwaysEvent] Activated: {id} ({today:yyyy-MM-dd}) | type={row.type} | effect={row.effectId}");
        }
    }

    // roster 타입은 RecruitmentManager가 처리하므로 스킵
    // 확인 버튼만 표시 — 확인 시 AlwaysEffectApplier.ApplyEffect() 호출
    private void ShowAlwaysEventPopup(AlwaysEventRow row)
    {
        if (row.type == "roster") return;

        // description이 없으면 팝업 없이 효과만 적용
        // if (string.IsNullOrEmpty(row.description))
        // {
        //     AlwaysEffectApplier.ApplyEffect(row);
        //     return;
        // }

        AlwaysEventRow capturedRow = row;
        Action onConfirm = () =>
        {
            AlwaysEffectApplier.ApplyEffect(capturedRow);

            // 방학 이벤트 확인 시 토너먼트 씬 진입을 기존 GameManager 로직으로 처리
            if (!IsLeagueBreakEvent(capturedRow))
                return;

            if (!GameManager.Instance.TryEnterTournament())
                Debug.Log("[AlwaysEvent] 토너먼트 진입 조건이 충족되지 않아 진입을 건너뜁니다.");
        };

        if (UIManager.Instance == null)
        {
            onConfirm.Invoke();
            return;
        }

        string title = row.type switch
        {
            "exam" => "시험 기간",
            "festival" => "학교 행사",
            "vacation" => "방학",
            "holiday" => "공휴일",
            _ => "이벤트 발생"
        };

        ConfirmPopupRequest request = new(
            title: title,
            message: row.description,
            primaryLabel: "확인",
            primaryAction: onConfirm
        );

        if (IsLeagueBreakEvent(row))
            request.SetModal(false).SetInvokeConfirmOnClose(true);

        UIManager.Instance.ShowConfirm(request);
    }

    // 다음 리그(vacation 타입) 시작 날짜 조회 — GameManager가 D-Day 계산에 사용
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
    {
        string id = string.IsNullOrWhiteSpace(row.id) ? "(no-id)" : row.id.Trim();
        string start = string.IsNullOrWhiteSpace(row.termStart) ? "" : row.termStart.Trim();
        return $"{id}_{start}"; // 예: roster_recruit_260302, roster_recruit_260810
    }

    private bool TryGetAlwaysEventTable(out AlwaysEventTableSO table)
    {
        table = CachedSOData.AlwaysEventTable;
        if (table != null && table.Rows != null && table.Rows.Count > 0)
            return true;

        Debug.LogWarning("[AlwaysEventManager] CachedSOData.AlwaysEventTable이 비어 있어 AlwaysEvent 처리를 건너 뜀.");
        return false;
    }

    public static bool IsLeagueBreakEvent(AlwaysEventRow row)
        => row.id == "summer_break" || row.id == "winter_break";

    private static bool TryParseTableDate(string value, out DateTime date)
        => AlwaysEventDateUtil.TryParseTableDate(value, out date);
}
