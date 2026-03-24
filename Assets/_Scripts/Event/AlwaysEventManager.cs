using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AlwaysEventManager : MonoBehaviour
{
    // 이벤트가 새로 활성화될 때 발행 — 구독자가 row.type 또는 row.id로 분기 처리
    public event Action<AlwaysEventRow> OnEventActivated;
    // 이벤트가 만료될 때 발행
    public event Action<AlwaysEventRow> OnEventExpired;

    private TurnManager _turnManager;
    private HashSet<string> _activeEventIds; // GameFlowData.ActiveEventIds 참조

    // TurnManager와 연동
    public void Bind(TurnManager turnManager, HashSet<string> activeEventIds)
    {
        UnsubscribeTurnManager();

        _turnManager = turnManager;
        _activeEventIds = activeEventIds;

        SubscribeTurnManager();
    }

    // TurnManager 연동 해제
    public void Unbind()
    {
        UnsubscribeTurnManager();

        _turnManager = null;
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

        AlwaysEventRow capturedRow = row;
        Action onConfirm = () =>
        {
            AlwaysEffectApplier.ApplyEffect(capturedRow);

            // 방학 이벤트는 "토너먼트 확인 팝업 -> 학생 배치 팝업 -> 진입" 순서로 진행
            if (!IsLeagueBreakEvent(capturedRow))
                return;

            StartCoroutine(ShowTournamentEntryPopupNextFrame());
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

        var req = UIPopupRequest.Default(
            title: title,
            message: GetEventDescription(row),
            onPrimary: onConfirm,
            onCancel: null,
            subMessage: GetEventSubMessage(row),
            previewImageId: GetEventPreviewImageId(row),
            showCancel: false,
            primaryKind: UIPopupRequest.PrimaryButtonKind.Confirm
        );

        UIManager.Instance.ShowPopup(req);
    }

    // 방학 팝업이 닫힌 뒤 다음 프레임에 토너먼트 확인 팝업을 띄운다.
    private IEnumerator ShowTournamentEntryPopupNextFrame()
    {
        yield return null;

        if (!GameManager.Instance.TryShowTournamentEntryPopup())
            Debug.Log("[AlwaysEvent] 토너먼트 진입 조건이 충족되지 않아 진입을 건너뜁니다.");
    }

    private static string GetRowId(AlwaysEventRow row)
    {
        string id = string.IsNullOrWhiteSpace(row.id) ? "(no-id)" : row.id.Trim();
        string start = string.IsNullOrWhiteSpace(row.termStart) ? "" : row.termStart.Trim();
        return $"{id}_{start}"; // 예: roster_recruit_260302, roster_recruit_260810
    }

    private bool TryGetAlwaysEventTable(out AlwaysEventTableSO table)
    {
        table = CachedSOData.Get<AlwaysEventTableSO>();
        if (table != null && table.Rows != null && table.Rows.Count > 0)
            return true;

        Debug.LogWarning("[AlwaysEventManager] CachedSOData.AlwaysEventTable이 비어 있어 AlwaysEvent 처리를 건너 뜀.");
        return false;
    }

    public static bool IsLeagueBreakEvent(AlwaysEventRow row) => row.type == "vacation";

    private static bool TryParseTableDate(string value, out DateTime date)
        => AlwaysEventDateUtil.TryParseTableDate(value, out date);


    // name 기반 이벤트 설명 반환 => 디스크립션은 결국 활용 안 하나??
    private static string GetEventDescription(AlwaysEventRow row)
    {
        switch (row.name)
        {
            // 시험 기간
            case "first_midterm_exam":
                return "1학기 중간고사 기간입니다.\n학생들의 컨디션 회복이 더뎌지고, 훈련 효율이 저하됩니다.";

            case "first_final_exam":
                return "1학기 기말고사 기간입니다.\n학생들의 컨디션 회복이 더뎌지고, 훈련 효율이 저하됩니다.";

            case "second_midterm_exam":
                return "2학기 중간고사 기간입니다.\n학생들의 컨디션 회복이 더뎌지고, 훈련 효율이 저하됩니다.";

            case "second_final_exam":
                return "2학기 기말고사 기간입니다.\n학생들의 컨디션 회복이 더뎌지고, 훈련 효율이 저하됩니다.";

            // 학교 행사
            case "festival_sports_day":
                return "체육대회가 열렸습니다!\n오늘은 훈련 대신 행사에 참여합니다.";

            case "festival_school":
                return "학교 축제가 열렸습니다!\n오늘은 훈련 대신 행사에 참여합니다.";

            // 방학 (여름·겨울)
            case "vacation_summer":
                return "여름 방학이 시작됩니다!\n전국 리그가 열리는 기간입니다.\n학생들을 배치하고 토너먼트에 참가하세요.";

            case "vacation_winter":
                return "겨울 방학이 시작됩니다!\n전국 리그가 열리는 기간입니다.\n학생들을 배치하고 토너먼트에 참가하세요.";

            // 공휴일
            case "holiday_children_day":
                return "어린이날입니다! 모처럼의 휴일에 학생들이 들뜬 분위기입니다.";

            case "holiday_buddha":
                return "부처님 오신 날입니다. 차분한 분위기 속에서 하루를 보냅니다.";

            case "holiday_memorial_day":
                return "현충일입니다. 잠시 마음을 다잡고 의미 있는 하루를 보냅니다.";

            case "holiday_liberation_Day":
            case "holiday_liberation_day":
                return "광복절입니다! 활기 넘치는 분위기 속에서 학생들의 사기가 높아집니다.";

            case "holiday_chuseok":
                return "추석 연휴입니다! 명절 분위기로 거리가 활기차게 물들었습니다.";

            case "holiday_foundation_day":
                return "개천절입니다. 공휴일의 여유 속에서 한층 편안하게 훈련합니다.";

            case "holiday_hangul_day":
                return "한글날입니다. 공휴일로 차분한 하루를 보냅니다.";

            case "holiday_christmas":
                return "성탄절입니다! 설레는 분위기 속에서 학생들의 의욕이 높아집니다.";

            case "holiday_independence":
                return "삼일절입니다. 공휴일로 학생들이 의욕으로 가득 찼습니다.";

            default:
                return string.IsNullOrWhiteSpace(row.description) || row.description == "-" ? string.Empty : row.description;
        }
    }

    // type 기반으로 TxtSub에 표시할 효과 요약 문자열을 반환    
    private static string GetEventSubMessage(AlwaysEventRow row)
    {
        return row.type switch
        {
            "exam" => "컨디션 회복량 -4  /  훈련 효율 x0.67",
            "festival" => "훈련 불가  /  컨디션 회복량 +10",
            "vacation" => "토너먼트 진입 가능",
            "holiday" => "컨디션 회복량 +5  /  훈련 효율 x1.5",
            _ => string.Empty
        };
    }

    // type/name 기준으로 이벤트 팝업 이미지 ID 반환
    private static string GetEventPreviewImageId(AlwaysEventRow row)
    {
        switch (row.name)
        {
            // 시험 기간
            case "first_midterm_exam":
            case "first_final_exam":
            case "second_midterm_exam":
            case "second_final_exam":
                return AlwaysEventImageIds.Exam;

            // 학교 행사
            case "festival_sports_day":
                return AlwaysEventImageIds.Tournament;
            case "festival_school":
                return AlwaysEventImageIds.Festival;

            // 방학
            case "vacation_summer":
                return AlwaysEventImageIds.Summer;
            case "vacation_winter":
                return AlwaysEventImageIds.Winter;

            // 공휴일
            case "holiday_children_day":
                return AlwaysEventImageIds.Children;
            case "holiday_buddha":
                return AlwaysEventImageIds.Buddha;
            case "holiday_memorial_day":
                return AlwaysEventImageIds.Memorial;
            case "holiday_liberation_Day":
            case "holiday_liberation_day":
                return AlwaysEventImageIds.Liberation;
            case "holiday_chuseok":
                return AlwaysEventImageIds.Chuseok;
            case "holiday_foundation_day":
                return AlwaysEventImageIds.National;
            case "holiday_hangul_day":
                return AlwaysEventImageIds.Hangul;
            case "holiday_christmas":
                return AlwaysEventImageIds.Christmas;
            case "holiday_independence":
                return AlwaysEventImageIds.Independence;
            default:
                return null;
        }
    }
}
