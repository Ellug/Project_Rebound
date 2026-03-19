using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class SaveManager : Singleton<SaveManager>
{
    public PlayData CurrentData { get; private set; }
    public UserData CurrentUserData { get; private set; }
    public bool IsPendingNewGame { get; private set; }
    public bool ShouldDeleteCurrentRunOnTitle { get; private set; }

    // 현재 플레이 중인 슬롯 인덱스를 런타임에서 고정
    private int _currentRuntimeSlotIndex = -1;

    public int CurrentSlotIndex => _currentRuntimeSlotIndex;

    private void Start()
    {
        CleanupIncompleteNewGameSlots(); // SaveSystem이 초기화된 이후 실행
    }

    protected override void OnSingletonAwake()
    {
        LoadUserData();

        if (CurrentUserData == null)
        {
            CurrentUserData = new UserData();
        }
    }

    public void LoadSlot(int slotIndex, string sceneName)
    {
        PlayData data = SaveSystem.Instance.Load(slotIndex);
        if (data == null) return;

        CurrentData = data;

        // 로드한 슬롯을 현재 런타임 슬롯으로 고정
        _currentRuntimeSlotIndex = slotIndex;
        CurrentData.slotIndex = slotIndex;

        IsPendingNewGame = false;
        LoadUserData();


        // 돈과 평판은 런타임 매니저가 존재할 때만 적용 (로비 씬에서 먼저 적용, 이후 씬에서도 계속 유지)
        if (MoneyManager.Instance != null)
            MoneyManager.Instance.ApplySaveData(
                data.gold,
                CurrentUserData != null ? CurrentUserData.reputation : data.reputation);

        // 슬롯 로드 시 시설 상태를 먼저 현재 슬롯 데이터 기준으로 맞춤
        if (FacilitySystem.Instance != null)
            ApplyFacilityData(data.facilities);

        // 슬롯 로드 시 메신저 상태도 현재 슬롯 기준으로 먼저 맞춤
        if (MessengerManager.Instance != null)
            MessengerManager.Instance.RestoreSaveData(data.messenger);

        // 토너먼트 진행 중이면 로비를 경유해 토너먼트로 이동
        // (UIManager가 로비에 있으므로 로비를 반드시 거쳐야 함)
        _pendingTournamentRestore = IsTournamentInProgress(data);
        SceneManager.LoadScene(sceneName); // 항상 로비로 먼저 이동
    }

    // 로비 씬 초기화 완료 시점(GameManager 등)에서 호출
    public bool ConsumePendingTournamentRestore()
    {
        if (!_pendingTournamentRestore) return false;
        _pendingTournamentRestore = false;
        return true;
    }

    private bool _pendingTournamentRestore;

    private static bool IsTournamentInProgress(PlayData data)
    {
        bool tournamentInProgress = data.tournament != null && data.tournament.isInProgress;
        bool matchInProgress = data.matchSim != null && data.matchSim.isMatchRunning;
        return tournamentInProgress || matchInProgress;
    }

    // 씬 로드 완료 후 각 매니저에 데이터 적용 (GameManager.ApplySaveDataIfLoaded에서 호출)
    public void ApplyLoadedData()
    {
        if (CurrentData == null)
        {
            Debug.LogWarning("적용할 데이터 없음");
            return;
        }

        ApplyFacilityData(CurrentData.facilities);
        ApplyStudentData(CurrentData.students, CurrentData.slotAssignments);
        ApplyTournamentData(CurrentData.tournament);    // 토너먼트 씬 복원 (로비에선 자동 스킵)
        ApplyMatchSimData(CurrentData.matchSim);        // 경기 시뮬레이션 복원 (토너먼트 씬 외에선 자동 스킵)
        ApplyMessengerData(CurrentData.messenger);      // 메신저 복원
        ApplyActiveEventEffects(CurrentData.flowData);  // 이벤트 activeEffectIds 복원
        RestoreHeadCoachNodesIfPossible();              // 감독 노드 복원 시도 (HeadCoachManager 초기화 여부에 따라 내부에서 처리)

        // HeadCoachManager는 InitFromTable() 완료 이후에 복원 가능
        if (HeadCoachManager.Instance != null && HeadCoachManager.Instance.IsInitialized)
        {
            HeadCoachManager.Instance.RestoreUnlockedNodes(
                CurrentUserData != null && CurrentUserData.unlockedNodeIds != null && CurrentUserData.unlockedNodeIds.Count > 0
                    ? CurrentUserData.unlockedNodeIds
                    : CurrentData.unlockedNodeIds);
        }
        else
        {
            Debug.LogWarning("[SaveManager] HeadCoachManager 초기화 전에 ApplyLoadedData 호출됨. 감독 노드 복원 생략.");
        }

        // 친선경기 매니저는 TurnManager의 날짜 변경 체크에서 Load된 flowData 기준으로 월별 신청 횟수 복원
        if (FriendlyMatchManager.Instance != null && CurrentData.flowData != null)
        {
            FriendlyMatchManager.Instance.RestoreApplyCount(
                CurrentData.flowData.friendlyMatchApplyCount,
                CurrentData.flowData.friendlyMatchLastMonth
            );
        }
    }

    // GameManager가 _flowData 복원에 사용 (RestoreTurnManagerState 이전에 호출)
    public SavedFlowData GetSavedFlowData()
    {
        return CurrentData?.flowData;
    }

    public bool CreateNewGameSlot(string schoolName)
    {
        LoadUserData();

        int slotIndex = SaveSystem.Instance.FindFirstEmptySlotIndex();
        if (slotIndex < 0)
        {
            Debug.LogWarning("[SaveManager] 빈 세이브 슬롯 없음");
            return false;
        }

        CurrentData = new PlayData
        {
            slotIndex = slotIndex,
            school = schoolName,
            playTime = "게임 시작",
            saveTime = DateTime.Now.ToString("yyyy-MM-dd HH:mm"),
            gold = 0,
            reputation = CurrentUserData != null ? CurrentUserData.reputation : 0,
            unlockedNodeIds = CurrentUserData != null
                ? new List<int>(CurrentUserData.unlockedNodeIds)
                : new List<int>(),
            flowData = new SavedFlowData(),
            facilities = new SavedFacilityData(),
            students = new List<SavedStudentData>(),
            slotAssignments = new List<SavedSlotAssignment>(),
            tournament = new SavedTournamentData(),
            messenger = new SavedMessengerData(),
        };

        // 새로 만든 슬롯을 현재 런타임 슬롯으로 고정
        _currentRuntimeSlotIndex = slotIndex;
        CurrentData.slotIndex = slotIndex;

        // 새 게임 생성 시 런타임 시설 상태도 반드시 기본값으로 초기화
        if (FacilitySystem.Instance != null)
        {
            FacilitySystem.Instance.ResetLevelsToDefault();
        }

        // 새 게임 생성 시 메신저 상태도 반드시 초기화
        if (MessengerManager.Instance != null)
        {
            MessengerManager.Instance.ClearAll();
        }

        IsPendingNewGame = true;

        SaveUserData();
        return true;
    }

    public void SaveCurrent()
    {
        if (CurrentData == null)
        {
            Debug.LogWarning("저장할 데이터 없음");
            return;
        }

        // 현재 슬롯이 없으면 다른 빈 슬롯으로 저장되지 않도록 중단
        if (_currentRuntimeSlotIndex < 0)
        {
            Debug.LogWarning("[SaveManager] 현재 슬롯 인덱스가 없어 저장을 중단합니다.");
            return;
        }

        int studentCountBeforeSave = StudentManager.Instance != null
            ? StudentManager.Instance.Students.Count
            : -1;

        if (MoneyManager.Instance != null)
        {
            CurrentData.gold = MoneyManager.Instance.Gold;
            CurrentData.reputation = MoneyManager.Instance.Reputation;
        }

        if (HeadCoachManager.Instance != null && HeadCoachManager.Instance.IsInitialized)
        {
            CurrentData.unlockedNodeIds = HeadCoachManager.Instance.GetUnlockedNodeIds();
            Debug.Log($"[SaveManager] SaveCurrent | slot unlockedNodeIds.Count={CurrentData.unlockedNodeIds.Count}");
        }

        SaveUserData();

        CurrentData.facilities = CollectFacilityData();

        List<SavedStudentData> collectedStudents = CollectStudentData();
        List<SavedSlotAssignment> collectedSlots = CollectSlotAssignments();

        bool hasExistingStudentData = CurrentData.students != null && CurrentData.students.Count > 0;
        bool collectedStudentDataIsEmpty = collectedStudents.Count == 0;

        // 기존 학생 데이터가 이미 있는데, 이번 저장에서만 0명으로 수집되면
        // 잘못된 타이밍 저장으로 판단하고 학생/슬롯 데이터 덮어쓰기를 막음
        if (hasExistingStudentData && collectedStudentDataIsEmpty)
        {
            Debug.LogWarning("[SaveManager] 기존 학생 데이터가 있는데 0명으로 수집되어 학생/슬롯 세이브 덮어쓰기를 방지합니다.");
        }
        else
        {
            CurrentData.students = collectedStudents;
            CurrentData.slotAssignments = collectedSlots;
        }

        CurrentData.flowData = CollectFlowData();

        // 토너먼트 데이터 수집
        CurrentData.tournament = CollectTournamentData();

        // 경기 시뮬레이션 상태 수집
        CurrentData.matchSim = CollectMatchSimData();

        // 메신저 상태 수집
        CurrentData.messenger = CollectMessengerData();

        // 슬롯 카드 표시용 메타 갱신
        CurrentData.saveTime = DateTime.Now.ToString("yyyy-MM-dd HH:mm");
        CurrentData.playTime = GameManager.Instance != null
            ? GameManager.Instance.CurrentDate.ToString("yyyy.MM.dd")
            : string.Empty;

        // 저장 직전 현재 플레이 중인 슬롯 번호를 강제로 유지
        CurrentData.slotIndex = _currentRuntimeSlotIndex;

        SaveSystem.Instance.Save(CurrentData);
    }

    public void AutoSaveByBranch(string branchName)
    {
        Debug.Log($"[SaveManager] 자동 저장 분기: {branchName}");
        SaveCurrent();
    }

    public void DeleteSlot(int slotIndex)
    {
        SaveSystem.Instance.Delete(slotIndex);

        if (CurrentData != null && CurrentData.slotIndex == slotIndex)
        {
            CurrentData = null;
            _currentRuntimeSlotIndex = -1;
        }
    }

    public void DeleteCurrentRunAndReturnTitle()
    {
        if (CurrentData != null)
        {
            SaveSystem.Instance.Delete(CurrentData.slotIndex);
        }

        CurrentData = null;
        _currentRuntimeSlotIndex = -1;

        if (MessengerManager.Instance != null)
        {
            MessengerManager.Instance.ClearAll();
        }

        SceneManager.LoadScene("Title");
    }

    public void Clear()
    {
        CurrentData = null;
        _currentRuntimeSlotIndex = -1;

        if (MessengerManager.Instance != null)
        {
            MessengerManager.Instance.ClearAll();
        }
    }

    private void SaveUserData()
    {
        if (CurrentUserData == null)
            CurrentUserData = new UserData();

        if (MoneyManager.Instance != null)
            CurrentUserData.reputation = MoneyManager.Instance.Reputation;

        if (HeadCoachManager.Instance != null && HeadCoachManager.Instance.IsInitialized)
            CurrentUserData.unlockedNodeIds = HeadCoachManager.Instance.GetUnlockedNodeIds();

        Debug.Log($"[SaveManager] SaveUserData | unlockedNodeIds.Count={(CurrentUserData.unlockedNodeIds != null ? CurrentUserData.unlockedNodeIds.Count : -1)}");
        SaveSystem.Instance.SaveUserData(CurrentUserData);
    }

    private void LoadUserData()
    {
        if (SaveSystem.Instance == null)
        {
            if (CurrentUserData == null)
                CurrentUserData = new UserData();

            Debug.LogWarning("[SaveManager] SaveSystem.Instance가 없어 기본 UserData로 초기화합니다.");
            return;
        }

        CurrentUserData = SaveSystem.Instance.LoadUserData();
        if (CurrentUserData == null)
            CurrentUserData = new UserData();

        Debug.Log($"[SaveManager] LoadUserData | unlockedNodeIds.Count={(CurrentUserData.unlockedNodeIds != null ? CurrentUserData.unlockedNodeIds.Count : -1)}");
    }

    private static SavedFlowData CollectFlowData()
    {
        if (GameManager.Instance == null)
            return new SavedFlowData();

        return new SavedFlowData
        {
            currentDate = GameManager.Instance.CurrentDate.ToString("yyyy-MM-dd"),
            turnIndex = GameManager.Instance.TurnIndex,
            dayIndex = GameManager.Instance.DayIndex,
            currentYear = GameManager.Instance.CurrentYear,
            phase = GameManager.Instance.CurrentPhase,
            isLeagueOpened = GameManager.Instance.IsLeagueOpened,
            isLeagueHandled = GameManager.Instance.IsLeagueHandled,
            leagueTermEnd = GameManager.Instance.LeagueTermEnd != default
                ? GameManager.Instance.LeagueTermEnd.ToString("yyyy-MM-dd")
                : string.Empty,
            activeEventIds = new List<string>(GameManager.Instance.ActiveEventIds),
            maxRecruitCount = GameManager.Instance.MaxRecruitCount,
            hasPendingFriendlyMatch = GameManager.Instance.HasPendingFriendlyMatch,
            hasPlayedVn10002 = GameManager.Instance.HasPlayedVn10002,
            hasPlayedVn10003 = GameManager.Instance.HasPlayedVn10003,

            friendlyMatchDate = GameManager.Instance.FriendlyMatchDate != default
                ? GameManager.Instance.FriendlyMatchDate.ToString("yyyy-MM-dd")
                : string.Empty,
            friendlyOpponentName = GameManager.Instance.FriendlyOpponentName,
            friendlyMatchConfirmed = GameManager.Instance.IsFriendlyMatchConfirmed,
            friendlyMatchApplyCount = FriendlyMatchManager.Instance != null
                ? FriendlyMatchManager.Instance.CurrentApplyCount
                : 0,
            friendlyMatchLastMonth = FriendlyMatchManager.Instance != null
                ? FriendlyMatchManager.Instance.LastMonth
                : -1,
        };
    }

    private static List<SavedStudentData> CollectStudentData()
    {
        if (StudentManager.Instance == null)
        {
            return new List<SavedStudentData>();
        }

        List<Student> students = StudentManager.Instance.Students;
        List<SavedStudentData> result = new(students.Count);

        foreach (Student s in students)
        {
            result.Add(new SavedStudentData
            {
                id = s.id,
                studentName = s.studentName,
                positionId = s.positionId,
                positionName = s.positionName,
                grade = s.grade,
                portraitColor = s.portraitColor,
                portraitIndex = s.portraitIndex,
                height = s.height,
                weight = s.weight,
                mental = s.mental,
                shoot = s.shoot,
                speed = s.speed,
                jump = s.jump,
                stamina = s.stamina,
                shootExp = s.shootExp,
                speedExp = s.speedExp,
                jumpExp = s.jumpExp,
                staminaExp = s.staminaExp,
                mentalExp = s.mentalExp,
                potentialTier = s.potential_tier,
                potential = s.potential,
                condition = s.condition,
                activeEffectIds = new List<string>(s.activeEffectIds ?? new List<string>()),
                conditionRecoveryBonus = s.conditionRecoveryBonus,
                trainingEfficiencyBonus = s.trainingEfficiencyBonus,
                isTrainingBlocked = s.isTrainingBlocked,
            });
        }

        return result;
    }

    private static List<SavedSlotAssignment> CollectSlotAssignments()
    {
        List<SavedSlotAssignment> result = new();

        if (StudentManager.Instance == null)
        {
            return result;
        }

        foreach (KeyValuePair<int, Student> pair in StudentManager.Instance.SlotAssignments)
        {
            if (pair.Value == null)
                continue;

            result.Add(new SavedSlotAssignment
            {
                slotIndex = pair.Key,
                studentId = pair.Value.id,
            });
        }

        return result;
    }

    private static SavedFacilityData CollectFacilityData()
    {
        if (FacilitySystem.Instance == null)
            return new SavedFacilityData();

        SavedFacilityData data = new SavedFacilityData
        {
            schoolLevel = FacilitySystem.Instance.GetLevel("school"),
            gymLevel = FacilitySystem.Instance.GetLevel("gym"),
            cafeteriaLevel = FacilitySystem.Instance.GetLevel("cafeteria"),
            counselingCenterLevel = FacilitySystem.Instance.GetLevel("counselingcenter"),
        };
        return data;
    }

    // TournamentManager 내부 상태 수집
    private static SavedTournamentData CollectTournamentData()
    {
        TournamentManager tm = UnityEngine.Object.FindFirstObjectByType<TournamentManager>();

        // 토너먼트 씬이 아닐 때는 빈 데이터 반환
        if (tm == null)
        {
            return new SavedTournamentData();
        }
        return tm.CollectSaveData();
    }

    // 경기 시뮬레이션 상태 수집
    private static SavedMatchSimData CollectMatchSimData()
    {
        MatchGameManager mgm = UnityEngine.Object.FindFirstObjectByType<MatchGameManager>();
        if (mgm == null)
        {
            return new SavedMatchSimData { isMatchRunning = false };
        }
        return mgm.CollectSaveData();
    }

    // 메신저 상태 수집
    private static SavedMessengerData CollectMessengerData()
    {
        if (MessengerManager.Instance == null)
            return new SavedMessengerData();

        return MessengerManager.Instance.CollectSaveData();
    }

    private static void ApplyFacilityData(SavedFacilityData data)
    {
        if (FacilitySystem.Instance == null)
            return;

        // 어떤 슬롯을 적용하든 먼저 기본값으로 초기화해서 이전 슬롯 잔존 상태를 제거
        FacilitySystem.Instance.ResetLevelsToDefault();

        if (data == null)
            return;

        FacilitySystem.Instance.SetLevel("school", data.schoolLevel);
        FacilitySystem.Instance.SetLevel("gym", data.gymLevel);
        FacilitySystem.Instance.SetLevel("cafeteria", data.cafeteriaLevel);
        FacilitySystem.Instance.SetLevel("counselingcenter", data.counselingCenterLevel);

        Debug.Log($"[SaveManager] 시설 데이터 복원 완료 | school={data.schoolLevel}, gym={data.gymLevel}, cafeteria={data.cafeteriaLevel}, counseling={data.counselingCenterLevel}");
    }

    private static void ApplyStudentData(List<SavedStudentData> savedStudents, List<SavedSlotAssignment> savedSlots)
    {
        if (StudentManager.Instance == null || savedStudents == null)
            return;

        StudentManager.Instance.ClearAllStudents();

        int maxId = 0;
        List<Student> restoredStudents = new(savedStudents.Count);

        foreach (SavedStudentData data in savedStudents)
        {
            Student student = new()
            {
                id = data.id,
                studentName = data.studentName,
                positionId = data.positionId,
                positionName = data.positionName,
                grade = data.grade,
                portraitColor = data.portraitColor,
                portraitIndex = data.portraitIndex,
                height = data.height,
                weight = data.weight,
                mental = data.mental,
                shoot = data.shoot,
                speed = data.speed,
                jump = data.jump,
                stamina = data.stamina,
                shootExp = data.shootExp,
                speedExp = data.speedExp,
                jumpExp = data.jumpExp,
                staminaExp = data.staminaExp,
                mentalExp = data.mentalExp,
                potential_tier = data.potentialTier,
                potential = data.potential,
                condition = data.condition,
                activeEffectIds = data.activeEffectIds ?? new List<string>(),
                conditionRecoveryBonus = data.conditionRecoveryBonus,
                trainingEfficiencyBonus = data.trainingEfficiencyBonus,
                isTrainingBlocked = data.isTrainingBlocked,
            };

            if (student.id > maxId)
                maxId = student.id;

            restoredStudents.Add(student);
            StudentManager.Instance.AddStudent(student);
        }

        StudentFactory.RestoreStudentIdCounter(maxId + 1);
        StudentFactory.RebuildRuntimeCaches(restoredStudents);

        if (savedSlots != null)
        {
            foreach (SavedSlotAssignment slotData in savedSlots)
            {
                Student student = restoredStudents.Find(s => s.id == slotData.studentId);
                if (student != null)
                {
                    StudentManager.Instance.AssignSlot(slotData.slotIndex, student);
                    Debug.Log($"[SaveManager] 슬롯 복원 | slot={slotData.slotIndex} | student={student.studentName}");
                }
            }
        }
    }

    // 토너먼트 데이터 복원 — TournamentManager가 씬에 존재할 때만 실행
    private static void ApplyTournamentData(SavedTournamentData data)
    {
        if (data == null)
            return;

        TournamentManager tm = UnityEngine.Object.FindFirstObjectByType<TournamentManager>();
        if (tm == null)
            return;

        tm.RestoreSaveData(data);
    }

    // 경기 시뮬레이션 상태 복원 — MatchGameManager가 씬에 존재할 때만 실행
    private static void ApplyMatchSimData(SavedMatchSimData data)
    {
        if (data == null || !data.isMatchRunning)
            return;

        MatchGameManager mgm = UnityEngine.Object.FindFirstObjectByType<MatchGameManager>();
        if (mgm == null)
            return;

        mgm.RestoreSaveData(data);
    }

    // 메신저 데이터 복원
    private static void ApplyMessengerData(SavedMessengerData data)
    {
        if (MessengerManager.Instance == null)
            return;

        MessengerManager.Instance.RestoreSaveData(data);
    }

    // 로드 시 activeEventIds 기준으로 학생의 activeEffectIds를 재동기화
    // conditionRecoveryBonus 등 수치 보너스는 SavedStudentData에서 이미 복원되므로
    // TickCondition()이 참조하는 activeEffectIds 누락분만 보충
    private static void ApplyActiveEventEffects(SavedFlowData flowData)
    {
        if (flowData == null || flowData.activeEventIds == null || flowData.activeEventIds.Count == 0)
            return;

        if (StudentManager.Instance == null)
            return;

        AlwaysEventTableSO eventTable = CachedSOData.Get<AlwaysEventTableSO>();
        if (eventTable == null || eventTable.Rows == null)
        {
            Debug.LogWarning("[SaveManager] AlwaysEventTable이 없어 이벤트 효과 복원을 건너뜁니다.");
            return;
        }

        AlwaysEffectTableSO effectTable = CachedSOData.Get<AlwaysEffectTableSO>();
        if (effectTable == null)
        {
            Debug.LogWarning("[SaveManager] AlwaysEffectTable이 없어 이벤트 효과 복원을 건너뜁니다.");
            return;
        }

        foreach (string savedId in flowData.activeEventIds)
        {
            // savedId에 해당하는 AlwaysEventRow 탐색
            // GetRowId()와 동일한 포맷: "{row.id}_{row.termStart}"
            AlwaysEventRow matchedRow = FindEventRowById(eventTable, savedId);

            if (matchedRow == null)
            {
                Debug.LogWarning($"[SaveManager] 저장된 이벤트 ID '{savedId}'에 해당하는 row를 찾을 수 없습니다.");
                continue;
            }

            // effectId 없음 or roster 타입은 AlwaysEffectApplier가 처리하지 않는 케이스 → 스킵
            if (string.IsNullOrEmpty(matchedRow.effectId) || matchedRow.type == "roster")
                continue;

            if (!effectTable.TryGet(matchedRow.effectId, out AlwaysEffectRow effectRow))
            {
                Debug.LogWarning($"[SaveManager] effectId '{matchedRow.effectId}'를 AlwaysEffectTable에서 찾을 수 없습니다.");
                continue;
            }

            // 수치 보너스는 SavedStudentData에서 이미 복원됨
            // TickCondition()이 참조하는 activeEffectIds만 재동기화
            SyncActiveEffectId(matchedRow, effectRow);
            Debug.Log($"[SaveManager] 이벤트 activeEffectId 복원: {savedId} → effectId={effectRow.id}");
        }
    }

    // AlwaysEventManager.GetRowId()와 동일한 포맷으로 row 탐색
    private static AlwaysEventRow FindEventRowById(AlwaysEventTableSO table, string targetId)
    {
        for (int i = 0; i < table.Rows.Count; i++)
        {
            AlwaysEventRow row = table.Rows[i];
            if (row == null) continue;

            string rowId = string.IsNullOrWhiteSpace(row.id) ? "(no-id)" : row.id.Trim();
            string start = string.IsNullOrWhiteSpace(row.termStart) ? "" : row.termStart.Trim();

            if ($"{rowId}_{start}" == targetId) return row;
        }
        return null;
    }

    // range == 0: 전체 학생 / range > 0: 특정 슬롯 학생
    // activeEffectIds에 effectId가 없으면 추가
    private static void SyncActiveEffectId(AlwaysEventRow eventRow, AlwaysEffectRow effectRow)
    {
        if (StudentManager.Instance == null) return;

        if (eventRow.range == 0)
        {
            foreach (Student student in StudentManager.Instance.Students)
                AddEffectIdIfMissing(student, effectRow.id);
        }
        else
        {
            Student student = StudentManager.Instance.GetAssignedStudent(eventRow.range);
            if (student != null)
                AddEffectIdIfMissing(student, effectRow.id);
            else
                Debug.LogWarning($"[SaveManager] 슬롯 {eventRow.range}에 배치된 학생이 없어 effectId 동기화 생략.");
        }
    }

    private static void AddEffectIdIfMissing(Student student, string effectId)
    {
        if (student == null) return;

        if (student.activeEffectIds == null)
            student.activeEffectIds = new List<string>();

        if (!student.activeEffectIds.Contains(effectId))
        {
            student.activeEffectIds.Add(effectId);
            StudentManager.Instance.NotifyStudentModified(student);
        }
    }

    public void ConsumePendingNewGameFlag()
    {
        IsPendingNewGame = false;
    }

    public bool HasPlayedVnStory(int storyId)
    {
        if (CurrentData == null || CurrentData.flowData == null)
            return false;

        return storyId switch
        {
            10002 => CurrentData.flowData.hasPlayedVn10002,
            10003 => CurrentData.flowData.hasPlayedVn10003,
            _ => false
        };
    }

    public void MarkVnStoryPlayed(int storyId)
    {
        if (CurrentData == null)
            return;

        if (CurrentData.flowData == null)
            CurrentData.flowData = new SavedFlowData();

        switch (storyId)
        {
            case 10002:
                CurrentData.flowData.hasPlayedVn10002 = true;
                break;
            case 10003:
                CurrentData.flowData.hasPlayedVn10003 = true;
                break;
        }

        if (GameManager.Instance != null)
            GameManager.Instance.MarkVnStoryPlayed(storyId);
    }

    public void MarkCurrentRunForDeleteOnTitle()
    {
        ShouldDeleteCurrentRunOnTitle = true;
    }

    public void DeleteCurrentRunIfMarked()
    {
        if (!ShouldDeleteCurrentRunOnTitle)
            return;

        if (CurrentData != null)
        {
            SaveSystem.Instance.Delete(CurrentData.slotIndex);
        }

        CurrentData = null;
        _currentRuntimeSlotIndex = -1;
        ShouldDeleteCurrentRunOnTitle = false;

        if (MessengerManager.Instance != null)
        {
            MessengerManager.Instance.ClearAll();
        }
    }

    public void SaveAfterChoice(string branchName, Action choiceAction)
    {
        choiceAction?.Invoke();
        AutoSaveByBranch(branchName);
    }

    public void RestoreHeadCoachNodesIfPossible()
    {
        if (HeadCoachManager.Instance == null || !HeadCoachManager.Instance.IsInitialized)
        {
            Debug.LogWarning("[SaveManager] HeadCoachManager가 아직 초기화되지 않아 감독 노드 복원을 건너뜁니다.");
            return;
        }

        List<int> unlockedNodeIds = null;

        if (CurrentUserData != null && CurrentUserData.unlockedNodeIds != null && CurrentUserData.unlockedNodeIds.Count > 0)
        {
            unlockedNodeIds = CurrentUserData.unlockedNodeIds;
        }
        else if (CurrentData != null && CurrentData.unlockedNodeIds != null)
        {
            unlockedNodeIds = CurrentData.unlockedNodeIds;
        }

        if (unlockedNodeIds == null)
            unlockedNodeIds = new List<int>();

        HeadCoachManager.Instance.RestoreUnlockedNodes(unlockedNodeIds);
        Debug.Log($"[SaveManager] 감독 노드 복원 완료. count={unlockedNodeIds.Count}");
    }

    // 새 게임 첫 영입 미완료 슬롯 삭제
    // isRecruitmentInProgress == true && students.Count == 0 → 새 게임 영입 중 강제종료
    // isRecruitmentInProgress == true && students.Count > 0  → 학기 중 영입 중 강제종료 → 플래그만 초기화, 슬롯 유지
    private void CleanupIncompleteNewGameSlots()
    {
        Debug.Log("[SaveManager] CleanupIncompleteNewGameSlots 시작");
        if (SaveSystem.Instance == null)
        {
            Debug.LogWarning("[SaveManager] SaveSystem.Instance가 null");
            return;
        }

        int totalSlots = SaveSystem.Instance.GetTotalSlotCount();
        for (int i = 1; i <= totalSlots; i++)
        {
            PlayData data = SaveSystem.Instance.Load(i);
            if (data == null) continue;
            if (!data.isRecruitmentInProgress) continue;

            bool hasStudents = data.students != null && data.students.Count > 0;
            if (hasStudents)
            {
                // 학기 중 영입 강제종료 → 플래그만 초기화 후 슬롯 유지
                data.isRecruitmentInProgress = false;
                SaveSystem.Instance.Save(data);
                Debug.Log($"[SaveManager] 슬롯 {i}: 학기 영입 미완료 감지 → 플래그 초기화 후 유지");
                continue;
            }

            // 새 게임 첫 영입 강제종료 → 슬롯 삭제
            Debug.LogWarning($"[SaveManager] 슬롯 {i}: 새 게임 영입 미완료 감지 → 삭제");
            SaveSystem.Instance.Delete(i);
        }
    }
}
