using UnityEngine;

// 로비 주말 분기(친선 우선/주말훈련) 전담
public class LobbyWeekendManager
{
    private const int WeekendTrainingConfirmIndex = 901;
    private const int WeekendTrainingCancelIndex = 902;

    private GameManager _gameManager;
    private TurnManager _turnManager;
    private LobbyMatchManager _lobbyMatchManager;
    private TrainingFlowController _trainingFlowController;

    // GameManager에서 현재 로비 참조 주입
    public void Bind(GameManager gameManager, TurnManager turnManager, LobbyMatchManager lobbyMatchManager)
    {
        _gameManager = gameManager;
        _turnManager = turnManager;
        _lobbyMatchManager = lobbyMatchManager;
        _trainingFlowController = Object.FindFirstObjectByType<TrainingFlowController>(FindObjectsInactive.Include);
    }

    // 씬 이탈 시 내부 참조 정리
    public void ClearRuntimeState()
    {
        if (_trainingFlowController != null)
            _trainingFlowController.OnFlowComplete -= HandleWeekendTrainingFlowComplete;

        _gameManager = null;
        _turnManager = null;
        _lobbyMatchManager = null;
        _trainingFlowController = null;
    }

    // 금요일 턴 종료 후 친선경기 or 주말훈련 팝업
    public void HandleFridayEnd()
    {
        if (_lobbyMatchManager != null && _lobbyMatchManager.TryShowFriendlyMatchEntryPopup())
            return;

        UIPopupRequest req = UIPopupRequest.Default(
            title: "주말 훈련 제안",
            message: "금요일 일정이 끝났습니다.\n주말 훈련을 진행하시겠습니까?",
            previewImageId: AlwaysEventImageIds.Weekend,
            onPrimary: OnWeekendTrainingConfirmed,
            onCancel: OnWeekendTrainingCancelled,
            subMessage: "확인: 전원 스탯 소량 상승, 주말 휴식 효율 50%\n취소: 주말 푹 쉬기 (체력 대폭 회복)",
            showCancel: true
        );

        UIManager.Instance.ShowPopup(req);
    }

    // 주말 훈련 확인 시 주말 훈련 row 실행
    private void OnWeekendTrainingConfirmed()
    {
        ExecuteWeekendTrainingFlow(WeekendTrainingConfirmIndex, "주말 훈련");
    }

    // 주말 훈련 취소 시 주말 휴식 row 실행
    private void OnWeekendTrainingCancelled()
    {
        ExecuteWeekendTrainingFlow(WeekendTrainingCancelIndex, "주말 휴식");
    }

    // 토·일 2일을 건너뛰어 월요일로 이동
    private void SkipWeekendToMonday()
    {
        _turnManager.SkipDays(2);
        _gameManager.SyncFlowStateFromLobby();
        _gameManager.RefreshLobbyTopInfo();
    }

    // WeekendTrainingTable row 효과를 전체 학생에게 적용
    private static void ApplyWeekendTrainingEffect(int rowIndex)
    {
        WeekendTrainingTableSO table = CachedSOData.Get<WeekendTrainingTableSO>();
        WeekendTrainingRow row = FindWeekendTrainingRow(table, rowIndex);

        StudentManager.Instance.ApplyWeekendTrainingEffect(row);
        Debug.Log($"[LobbyWeekendManager] 주말 효과 적용 완료 index={rowIndex}");
    }

    // row index에 맞는 WeekendTrainingRow를 탐색
    private static WeekendTrainingRow FindWeekendTrainingRow(WeekendTrainingTableSO table, int rowIndex)
    {
        for (int i = 0; i < table.Rows.Count; i++)
        {
            WeekendTrainingRow row = table.Rows[i];
            if (row != null && row.index == rowIndex)
                return row;
        }

        return null;
    }

    // TrainingFlowController를 통해 주말 훈련/휴식 연출 실행
    private void ExecuteWeekendTrainingFlow(int rowIndex, string trainingName)
    {
        if (_trainingFlowController == null)
            _trainingFlowController = Object.FindFirstObjectByType<TrainingFlowController>(FindObjectsInactive.Include);

        if (_trainingFlowController == null)
        {
            ApplyWeekendTrainingEffect(rowIndex);
            SkipWeekendToMonday();
            return;
        }

        _trainingFlowController.OnFlowComplete -= HandleWeekendTrainingFlowComplete;
        _trainingFlowController.OnFlowComplete += HandleWeekendTrainingFlowComplete;

        string backgroundImageId = _trainingFlowController.GetWeekendBgImageId(rowIndex);
        string resultImageId = _trainingFlowController.GetWeekendResultImageId(rowIndex);

        _trainingFlowController.Execute(
            trainingKey: $"weekend_{rowIndex}",
            trainingName: trainingName,
            students: StudentManager.Instance.Students,
            applyEffect: (_, __) => ApplyWeekendTrainingEffect(rowIndex),
            backgroundImageId: backgroundImageId,
            resultImageId: resultImageId
        );
    }

    // 주말 연출이 끝나면 월요일로
    private void HandleWeekendTrainingFlowComplete()
    {
        if (_trainingFlowController != null)
            _trainingFlowController.OnFlowComplete -= HandleWeekendTrainingFlowComplete;

        SkipWeekendToMonday();
    }
}
