using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class GameManager : Singleton<GameManager>
{
    private const string LobbyScene = "Lobby";
    private const string TournamentScene = "Tournament";
    private const string TitleScene = "Title";

    private TurnManager _turnManager;               // Lobby 씬의 TurnManager (씬별 런타임 참조)
    private AlwaysEventManager _alwaysEventManager; // Lobby 씬의 AlwaysEventManager
    private LobbyUI _lobbyUI;                       // Lobby 씬의 LobbyUI
    private TournamentResultUI _tournamentResultUI; // Lobby 씬의 TournamentResultUI
    private RecruitmentManager _recruitmentManager; // Lobby 씬의 RecruitmentManager
    private GameState _gameState;                   // 이벤트 시스템용 게임 상태
    private bool _isLoadingTournament;              // 토너먼트 씬 로딩 중 플래그
    private bool _initialRecruitmentTriggered;      // 게임 시작 시 최초 영입 트리거 여부 (중복 방지)
    private bool _lobbyInitialized;                 // 로비 씬 초기화 완료 여부 (이중 호출 방지)
    private bool _isNewGame;                        // 새 게임 여부 (SyncFlowState 실행 전에 판단해야 하므로 별도 보관)

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

        // 다른 씬으로 이동하면 가드를 해제해 다음 Lobby 복귀 때 재초기화
        if (scene.name != LobbyScene)
            _lobbyInitialized = false;

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
    // GameManager가 로비 씬에만 존재하므로 TryInitializeLobbyFlow 내부에서 자동 처리됨
    public void StartNewGame()
    {
        StudentFactory.ResetUsedNames();
        StudentFactory.ResetStudentIdCounter();

        if (StudentManager.Instance != null)
            StudentManager.Instance.ClearAllStudents();

        _initialRecruitmentTriggered = false; // 새 게임 시 영입 트리거 초기화
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
        UnbindAlwaysEventManager();

        // 영입 완료 이벤트 구독 해제
        if (_recruitmentManager != null)
            _recruitmentManager.OnRecruitmentCompleted -= HandleRecruitmentCompleted;

        _turnManager = null;
        _alwaysEventManager = null;
        _lobbyUI = null;
        _tournamentResultUI = null;
        _recruitmentManager = null;
        _gameState = null;
        _isLoadingTournament = false;
        _lobbyInitialized = false; // 로비 초기화 플래그 리셋
        _isNewGame = false;

        _flowData.Clear();
        _tournamentData.Clear();
    }

    public void OpenLeague()
    {
        _flowData.IsLeagueOpened = true;
        _flowData.IsLeagueHandled = false;
        Debug.Log("[GameManager] 리그가 오픈되었습니다.");
    }

    // Tournament 씬에서 토너먼트 결과 저장
    public void SetPendingTournamentResult(string champion, int mySchoolReachedRoundTeamCount)
    {
        _tournamentData.SetResult(champion, mySchoolReachedRoundTeamCount);
    }

    // Lobby 씬에서 토너먼트 결과 전체 소비 (한 번만 읽고 클리어)
    public bool TryConsumePendingTournamentResult(out TournamentData tournamentResultData)
    {
        return _tournamentData.TryConsumeResult(out tournamentResultData);
    }

    // AlwaysEventManager 에서 호출하는 토너먼트 진입 API
    public bool TryEnterTournament()
    {
        if (!CanEnterTournament())
            return false;

        EnterTournament();
        return true;
    }

    // 토너먼트 씬 진입 가능 여부 확인
    private bool CanEnterTournament()
    {
        if (_turnManager == null || _isLoadingTournament || _flowData.IsLeagueHandled)
            return false;

        if (!_flowData.IsLeagueOpened)
            return false;

        if (_flowData.LeagueTermEnd == default)
            return false;

        DateTime today = _turnManager.DateManager.CurrentDate.Date;
        if (today > _flowData.LeagueTermEnd.Date)
        {
            ResetLeagueWindowState();
            return false;
        }

        return true;
    }

    // 토너먼트 씬 진입 처리
    // 학생 관리 팝업을 열고, 팝업 내 배치 완료 버튼으로 씬 전환
    private void EnterTournament()
    {
        if (_lobbyUI == null) return;

        // 학생 관리 팝업에 토너먼트 진입 콜백 주입 후 오픈
        _lobbyUI.OpenStudentManagementPopupForTournament(ProceedToTournament);
    }

    // 실제 토너먼트 씬 전환 처리
    private void ProceedToTournament()
    {
        _flowData.IsLeagueHandled = true;
        _turnManager.SetPhase(GamePhase.MatchInProgress);
        SyncFlowStateFromLobby();

        _isLoadingTournament = true;
        SceneManager.LoadScene(TournamentScene);
    }

    // 다음 토너먼트까지 남은 일수 계산 — CachedSOData를 직접 읽어 AEM 의존 없음
    private int GetTournamentDday()
    {
        if (_turnManager == null)
            return -1;

        if (!AlwaysEventDateUtil.TryGetNextLeagueDate(_turnManager.DateManager.CurrentDate, out DateTime nextLeagueDate))
            return -1;

        return (nextLeagueDate.Date - _turnManager.DateManager.CurrentDate.Date).Days;
    }

    // Lobby 씬 로드 시 턴 흐름 초기화/복원 (11단계)
    // _lobbyInitialized 플래그로 Start/OnSceneLoaded 이중 호출 방지
    private void TryInitializeLobbyFlow(Scene scene)
    {
        if (!scene.IsValid() || scene.name != LobbyScene)
            return;

        if (_lobbyInitialized) return; // 이중 호출 방지
        _lobbyInitialized = true;

        // SyncFlowStateFromLobby 실행 전에 새 게임 여부를 먼저 저장
        _isNewGame = !_flowData.HasFlowState;

        if (_isNewGame)
            ResetNewGameState();

        CacheSceneReferences();         // 1. 씬 오브젝트 참조 캐싱
        SubscribeTurnManager();         // 2. TurnManager 이벤트 구독
        RegisterTurnModules();          // 3. TurnModule 등록 (AlwaysEffectTickModule 등)
        RestoreTurnManagerState();      // 4. TurnManager 상태 복원 (씬 복귀 시)
        InitializeGameState();          // 5. GameState 생성 및 동기화
        InitializeEventManager();       // 6. EventManager 초기화
        SetInitialPhase();              // 7. 초기 페이즈 설정
        HandleTournamentResult();       // 8. 토너먼트 결과 처리
        SyncFlowStateFromLobby();       // 9. GameFlowData 동기화 (이후 HasFlowState = true)
        RefreshLobbyTopInfo();          // 10. 로비 UI 갱신
        TryTriggerInitialRecruitment(); // 11. 게임 시작 시 최초 영입 트리거
    }

    // 새 게임 상태 초기화
    // GameManager가 타이틀 씬에 없으므로 로비 씬 최초 진입 시점에 처리
    private void ResetNewGameState()
    {
        StudentFactory.ResetUsedNames();
        StudentFactory.ResetStudentIdCounter();

        if (StudentManager.Instance != null)
            StudentManager.Instance.ClearAllStudents();

        _initialRecruitmentTriggered = false;
    }

    // Lobby 씬 오브젝트 참조 캐싱
    private void CacheSceneReferences()
    {
        _turnManager = FindFirstObjectByType<TurnManager>();
        _alwaysEventManager = FindFirstObjectByType<AlwaysEventManager>();
        _lobbyUI = FindFirstObjectByType<LobbyUI>();
        _tournamentResultUI = FindFirstObjectByType<TournamentResultUI>(FindObjectsInactive.Include);
        _recruitmentManager = FindFirstObjectByType<RecruitmentManager>(); // 영입 매니저 참조
        _isLoadingTournament = false;

        // 영입 완료 이벤트 구독
        if (_recruitmentManager != null)
        {
            _recruitmentManager.OnRecruitmentCompleted -= HandleRecruitmentCompleted;
            _recruitmentManager.OnRecruitmentCompleted += HandleRecruitmentCompleted;
        }
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

    // ITurnModule 구현체 등록 — SubscribeTurnManager() 직후 호출
    // AlwaysEffectTickModule: 매 턴 종료 시 상시 이벤트 condition 틱 처리
    private void RegisterTurnModules()
    {
        if (_turnManager == null) return;

        AlwaysEffectTickModule tickModule = FindFirstObjectByType<AlwaysEffectTickModule>();
        if (tickModule != null)
            _turnManager.RegisterModule(tickModule);
    }

    // AlwaysEventManager 이벤트 구독 해제 및 Unbind
    private void UnbindAlwaysEventManager()
    {
        if (_alwaysEventManager == null) return;

        _alwaysEventManager.OnEventActivated -= HandleAlwaysEventActivated;
        _alwaysEventManager.OnEventExpired -= HandleAlwaysEventExpired;
        _alwaysEventManager.Unbind();
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

    // EventManager 초기화 (ActiveEventIds를 GameFlowData에서 직접 전달 — 씬 전환과 무관하게 유지됨)
    private void InitializeEventManager()
    {
        if (_alwaysEventManager == null) return;

        if (!_flowData.HasFlowState)
            _flowData.ActiveEventIds.Clear();   // 새 게임: 활성 이벤트 초기화

        _alwaysEventManager.Bind(_turnManager, _gameState, _flowData.ActiveEventIds);

        // AlwaysEventManager가 발행하는 이벤트를 GM이 구독 — AEM → GM 직접참조 제거
        _alwaysEventManager.OnEventActivated += HandleAlwaysEventActivated;
        _alwaysEventManager.OnEventExpired += HandleAlwaysEventExpired;
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
        if (!TryConsumePendingTournamentResult(out TournamentData tournamentResultData))
            return;

        if (_turnManager != null)
        {
            // 저장해둔 term_end 날짜로 복원
            DateTime leagueTermEnd = _flowData.LeagueTermEnd;
            if (leagueTermEnd != default)
            {
                int dayDelta = (int)(leagueTermEnd - _turnManager.DateManager.CurrentDate.Date).TotalDays;
                int targetDayIndex = _turnManager.DateManager.DayIndex + dayDelta;
                _turnManager.RestoreRuntimeState(leagueTermEnd, _turnManager.TurnIndex, targetDayIndex, _turnManager.DateManager.CurrentYear, GamePhase.DailyTraining);
            }
            _turnManager.SetPhase(GamePhase.DailyTraining);
        }
        ResetLeagueWindowState();

        if (_tournamentResultUI != null)
            _tournamentResultUI.ShowResult(tournamentResultData);
    }

    // 게임 시작 시 최초 영입 트리거
    // _isNewGame 플래그로 판단 (SyncFlowStateFromLobby 이후에도 새 게임 여부 유지)
    private void TryTriggerInitialRecruitment()
    {
        if (_initialRecruitmentTriggered) return;   // 이미 트리거됨
        if (!_isNewGame) return;                     // 새 게임이 아님 (씬 복귀)
        if (_recruitmentManager == null) return;

        _initialRecruitmentTriggered = true;
        _recruitmentManager.TriggerInitialRecruitment();
    }

    // 영입 완료 시 호출 — 새 게임 최초 영입인 경우에만 슬롯 자동 배치
    private void HandleRecruitmentCompleted(List<Student> recruits)
    {
        if (!_isNewGame) return;

        AutoAssignStudentsToSlots(recruits);
    }

    // 영입된 학생을 필드 슬롯에 순서대로 자동 배치
    // 초상화(Sprite)는 카드 미생성 시점이므로 null — 팝업 열 때 RestoreSlotAssignments()에서 자동 복원
    private void AutoAssignStudentsToSlots(List<Student> students)
    {
        if (StudentManager.Instance == null) return;
        if (_lobbyUI == null) return;

        List<StudentSlot> fieldSlots = _lobbyUI.GetFieldSlots();
        if (fieldSlots == null || fieldSlots.Count == 0) return;

        int count = Mathf.Min(students.Count, fieldSlots.Count);

        for (int i = 0; i < count; i++)
        {
            StudentSlot slot = fieldSlots[i];
            Student student = students[i];

            if (slot == null || student == null) continue;

            slot.AssignStudent(student, null);
            StudentManager.Instance.AssignSlot(i, student);
        }

        Debug.Log($"[GameManager] 슬롯 자동 배치 완료: {count}명");
    }

    // 턴 완료 시 호출되는 이벤트 핸들러
    private void HandleTurnCompleted(TurnContext context)
    {
        if (_turnManager == null)
            return;

        _gameState?.SyncState(_turnManager.DateManager.CurrentDate, _turnManager.TurnIndex);

        SyncFlowStateFromLobby();
        RefreshLobbyTopInfo();

        // 금요일 종료 시 주말 분기 처리
        if (context.IsFriday)
            HandleFridayEnd();
    }

    // 금요일 턴 종료 후 친선경기 or 주말 훈련 팝업 분기
    private void HandleFridayEnd()
    {
        if (UIManager.Instance == null)
            return;

        if (_flowData.HasPendingFriendlyMatch)
        {
            var req = UIPopupRequest.Default(
                title: "친선경기",
                message: "이번 주말 친선경기가 예정되어 있습니다.\n친선경기에 진입하시겠습니까? (미구현)",
                onPrimary: EnterFriendlyMatch,
                onCancel: () => { },
                showCancel: true
            );

            UIManager.Instance.ShowPopup(req);
        }
        else
        {
            var req = UIPopupRequest.Default(
                title: "주말 훈련 제안",
                message: "금요일 일정이 끝났습니다.\n주말 훈련을 진행하시겠습니까?",
                onPrimary: OnWeekendTrainingConfirmed,
                onCancel: OnWeekendTrainingCancelled,
                subMessage: "확인: 전원 스탯 소량 상승, 주말 휴식 효율 50%\n취소: 주말 푹 쉬기 (체력 대폭 회복)",
                showCancel: true
            );

            UIManager.Instance.ShowPopup(req);
        }
    }

    // 주말 훈련 확인 (훈련 진행)
    private void OnWeekendTrainingConfirmed()
    {
        Debug.Log("[GameManager] 주말 훈련 확인");
    }

    // 주말 훈련 취소 (주말 스킵 → 월요일로)
    private void OnWeekendTrainingCancelled()
    {
        if (_turnManager == null) return;

        // 금요일 기준 토·일 2일 스킵 → 월요일
        _turnManager.SkipDays(2);
        SyncFlowStateFromLobby();
        RefreshLobbyTopInfo();
    }

    // 친선경기 진입 처리 (추후 구현)
    private void EnterFriendlyMatch()
    {
        _flowData.HasPendingFriendlyMatch = false;
        // TODO: 친선경기 씬/흐름 연결
        Debug.Log("[GameManager] 친선경기 진입 (미구현)");
    }

    // AlwaysEventManager가 이벤트 활성화를 알릴 때 호출 — row.type / row.id 기반으로 분기
    private void HandleAlwaysEventActivated(AlwaysEventRow row)
    {
        if (AlwaysEventManager.IsLeagueBreakEvent(row))
        {
            if (AlwaysEventDateUtil.TryParseTableDate(row.termEnd, out DateTime termEnd))
            {
                _flowData.LeagueTermEnd = termEnd.Date;
                OpenLeague();
            }
            else
            {
                Debug.LogWarning($"[GameManager] 리그 term_end 파싱 실패로 리그 오픈을 건너뜁니다. id={row.id}, term_end={row.termEnd}");
            }
        }
    }

    // AlwaysEventManager가 이벤트 만료를 알릴 때 호출
    private void HandleAlwaysEventExpired(AlwaysEventRow row)
    {
        if (!AlwaysEventManager.IsLeagueBreakEvent(row))
            return;

        ResetLeagueWindowState();
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

    // 리그 윈도우 상태 전체 초기화 (만료 / 토너먼트 복귀 후 공통 사용)
    private void ResetLeagueWindowState()
    {
        _flowData.LeagueTermEnd = default;
        _flowData.IsLeagueOpened = false;
        _flowData.IsLeagueHandled = false;
    }
}
