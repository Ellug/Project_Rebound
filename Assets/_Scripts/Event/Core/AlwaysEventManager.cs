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
            //message: row.description,
            message: GetEventDescription(row),
            primaryLabel: "확인",
            primaryAction: onConfirm,
            subMessage: GetEventSubMessage(row)     // TxtSub에 효과 텍스트 출력
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


    // ID 기반 이벤트 설명 반환
    // description이 없는 경우 ID에 따른 기본 설명
    private static string GetEventDescription(AlwaysEventRow row)
    {
        // 시험 기간
        // 효과: 컨디션 매 턴 -4 회복량, 훈련 효율 -1.5배
        switch (row.id)
        {
            case "1st_midexam":
                return "1학기 중간고사 기간입니다.\n학생들의 컨디션 회복이 더뎌지고, 훈련 효율이 저하됩니다.";

            case "1st_finalexam":
                return "1학기 기말고사 기간입니다.\n학생들의 컨디션 회복이 더뎌지고, 훈련 효율이 저하됩니다.";

            case "2nd_midexam":
                return "2학기 중간고사 기간입니다.\n학생들의 컨디션 회복이 더뎌지고, 훈련 효율이 저하됩니다.";

            case "2nd_finalexam":
                return "2학기 기말고사 기간입니다.\n학생들의 컨디션 회복이 더뎌지고, 훈련 효율이 저하됩니다.";

            // 학교 행사
            // 효과: 훈련 불가, 컨디션 회복량 +10
            case "sports_day":
                return "체육대회가 열렸습니다!\n오늘은 훈련 대신 행사에 참여합니다.";

            case "school_event":
                return "학교 축제가 열렸습니다!\n오늘은 훈련 대신 행사에 참여합니다.";

            // 방학 (여름·겨울)
            // 효과: 훈련 불가 (토너먼트 기간)
            case "summer_break":
                return "여름 방학이 시작됩니다!\n전국 리그가 열리는 기간입니다.\n학생들을 배치하고 토너먼트에 참가하세요.";

            case "winter_break":
                return "겨울 방학이 시작됩니다!\n전국 리그가 열리는 기간입니다.\n학생들을 배치하고 토너먼트에 참가하세요.";

            // 공휴일
            // 효과: 컨디션 매 턴 +0, 회복량 +5, 훈련 효율 +1.5배
            case "children_day":
                return "어린이날입니다! 모처럼의 휴일에 학생들이 들뜬 분위기입니다.";

            case "buddha_day":
                return "부처님 오신 날입니다. 차분한 분위기 속에서 하루를 보냅니다.";

            case "korean_memorial_day":
                return "현충일입니다. 잠시 마음을 다잡고 의미 있는 하루를 보냅니다.";

            case "constitution_day":
                return "제헌절입니다. 공휴일의 여유로운 분위기 속에서 훈련을 이어갑니다.";

            case "liberation_day":
                return "광복절입니다! 활기 넘치는 분위기 속에서 학생들의 사기가 높아집니다.";

            case "chuseok_break":
                return "추석 연휴입니다! 명절 분위기로 거리가 활기차게 물들었습니다.";

            case "foundation_day":
                return "개천절입니다. 공휴일의 여유 속에서 한층 편안하게 훈련합니다.";

            case "hangul_day":
                return "한글날입니다. 공휴일로 차분한 하루를 보냅니다.";

            case "christmas":
                return "성탄절입니다! 설레는 분위기 속에서 학생들의 의욕이 높아집니다.";

            case "independence":
                return "삼일절입니다. 공휴일로 학생들이 의욕으로 가득 찼습니다.";

            // 폴백: 알 수 없는 ID
            default:
                // description이 있으면 그대로 사용, 없으면 빈 문자열
                return string.IsNullOrEmpty(row.description) ? string.Empty : row.description;
        }
    }

    // type 기반으로 TxtSub에 표시할 효과 요약 문자열을 반환    
    private static string GetEventSubMessage(AlwaysEventRow row)
    {
        return row.type switch
        {
            "exam" => "컨디션 회복량 -4  /  훈련 효율 ×0.67",
            "festival" => "훈련 불가  /  컨디션 회복량 +10",
            "vacation" => "토너먼트 진입 가능",
            "holiday" => "컨디션 회복량 +5  /  훈련 효율 ×1.5",
            _ => string.Empty
        };
    }
}