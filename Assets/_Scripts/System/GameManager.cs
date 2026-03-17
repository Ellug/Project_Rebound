using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class GameManager : Singleton<GameManager>
{
    private const string LobbyScene = "Lobby";
    private const string TitleScene = "Title";
    private const int PreWinterStoryId = 10002;
    private const int WinterChampionStoryId = 10003;
    private const int PreWinterStoryOffsetMonths = 2;

    private TurnManager _turnManager;               // Lobby 씬의 TurnManager (씬별 런타임 참조)
    private AlwaysEventManager _alwaysEventManager; // Lobby 씬의 AlwaysEventManager
    private LobbyUI _lobbyUI;                       // Lobby 씬의 LobbyUI
    private TournamentResultUI _tournamentResultUI; // Lobby 씬의 TournamentResultUI
    private LobbyMatchManager _lobbyMatchManager;   // Lobby 씬의 매치 흐름 전담 매니저
    private LobbyWeekendManager _lobbyWeekendManager; // Lobby 씬의 주말 흐름 전담 매니저
    private RecruitmentManager _recruitmentManager; // Lobby 씬의 RecruitmentManager
    private bool _initialRecruitmentTriggered;      // 게임 시작 시 최초 영입 트리거 여부 (중복 방지)
    private bool _lobbyInitialized;                 // 로비 씬 초기화 완료 여부 (이중 호출 방지)
    private bool _isNewGame;                        // 새 게임 여부 (SyncFlowState 실행 전에 판단해야 하므로 별도 보관)
    private DateTime _firstWinterStartDate;         // 테이블 기반 첫 겨울방학 시작일
    private DateTime _firstWinterPreStoryDate;      // 첫 겨울방학 2개월 전 VN 트리거 날짜
    private bool _hasFirstWinterSchedule;           // 첫 겨울방학 일정 조회 성공 여부
    private bool _hasPendingFriendlyMatchResult;    // 로비 복귀 후 친선전 결과 팝업 대기 여부
    private bool _pendingFriendlyMatchDidWin;       // 대기 중인 친선전 승패
    private string _pendingFriendlyOpponentName = string.Empty; // 대기 중인 친선전 상대 학교명

    private GameFlowData _flowData = GameFlowData.Default;
    private TournamentData _tournamentData = TournamentData.Default;

    // Property
    public DateTime CurrentDate => _flowData.CurrentDate;
    public int TurnIndex => _flowData.TurnIndex;
    public int DayIndex => _flowData.DayIndex;
    public int CurrentYear => _flowData.CurrentYear;
    public GamePhase CurrentPhase => _flowData.Phase;
    public bool IsLeagueOpened => _flowData.IsLeagueOpened;
    public bool IsLeagueHandled => _flowData.IsLeagueHandled;

    // SaveManager.CollectFlowData()에서 사용
    public DateTime LeagueTermEnd => _flowData.LeagueTermEnd;
    public HashSet<string> ActiveEventIds => _flowData.ActiveEventIds;
    public bool HasPendingFriendlyMatch => _flowData.HasPendingFriendlyMatch;
    public bool HasPlayedVn10001 => _flowData.HasPlayedVn10001;
    public bool HasPlayedVn10002 => _flowData.HasPlayedVn10002;
    public bool HasPlayedVn10003 => _flowData.HasPlayedVn10003;

    public DateTime FriendlyMatchDate { get; private set; }
    public string FriendlyOpponentName { get; private set; } = string.Empty;
    public bool IsFriendlyMatchConfirmed { get; private set; }

    // MaxRecruitCount는 RecruitmentManager가 관리 — GameFlowData 경유 없이 직접 위임
    public int MaxRecruitCount
    {
        get
        {
            if (_recruitmentManager != null)
                return _recruitmentManager.MaxRecruitCount;
                
            return 0;
        }
    }

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

    // 새 게임 시작 시 학생/팩토리 상태 초기화
    // ClearFlowRuntimeState()와 달리 _isNewGame을 건드리지 않음
    // — TryTriggerInitialRecruitment()가 _isNewGame을 읽어야 하므로
    private void ResetNewGameState()
    {
        StudentFactory.ResetUsedNames();
        StudentFactory.ResetStudentIdCounter();

        if (StudentManager.Instance != null)
            StudentManager.Instance.ClearAllStudents();

        _initialRecruitmentTriggered = false;
    }

    // 로비에서 턴 실행 요청
    public bool TryExecuteLobbyTurn(TurnActionType action)
    {
        if (_turnManager == null || (_lobbyMatchManager != null && _lobbyMatchManager.IsLoadingTournament))
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

    // VN 시청 완료 플래그를 런타임 상태에 반영
    public void MarkVnStoryPlayed(int storyId)
    {
        switch (storyId)
        {
            case 10001:
                _flowData.HasPlayedVn10001 = true;
                break;
            case 10002:
                _flowData.HasPlayedVn10002 = true;
                break;
            case 10003:
                _flowData.HasPlayedVn10003 = true;
                break;
        }
    }

    // 씬별 런타임 참조 및 상태 데이터 초기화
    public void ClearFlowRuntimeState()
    {
        UnsubscribeTurnManager();
        UnbindAlwaysEventManager();
        // 씬 이동 중 남아 있을 수 있는 토너먼트 씬 요청을 초기화한다.
        TournamentSceneBridge.Clear();

        // 영입 완료 이벤트 구독 해제
        if (_recruitmentManager != null)
            _recruitmentManager.OnRecruitmentCompleted -= HandleRecruitmentCompleted;

        _turnManager = null;
        _alwaysEventManager = null;
        _lobbyUI = null;
        _tournamentResultUI = null;
        _lobbyMatchManager?.ClearRuntimeState();
        _lobbyWeekendManager?.ClearRuntimeState();
        _lobbyMatchManager = null;
        _lobbyWeekendManager = null;
        _recruitmentManager = null;
        _lobbyInitialized = false; // 로비 초기화 플래그 리셋
        _isNewGame = false;
        _firstWinterStartDate = default;
        _firstWinterPreStoryDate = default;
        _hasFirstWinterSchedule = false;
        _hasPendingFriendlyMatchResult = false;
        _pendingFriendlyMatchDidWin = false;
        _pendingFriendlyOpponentName = string.Empty;

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
    public void SetPendingTournamentResult(int mySchoolReachedRoundTeamCount)
    {
        _tournamentData.SetResult(mySchoolReachedRoundTeamCount);
    }

    // Lobby 씬에서 토너먼트 결과 전체 소비 (한 번만 읽고 클리어)
    public bool TryConsumePendingTournamentResult(out TournamentData tournamentResultData)
    {
        return _tournamentData.TryConsumeResult(out tournamentResultData);
    }

    // Tournament 씬에서 친선전 결과를 로비 표시용으로 저장
    public void SetPendingFriendlyMatchResult(bool didWin, string opponentName)
    {
        _hasPendingFriendlyMatchResult = true;
        _pendingFriendlyMatchDidWin = didWin;
        _pendingFriendlyOpponentName = string.IsNullOrWhiteSpace(opponentName) ? string.Empty : opponentName.Trim();
    }

    // LobbyMatchManager에서 친선전 결과를 1회 소비
    public bool TryConsumePendingFriendlyMatchResult(out bool didWin, out string opponentName)
    {
        if (!_hasPendingFriendlyMatchResult)
        {
            didWin = false;
            opponentName = string.Empty;
            return false;
        }

        didWin = _pendingFriendlyMatchDidWin;
        opponentName = _pendingFriendlyOpponentName;
        _hasPendingFriendlyMatchResult = false;
        _pendingFriendlyMatchDidWin = false;
        _pendingFriendlyOpponentName = string.Empty;
        return true;
    }

    // 토너먼트 씬 진입 직전에 리그 처리 상태를 완료로 표시한다.
    public void MarkLeagueHandled()
    {
        _flowData.IsLeagueHandled = true;
    }

    // 첫 겨울방학 우승 VN(10003) 진입 조건을 확인하고 씬 전환
    public bool TryEnterFirstWinterChampionStory()
    {
        if (_flowData.HasPlayedVn10003)
            return false;

        if (!TryGetFirstWinterDates(out DateTime firstWinterStart, out DateTime firstWinterEnd))
            return false;

        DateTime today = _flowData.CurrentDate.Date;
        if (today < firstWinterStart || today > firstWinterEnd)
            return false;

        VNBridge.RequestStory(WinterChampionStoryId, LobbyScene);
        SceneManager.LoadScene(VNBridge.VNSceneName);
        return true;
    }

    // AlwaysEventManager 에서 호출하는 토너먼트 진입 API
    public bool TryEnterTournament()
    {
        if (_lobbyMatchManager == null)
            return false;

        return _lobbyMatchManager.TryEnterTournament();
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

    // 첫 겨울방학 시작/종료일과 10002 트리거 날짜를 테이블 기준으로 캐싱
    private void CacheFirstWinterSchedule()
    {
        if (!TryGetFirstWinterDates(out DateTime firstWinterStart, out DateTime firstWinterEnd))
        {
            _hasFirstWinterSchedule = false;
            _firstWinterStartDate = default;
            _firstWinterPreStoryDate = default;
            return;
        }

        _hasFirstWinterSchedule = true;
        _firstWinterStartDate = firstWinterStart;
        _firstWinterPreStoryDate = _firstWinterStartDate.AddMonths(-PreWinterStoryOffsetMonths).Date;
    }

    // 첫 겨울방학 2개월 전 날짜에 10002를 1회 실행
    private bool TryTriggerPreWinterStory()
    {
        if (_turnManager == null) return false;
        if (_flowData.HasPlayedVn10002) return false;

        if (!_hasFirstWinterSchedule)
            CacheFirstWinterSchedule();

        if (!_hasFirstWinterSchedule)
            return false;

        DateTime today = _turnManager.DateManager.CurrentDate.Date;
        if (today < _firstWinterPreStoryDate || today >= _firstWinterStartDate)
            return false;

        VNBridge.RequestStory(PreWinterStoryId, LobbyScene);
        SceneManager.LoadScene(VNBridge.VNSceneName);
        return true;
    }

    // Lobby 씬 로드 시 턴 흐름 초기화/복원
    // _lobbyInitialized 플래그로 Start/OnSceneLoaded 이중 호출 방지
    private void TryInitializeLobbyFlow(Scene scene)
    {
        if (!scene.IsValid() || scene.name != LobbyScene)
            return;

        if (_lobbyInitialized) return;
        _lobbyInitialized = true;

        // SaveManager flowData → _flowData 복원
        RestoreFlowDataFromSave();

        _isNewGame = !_flowData.HasFlowState;

        if (_isNewGame)
        {
            ResetNewGameState();
        }

        CacheSceneReferences();         // 1. 씬 오브젝트 참조 캐싱
        SubscribeTurnManager();         // 2. TurnManager 이벤트 구독
        RegisterTurnModules();          // 3. TurnModule 등록 (AlwaysEffectTickModule 등)
        RestoreTurnManagerState();      // 4. TurnManager 상태 복원 (씬 복귀 시)
        InitializeEventManager();       // 5. EventManager 초기화
        CacheFirstWinterSchedule();     // 6. 첫 겨울방학 일정 캐싱
        SetInitialPhase();              // 7. 초기 페이즈 설정
        _lobbyMatchManager.HandlePendingResults(); // 8. 토너먼트/친선 결과 처리
        SyncFlowStateFromLobby();       // 10. GameFlowData 동기화 (이후 HasFlowState = true)
        RefreshLobbyTopInfo();          // 11. 로비 UI 갱신
        TryTriggerInitialRecruitment(); // 12. 게임 시작 시 최초 영입 트리거
        TryTriggerPreWinterStory();     // 13. 첫 겨울방학 2개월 전 VN 트리거
    }

    // Lobby 씬 오브젝트 참조 캐싱
    private void CacheSceneReferences()
    {
        _turnManager = FindFirstObjectByType<TurnManager>();
        _alwaysEventManager = FindFirstObjectByType<AlwaysEventManager>();
        _lobbyUI = FindFirstObjectByType<LobbyUI>();
        _tournamentResultUI = FindFirstObjectByType<TournamentResultUI>(FindObjectsInactive.Include);
        _lobbyMatchManager = FindFirstObjectByType<LobbyMatchManager>(FindObjectsInactive.Include);
        _lobbyWeekendManager = FindFirstObjectByType<LobbyWeekendManager>(FindObjectsInactive.Include);
        _recruitmentManager = FindFirstObjectByType<RecruitmentManager>(); // 영입 매니저 참조

        _lobbyMatchManager.Bind(this, _turnManager, _lobbyUI, _tournamentResultUI);
        _lobbyWeekendManager.Bind(this, _turnManager, _lobbyMatchManager);

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

    // EventManager 초기화 (ActiveEventIds를 GameFlowData에서 직접 전달 — 씬 전환과 무관하게 유지됨)
    private void InitializeEventManager()
    {
        if (_alwaysEventManager == null) return;

        if (!_flowData.HasFlowState)
            _flowData.ActiveEventIds.Clear();   // 새 게임: 활성 이벤트 초기화

        _alwaysEventManager.Bind(_turnManager, _flowData.ActiveEventIds);

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

    // 게임 시작 시 최초 영입 트리거
    // _isNewGame이 아닌 SaveManager.IsPendingNewGame을 기준으로 판단
    // — _isNewGame은 ClearFlowRuntimeState()에서 false로 리셋되어
    //   이어하기 시에도 영입이 뜨는 버그가 있었음
    // — IsPendingNewGame은 CreateNewGameSlot()에서 true,
    //   LoadSlot()에서 false로 명시적으로 세팅되므로 신뢰도 높음
    private void TryTriggerInitialRecruitment()
    {
        if (_initialRecruitmentTriggered) return;   // 이미 트리거됨

        // SaveManager.IsPendingNewGame이 명시적으로 새 게임임을 보장
        bool isNewGame = SaveManager.Instance != null && SaveManager.Instance.IsPendingNewGame;
        if (!isNewGame) return;

        if (_recruitmentManager == null) return;

        _initialRecruitmentTriggered = true;
        SaveManager.Instance.ConsumePendingNewGameFlag(); // IsPendingNewGame → false 소비
        _recruitmentManager.TriggerInitialRecruitment();
    }

    // 영입 완료 시 호출 — 최초 영입 트리거된 경우에만 슬롯 자동 배치
    // _isNewGame 대신 _initialRecruitmentTriggered 사용
    // — 영입 완료 시점에 _isNewGame이 이미 false로 바뀌어 있을 수 있으므로
    private void HandleRecruitmentCompleted(List<Student> recruits)
    {
        if (!_initialRecruitmentTriggered) return;

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

        SyncFlowStateFromLobby();
        RefreshLobbyTopInfo();

        if (TryTriggerPreWinterStory())
            return;

        // 금요일 종료 시 주말 분기 처리
        if (context.IsFriday)
            _lobbyWeekendManager.HandleFridayEnd();
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

    // AlwaysEventTable에서 첫 겨울방학 termStart/termEnd를 조회
    private static bool TryGetFirstWinterDates(out DateTime termStartDate, out DateTime termEndDate)
    {
        return AlwaysEventDateUtil.TryGetFirstWinterVacationTerm(out termStartDate, out termEndDate);
    }

    // Lobby 씬의 TurnManager 상태를 GameFlowData에 동기화
    public void SyncFlowStateFromLobby()
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
    public void RefreshLobbyTopInfo()
    {
        int dDay = GetTournamentDday();
        _lobbyUI.UpdateDateAndDday(_turnManager.DateManager.CurrentDate, dDay);
    }

    // 리그 윈도우 상태 전체 초기화 (만료 / 토너먼트 복귀 후 공통 사용)
    public void ResetLeagueWindowState()
    {
        _flowData.LeagueTermEnd = default;
        _flowData.IsLeagueOpened = false;
        _flowData.IsLeagueHandled = false;
    }

    //친선경기 예약
    public void ScheduleFriendlyMatch(DateTime matchDate, string opponentName)
    {
        FriendlyMatchDate = matchDate.Date;
        FriendlyOpponentName = string.IsNullOrWhiteSpace(opponentName) ? string.Empty : opponentName.Trim();
        IsFriendlyMatchConfirmed = true;
        _flowData.HasPendingFriendlyMatch = true; // 금요일 이후 분기에서 친선전 팝업 띄우기 위함
    }

    //친선경기 해제 
    public void ClearFriendlyMatchSchedule()
    {
        FriendlyMatchDate = default;
        FriendlyOpponentName = string.Empty;
        IsFriendlyMatchConfirmed = false;
        _flowData.HasPendingFriendlyMatch = false; // 친선전 예약이 없으면 주말 훈련 분기로 돌아감
    }

    // SaveManager.CurrentData.flowData → GameManager._flowData 복원
    // 이어하기 로드 시에만 유효. 새 게임 슬롯은 currentDate가 비어 있으므로 자동 스킵됨
    private void RestoreFlowDataFromSave()
    {
        if (SaveManager.Instance == null)
            return;

        SavedFlowData saved = SaveManager.Instance.GetSavedFlowData();
        if (saved == null)
            return;

        _flowData.HasPlayedVn10001 = saved.hasPlayedVn10001;
        _flowData.HasPlayedVn10002 = saved.hasPlayedVn10002;
        _flowData.HasPlayedVn10003 = saved.hasPlayedVn10003;

        // currentDate가 비어 있으면 새 게임 슬롯 → 복원 스킵
        if (string.IsNullOrEmpty(saved.currentDate))
            return;

        DateTime parsedDate = saved.ParseCurrentDate();
        if (parsedDate == default)
            return;

        _flowData.Sync(
            parsedDate,
            saved.turnIndex,
            saved.dayIndex,
            saved.currentYear,
            saved.phase,
            saved.isLeagueOpened,
            saved.isLeagueHandled
        );

        _flowData.LeagueTermEnd = saved.ParseLeagueTermEnd();
        _flowData.HasPendingFriendlyMatch = saved.hasPendingFriendlyMatch;

        _flowData.ActiveEventIds.Clear();
        if (saved.activeEventIds != null)
        {
            foreach (string id in saved.activeEventIds)
                _flowData.ActiveEventIds.Add(id);
        }

        // 친선경기 상세 복원
        DateTime friendlyDate = saved.ParseFriendlyMatchDate();
        FriendlyMatchDate = friendlyDate != default ? friendlyDate.Date : default;
        FriendlyOpponentName = saved.friendlyOpponentName ?? string.Empty;
        IsFriendlyMatchConfirmed = saved.friendlyMatchConfirmed;

        Debug.Log($"[GameManager] flowData 복원: {parsedDate:yyyy-MM-dd}, phase={saved.phase}, events={_flowData.ActiveEventIds.Count}");

        SaveManager.Instance.ApplyLoadedData();
    }
}
