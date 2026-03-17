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

    public int CurrentSlotIndex => CurrentData != null ? CurrentData.slotIndex : -1;

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

        if (data == null)
        {
            return;
        }

        CurrentData = data;
        IsPendingNewGame = false;
        LoadUserData();

        if (MoneyManager.Instance != null)
        {
            MoneyManager.Instance.ApplySaveData(
                data.gold,
                CurrentUserData != null ? CurrentUserData.reputation : data.reputation);
        }

        SceneManager.LoadScene(sceneName);
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
        };

        IsPendingNewGame = true;

        SaveUserData();
        SaveSystem.Instance.Save(CurrentData);
        return true;
    }

    public void SaveCurrent()
    {
        if (CurrentData == null)
        {
            Debug.LogWarning("저장할 데이터 없음");
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

        // 슬롯 카드 표시용 메타 갱신
        CurrentData.saveTime = DateTime.Now.ToString("yyyy-MM-dd HH:mm");
        CurrentData.playTime = GameManager.Instance != null
            ? GameManager.Instance.CurrentDate.ToString("yyyy.MM.dd")
            : string.Empty;

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
        }
    }

    public void DeleteCurrentRunAndReturnTitle()
    {
        if (CurrentData != null)
        {
            SaveSystem.Instance.Delete(CurrentData.slotIndex);
        }

        CurrentData = null;
        SceneManager.LoadScene("Title");
    }

    public void Clear()
    {
        CurrentData = null;
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
            hasPlayedVn10001 = GameManager.Instance.HasPlayedVn10001,
            hasPlayedVn10002 = GameManager.Instance.HasPlayedVn10002,
            hasPlayedVn10003 = GameManager.Instance.HasPlayedVn10003,

            friendlyMatchDate = GameManager.Instance.FriendlyMatchDate != default
                ? GameManager.Instance.FriendlyMatchDate.ToString("yyyy-MM-dd")
                : string.Empty,
            friendlyOpponentName = GameManager.Instance.FriendlyOpponentName,
            friendlyMatchConfirmed = GameManager.Instance.IsFriendlyMatchConfirmed,
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

    private static void ApplyFacilityData(SavedFacilityData data)
    {
        if (data == null)
            return;

        if (FacilitySystem.Instance == null)
            return;

        FacilitySystem.Instance.SetLevel("school", data.schoolLevel);
        FacilitySystem.Instance.SetLevel("gym", data.gymLevel);
        FacilitySystem.Instance.SetLevel("cafeteria", data.cafeteriaLevel);
        FacilitySystem.Instance.SetLevel("counselingcenter", data.counselingCenterLevel);
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
            10001 => CurrentData.flowData.hasPlayedVn10001,
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
            case 10001:
                CurrentData.flowData.hasPlayedVn10001 = true;
                break;
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
        ShouldDeleteCurrentRunOnTitle = false;
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
}
