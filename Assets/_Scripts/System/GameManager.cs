using System;
using UnityEngine;
using UnityEngine.SceneManagement;

public class GameManager : Singleton<GameManager>
{
    private const string LobbyScene = "Lobby";
    private const string TournamentScene = "Tournament";
    private const string TitleScene = "Title";

    private TurnManager _turnManager;       // Lobby 씬의 TurnManager (씬별 런타임 참조)
    private EventManager _eventManager;     // Lobby 씬의 EventManager
    private LobbyUI _lobbyUI;               // Lobby 씬의 LobbyUI
    private GameState _gameState;           // 이벤트 시스템용 게임 상태
    private bool _isLoadingTournament;      // 토너먼트 씬 로딩 중 플래그

    private GameFlowData _flowData = GameFlowData.Default;
    private TournamentData _tournamentData = TournamentData.Default;

    // Property
    public bool HasFlowState => _flowData.HasFlowState;
    public DateTime CurrentDate => _flowData.CurrentDate;
    public int TurnIndex => _flowData.TurnIndex;
    public int DayIndex => _flowData.DayIndex;
    public int CurrentYear => _flowData.CurrentYear;
    public GamePhase CurrentPhase => _flowData.Phase;
    public bool IsLeagueOpened => _flowData.IsLeagueOpened;
    public bool IsLeagueHandled => _flowData.IsLeagueHandled;

    protected override void OnSingletonAwake()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    void Start()
    {
        TryInitializeLobbyFlow(SceneManager.GetActiveScene());
    }

    void OnDestroy()
    {
        UnsubscribeTurnManager();
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (scene.name == TitleScene)
            CleanupManagers();

        TryInitializeLobbyFlow(scene);
    }

    // Title 씬 복귀 시 매니저 정리
    private void CleanupManagers()
    {
        if (StudentManager.Instance != null)
        {
            StudentManager.Instance.Cleanup();
            // 저장 로직도 여기서 한번에 하고, wantsToQuit 같은 곳에서 저장 처리하게 하는 방법도 있을듯?
            // 우선 저장은 세부 명세가 없으니 큰 그림만 고려해두기
        }

        ClearFlowRuntimeState();
    }

    // 새 게임 시작 시 모든 상태 초기화 => 타이틀에서 새게임시 실행해야할 듯
    public void StartNewGame()
    {
        StudentFactory.ResetUsedNames();
        StudentFactory.ResetStudentIdCounter();

        if (StudentManager.Instance != null)
            StudentManager.Instance.ClearAllStudents();

        ClearFlowRuntimeState();
    }

    // 로비에서 턴 실행 요청
    public bool TryExecuteLobbyTurn(TurnActionType action)
    {
        if (_turnManager == null || _isLoadingTournament)
            return false;

        if (_turnManager.IsTurnRunning)
            return false;

        _turnManager.ExecuteTurn(action);
        return true;
    }

    // TurnManager 상태를 GameFlowData에 동기화
    public void SyncFlowState(DateTime currentDate, int turnIndex, int dayIndex, int currentYear, GamePhase phase, bool isLeagueOpened, bool isLeagueHandled)
    {
        _flowData.Sync(currentDate, turnIndex, dayIndex, currentYear, phase, isLeagueOpened, isLeagueHandled);
    }

    // 씬별 런타임 참조 및 상태 데이터 초기화
    public void ClearFlowRuntimeState()
    {
        UnsubscribeTurnManager();

        _turnManager = null;
        _eventManager = null;
        _lobbyUI = null;
        _gameState = null;
        _isLoadingTournament = false;

        _flowData.Clear();
        _tournamentData.Clear();
    }

    public void OpenLeague()
    {
        _flowData.IsLeagueOpened = true;
        Debug.Log("[GameManager] 리그가 오픈되었습니다.");
    }

    // Tournament 씬에서 우승팀 결과 저장
    public void SetPendingTournamentChampion(string champion)
    {
        _tournamentData.SetChampion(champion);
    }

    // Lobby 씬에서 우승팀 결과 소비 (한 번만 읽고 클리어)
    public bool TryConsumePendingTournamentChampion(out string champion)
    {
        return _tournamentData.TryConsumeChampion(out champion);
    }

    // 토너먼트 씬 진입 조건 확인 및 씬 전환
    private void TryEnterTournament()
    {
        if (_isLoadingTournament || _flowData.IsLeagueHandled || _turnManager == null)
            return;

        bool shouldEnterTournament = _flowData.IsLeagueOpened || IsTournamentDateReached();
        if (!shouldEnterTournament)
            return;

        _flowData.IsLeagueHandled = true;
        _turnManager.SetPhase(GamePhase.MatchInProgress);
        SyncFlowStateFromLobby();

        _isLoadingTournament = true;
        SceneManager.LoadScene(TournamentScene);
    }

    // 토너먼트 시작 날짜 도달 여부 확인
    private bool IsTournamentDateReached()
    {
        if (_eventManager == null || _turnManager == null)
            return false;

        if (!_eventManager.TryGetNextLeagueDate(_turnManager.DateManager.CurrentDate, out DateTime nextLeagueDate))
            return false;

        return _turnManager.DateManager.CurrentDate.Date >= nextLeagueDate.Date;
    }

    // 다음 토너먼트까지 남은 일수 계산
    private int GetTournamentDday()
    {
        if (_eventManager == null || _turnManager == null)
            return -1;

        if (!_eventManager.TryGetNextLeagueDate(_turnManager.DateManager.CurrentDate, out DateTime nextLeagueDate))
            return -1;

        return (nextLeagueDate.Date - _turnManager.DateManager.CurrentDate.Date).Days;
    }

    // Lobby 씬 로드 시 턴 흐름 초기화/복원 (9단계)
    private void TryInitializeLobbyFlow(Scene scene)
    {
        if (!scene.IsValid() || scene.name != LobbyScene)
            return;

        CacheSceneReferences();         // 1. 씬 오브젝트 참조 캐싱
        SubscribeTurnManager();         // 2. TurnManager 이벤트 구독
        RestoreTurnManagerState();      // 3. TurnManager 상태 복원 (씬 복귀 시)
        InitializeGameState();          // 4. GameState 생성 및 동기화
        InitializeEventManager();       // 5. EventManager 초기화
        SetInitialPhase();              // 6. 초기 페이즈 설정
        HandleTournamentResult();       // 7. 토너먼트 결과 처리
        SyncFlowStateFromLobby();       // 8. GameFlowData 동기화
        RefreshLobbyTopInfo();          // 9. 로비 UI 갱신
    }

    // Lobby 씬 오브젝트 참조 캐싱
    private void CacheSceneReferences()
    {
        _turnManager = FindFirstObjectByType<TurnManager>();
        _eventManager = FindFirstObjectByType<EventManager>();
        _lobbyUI = FindFirstObjectByType<LobbyUI>();
        _isLoadingTournament = false;
    }

    // TurnManager 이벤트 구독
    private void SubscribeTurnManager()
    {
        if (_turnManager == null) return;

        _turnManager.OnTurnCompleted -= HandleTurnCompleted;
        _turnManager.OnTurnCompleted += HandleTurnCompleted;
    }

    // TurnManager 이벤트 구독 해제
    private void UnsubscribeTurnManager()
    {
        if (_turnManager != null)
            _turnManager.OnTurnCompleted -= HandleTurnCompleted;
    }

    // 씬 복귀 시 TurnManager 상태 복원
    private void RestoreTurnManagerState()
    {
        if (_turnManager == null || !_flowData.HasFlowState)
            return;

        _turnManager.RestoreRuntimeState(
            _flowData.CurrentDate,
            _flowData.TurnIndex,
            _flowData.DayIndex,
            _flowData.CurrentYear,
            _flowData.Phase
        );
    }

    // GameState 초기화 / 현재 상태 동기화
    private void InitializeGameState()
    {
        if (_turnManager == null) return;

        _gameState = new GameState(_turnManager.DateManager.CurrentDate);
        _gameState.SyncState(_turnManager.DateManager.CurrentDate, _turnManager.TurnIndex);
    }

    // EventManager 초기화 (씬 복귀 시 이벤트 런타임 상태 리셋 여부 전달)
    private void InitializeEventManager()
    {
        if (_eventManager == null) return;

        _eventManager.Initialize(_gameState, resetRuntimeState: !_flowData.HasFlowState);
    }

    // 초기 게임 페이즈 설정 (Init이면 DailyTraining으로 전환)
    private void SetInitialPhase()
    {
        if (_turnManager == null) return;

        if (_turnManager.CurrentPhase == GamePhase.Init)
            _turnManager.SetPhase(GamePhase.DailyTraining);
    }

    // 토너먼트 결과 처리 (우승팀 표시 및 페이즈 복원)
    private void HandleTournamentResult()
    {
        if (!TryConsumePendingTournamentChampion(out string champion))
            return;

        if (_turnManager != null)
        {
            // 토너먼트가 끝난 날은 별도 액션 없이 하루 경과 처리
            _turnManager.SetPhase(GamePhase.DailyTraining);
            _turnManager.ExecuteTurn(TurnActionType.Rest);
            _turnManager.SetPhase(GamePhase.DailyTraining);
        }

        if (_lobbyUI != null)
            _lobbyUI.SetStatusMessage($"{champion} 우승. 토너먼트 종료.");
    }

    // 턴 완료 시 호출되는 이벤트 핸들러
    private void HandleTurnCompleted(TurnContext context)
    {
        if (_turnManager == null)
            return;

        _gameState?.SyncState(_turnManager.DateManager.CurrentDate, _turnManager.TurnIndex);

        if (_eventManager != null)
            _eventManager.CheckEvents();

        SyncFlowStateFromLobby();
        RefreshLobbyTopInfo();
        TryEnterTournament();
    }

    // Lobby 씬의 TurnManager 상태를 GameFlowData에 동기화
    private void SyncFlowStateFromLobby()
    {
        if (_turnManager == null)
            return;

        SyncFlowState(
            _turnManager.DateManager.CurrentDate,
            _turnManager.TurnIndex,
            _turnManager.DateManager.DayIndex,
            _turnManager.DateManager.CurrentYear,
            _turnManager.CurrentPhase,
            _flowData.IsLeagueOpened,
            _flowData.IsLeagueHandled
        );
    }

    // 로비 UI 상단 정보 갱신 (날짜 / D-Day)
    private void RefreshLobbyTopInfo()
    {
        if (_lobbyUI == null || _turnManager == null)
            return;

        int dDay = GetTournamentDday();
        _lobbyUI.UpdateDateAndDday(_turnManager.DateManager.CurrentDate, dDay);
    }
}
