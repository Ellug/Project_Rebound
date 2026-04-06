using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

// 로비에서 토너먼트/친선전 진입과 결과 표시 흐름을 담당한다.
// 인풋 0 테스트용 코드 빼면 얘도 실질적으로 모노비해비어 필요 없어짐. 뺀 뒤에 제거 고려
public class LobbyMatchManager : MonoBehaviour
{
    private const string LobbyScene = "Lobby";
    private const string TournamentScene = "Tournament";
    private const int FriendlyMatchWinRewardId = 100;
    private const string DefaultFriendlyOpponentName = "친선고등학교";

    private static readonly AbnormalStatusEffect _abnormalStatusEffect = new();

    private GameManager _gameManager;
    private TurnManager _turnManager;
    private LobbyUI _lobbyUI;
    private TournamentResultUI _tournamentResultUI;
    private bool _isLoadingTournament;

    public bool IsLoadingTournament => _isLoadingTournament;

    void Update()
    {
#if UNITY_EDITOR
        HandleDebugFriendlyMatchInput();
#endif
    }

    // GameManager에서 현재 로비 참조 주입
    public void Bind(GameManager gameManager, TurnManager turnManager, LobbyUI lobbyUI)
    {
        _gameManager = gameManager;
        _turnManager = turnManager;
        _lobbyUI = lobbyUI;
        _tournamentResultUI = FindFirstObjectByType<TournamentResultUI>(FindObjectsInactive.Include);
        _isLoadingTournament = false;
    }

    // 상대 학교명을 정규화해 친선전 예약
    public void ScheduleFriendlyMatch(DateTime matchDate, string opponentName)
    {
        _gameManager.ScheduleFriendlyMatch(matchDate, NormalizeOpponentName(opponentName));
    }

    // 학교 테이블 랜덤 상대명으로 친선전 예약
    public void ScheduleRandomFriendlyMatch(DateTime matchDate)
    {
        ScheduleFriendlyMatch(matchDate, GetRandomOpponentName());
    }

#if UNITY_EDITOR
    // 테스트용: 0 키 입력으로 친선전 예약
    private void HandleDebugFriendlyMatchInput()
    {
        if (SceneManager.GetActiveScene().name != LobbyScene)
            return;

        Keyboard keyboard = Keyboard.current;
        if (keyboard == null) return;

        bool pressedZero = keyboard.digit0Key.wasPressedThisFrame || keyboard.numpad0Key.wasPressedThisFrame;
        if (!pressedZero)
            return;

        DateTime matchDate = _turnManager != null
            ? _turnManager.DateManager.CurrentDate.Date
            : (_gameManager != null && _gameManager.CurrentDate != default ? _gameManager.CurrentDate.Date : DateTime.Today);

        ScheduleRandomFriendlyMatch(matchDate);
        Debug.Log($"[LobbyMatchManager] 디버그 입력으로 친선전 예약: {matchDate:yyyy-MM-dd} / {_gameManager?.FriendlyOpponentName}");
    }
#endif

    // 씬 이탈 시 내부 참조를 정리
    public void ClearRuntimeState()
    {
        _gameManager = null;
        _turnManager = null;
        _lobbyUI = null;
        _tournamentResultUI = null;
        _isLoadingTournament = false;
    }

    // 방학 이벤트에서 호출되는 토너먼트 진입 API
    public bool TryEnterTournament()
    {
        if (!CanEnterTournament()) return false;

        EnterTournament();
        return true;
    }

    // 방학 확인 후 노출되는 토너먼트 진입 확인 팝업
    public bool TryShowTournamentEntryPopup()
    {
        if (!CanEnterTournament()) return false;

        var req = UIPopupRequest.Default(
            title: "토너먼트",
            message: "방학 토너먼트에 진입합니다.\n학생 배치를 완료하면 토너먼트가 시작됩니다.",
            previewImageId: AlwaysEventImageIds.Tournament,
            onPrimary: EnterTournament,
            onCancel: null,
            showCancel: false
        );

        UIPopup popup = UIManager.Instance.ShowPopup(req);
        if (popup != null)
            popup.DisableBackKey = true;

        return true;
    }

    // 로비 초기화 직후 대기 중인 토너먼트/친선 결과 출력
    public void HandlePendingResults()
    {
        HandleTournamentResult();
        HandleFriendlyMatchResult();

        // 엔딩 판정
        if (GameManager.Instance != null && GameManager.Instance.TryTriggerEnding())
        {
            // 엔딩 시퀀스가 시작됨 → 이후 일반 로비 복귀 흐름을 모두 건너뜀
            return;
        }
    }

    // 금요일 종료 시 친선전 예약이 있으면 진입 팝업
    public bool TryShowFriendlyMatchEntryPopup()
    {
        if (!_gameManager.HasPendingFriendlyMatch) return false;

        var req = UIPopupRequest.Default(
            title: "친선경기",
            message: "이번 주말 친선경기가 예정되어 있습니다.\n친선경기에 진입합니다.",
            previewImageId: AlwaysEventImageIds.Tournament,
            onPrimary: EnterFriendlyMatch,
            onCancel: null,
            showCancel: false
        );

        UIPopup popup = UIManager.Instance.ShowPopup(req);
        if (popup != null)
            popup.DisableBackKey = true;

        return true;
    }

    private bool CanEnterTournament()
    {
        if (_isLoadingTournament || _gameManager.IsLeagueHandled) return false;
        if (!_gameManager.IsLeagueOpened) return false;
        if (_gameManager.LeagueTermEnd == default) return false;

        DateTime today = _turnManager.DateManager.CurrentDate.Date;
        if (today > _gameManager.LeagueTermEnd.Date)
        {
            _gameManager.ResetLeagueWindowState();
            return false;
        }

        return true;
    }

    // 학생 배치 팝업을 열고 배치 완료 시 실제 씬 전환 수행
    private void EnterTournament()
    {
        _lobbyUI.OpenStudentManagementPopupForTournament(ProceedToTournament);
    }

    // Tournament 씬을 일반 토너먼트 모드 진입
    private void ProceedToTournament()
    {
        if (!HasEnoughMatchEntryPlayers(out string reason))
        {
            HandleTournamentEntryForfeit(reason);
            return;
        }

        TournamentSceneBridge.RequestTournament();
        _gameManager.MarkLeagueHandled();
        _turnManager.SetPhase(GamePhase.MatchInProgress);
        _gameManager.SyncFlowStateFromLobby();

        _isLoadingTournament = true;
        SceneTransitionManager.Instance.LoadScene(TournamentScene);
    }

    // 친선전 예약이 있으면 단일 경기 모드로 토너먼트 씬 진입
    private void EnterFriendlyMatch()
    {
        string opponentName = NormalizeOpponentName(_gameManager.FriendlyOpponentName);

        if (!HasEnoughMatchEntryPlayers(out string reason))
        {
            HandleFriendlyMatchEntryForfeit(opponentName, reason);
            return;
        }

        TournamentSceneBridge.RequestFriendlyMatch(opponentName);
        _gameManager.ClearFriendlyMatchSchedule();
        _gameManager.SyncFlowStateFromLobby();

        _isLoadingTournament = true;
        SceneTransitionManager.Instance.LoadScene(TournamentScene);
    }

    // 토너먼트 결과를 소비하고 결과 UI 출력
    private void HandleTournamentResult()
    {
        if (!_gameManager.TryConsumePendingTournamentResult(out TournamentData tournamentResultData))
            return;

        DateTime leagueTermEnd = _gameManager.LeagueTermEnd;
        if (leagueTermEnd != default)
        {
            int dayDelta = (int)(leagueTermEnd - _turnManager.DateManager.CurrentDate.Date).TotalDays;
            int targetDayIndex = _turnManager.DateManager.DayIndex + dayDelta;
            _turnManager.RestoreRuntimeState(leagueTermEnd, _turnManager.TurnIndex, targetDayIndex, _turnManager.DateManager.CurrentYear, GamePhase.DailyTraining);
        }

        _turnManager.SetPhase(GamePhase.DailyTraining);
        _gameManager.ResetLeagueWindowState();

        // 결과창 띄우기 전에 보상 먼저 지급
        _tournamentResultUI.ApplyRewardBeforeShow(tournamentResultData);

        // 결과 소비 + 날짜 전진 완료 상태를 즉시 저장
        SaveManager.Instance?.AutoSaveByBranch("토너먼트 결과 처리 완료");

        _tournamentResultUI.ShowResult(tournamentResultData);
    }

    // 친선전 결과를 소비하고 토너먼트 결과 UI를 같은 패널로 출력
    private void HandleFriendlyMatchResult()
    {
        if (!_gameManager.TryConsumePendingFriendlyMatchResult(out bool didWin, out string opponentName))
            return;

        string normalizedOpponent = NormalizeOpponentName(opponentName);

        if (_turnManager != null)
        {
            _turnManager.SkipDays(2);
            _gameManager.SyncFlowStateFromLobby();
            _gameManager.RefreshLobbyTopInfo();
        }

        _tournamentResultUI.ShowFriendlyResult(didWin, normalizedOpponent, FriendlyMatchWinRewardId);
    }

    // 값이 비어 있으면 테이블에서 랜덤 상대명을 뽑는다.
    private static string NormalizeOpponentName(string opponentName)
    {
        return string.IsNullOrWhiteSpace(opponentName)
            ? GetRandomOpponentName()
            : opponentName.Trim();
    }

    // 상태이상 제외 출전 가능 인원이 5명 이상인지 확인
    private static bool HasEnoughMatchEntryPlayers(out string reason)
    {
        reason = string.Empty;

        int totalCount = 0;
        int blockedCount = 0;

        foreach (Student student in StudentManager.Instance.Students)
        {
            if (student == null) continue;

            totalCount++;

            if (_abnormalStatusEffect.IsMatchBlocked(student))
                blockedCount++;
        }

        int availableCount = totalCount - blockedCount;
        if (availableCount >= 5)
            return true;

        reason = $"출전 가능 인원 부족 (전체 {totalCount}명 / 상태이상 {blockedCount}명 / 출전 가능 {availableCount}명)";
        return false;
    }

    // 토너먼트 시작 불가 시 안내 팝업 후 로비 패배 결과 팝업으로 전환
    private void HandleTournamentEntryForfeit(string reason)
    {
        Debug.LogWarning($"[LobbyMatchManager] 토너먼트 진입 불가: {reason} - 실격패 안내 팝업을 표시합니다.");

        void proceedDefeatFlow()
        {
            _gameManager.MarkLeagueHandled();
            _gameManager.SetPendingTournamentResult(32);
            HandleTournamentResult();
        }

        if (UIManager.Instance == null)
        {
            proceedDefeatFlow();
            return;
        }

        string message = $"출전 가능 인원이 부족하여 실격패 처리됩니다.";
        UIPopupRequest req = UIPopupRequest.Default(
            title: "토너먼트",
            message: message,
            previewImageId: AlwaysEventImageIds.Tournament,
            onPrimary: proceedDefeatFlow,
            onCancel: null,
            showCancel: false
        );

        UIPopup popup = UIManager.Instance.ShowPopup(req);
        if (popup != null)
            popup.DisableBackKey = true;
    }

    // 친선전 시작 불가 시 안내 팝업 후 로비 패배 결과 팝업으로 전환
    private void HandleFriendlyMatchEntryForfeit(string opponentName, string reason)
    {
        Debug.LogWarning($"[LobbyMatchManager] 친선전 진입 불가: {reason} - 실격패 안내 팝업을 표시합니다.");

        void proceedDefeatFlow()
        {
            _gameManager.ClearFriendlyMatchSchedule();
            _gameManager.SetPendingFriendlyMatchResult(false, opponentName);
            HandleFriendlyMatchResult();
        }

        if (UIManager.Instance == null)
        {
            proceedDefeatFlow();
            return;
        }

        string message = $"출전 가능 인원이 부족하여 실격패 처리됩니다.";
        UIPopupRequest req = UIPopupRequest.Default(
            title: "친선경기",
            message: message,
            previewImageId: AlwaysEventImageIds.Tournament,
            onPrimary: proceedDefeatFlow,
            onCancel: null,
            showCancel: false
        );

        UIPopup popup = UIManager.Instance.ShowPopup(req);
        if (popup != null)
            popup.DisableBackKey = true;
    }

    // SchoolNameTable에서 친선전 상대 학교명을 랜덤으로 뽑는다.
    private static string GetRandomOpponentName()
    {
        if (!CachedSOData.TryGet<SchoolNameTableSO>(out var schoolTable) || schoolTable.Rows == null || schoolTable.Rows.Count == 0)
            return DefaultFriendlyOpponentName;

        List<string> candidateNames = new(schoolTable.Rows.Count);
        for (int i = 0; i < schoolTable.Rows.Count; i++)
        {
            SchoolNameRow row = schoolTable.Rows[i];
            if (row == null)
                continue;

            string schoolName = (row.name ?? string.Empty).Trim();
            if (string.IsNullOrEmpty(schoolName))
                continue;

            candidateNames.Add(schoolName);
        }

        if (candidateNames.Count == 0)
            return DefaultFriendlyOpponentName;

        return candidateNames[UnityEngine.Random.Range(0, candidateNames.Count)];
    }
}
