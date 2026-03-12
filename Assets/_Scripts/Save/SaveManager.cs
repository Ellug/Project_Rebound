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
        CurrentUserData = new UserData();
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
            playTime = string.Empty,
            saveTime = string.Empty,
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

        if (MoneyManager.Instance != null)
        {
            CurrentData.gold = MoneyManager.Instance.Gold;
            CurrentData.reputation = MoneyManager.Instance.Reputation;
        }

        if (HeadCoachManager.Instance != null && HeadCoachManager.Instance.IsInitialized)
        {
            CurrentData.unlockedNodeIds = HeadCoachManager.Instance.GetUnlockedNodeIds();
        }

        SaveUserData();

        CurrentData.facilities = CollectFacilityData();
        CurrentData.students = CollectStudentData();
        CurrentData.slotAssignments = CollectSlotAssignments();
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
    }

    private static SavedFlowData CollectFlowData()
    {
        GameManager gm = GameManager.Instance;
        if (gm == null)
            return new SavedFlowData();

        return new SavedFlowData
        {
            currentDate = gm.CurrentDate.ToString("yyyy-MM-dd"),
            turnIndex = gm.TurnIndex,
            dayIndex = gm.DayIndex,
            currentYear = gm.CurrentYear,
            phase = gm.CurrentPhase,
            isLeagueOpened = gm.IsLeagueOpened,
            isLeagueHandled = gm.IsLeagueHandled,
            leagueTermEnd = gm.LeagueTermEnd == default
                ? string.Empty
                : gm.LeagueTermEnd.ToString("yyyy-MM-dd"),
            activeEventIds = new List<string>(gm.ActiveEventIds),
            maxRecruitCount = gm.MaxRecruitCount,
            hasPendingFriendlyMatch = gm.HasPendingFriendlyMatch,
        };
    }

    private static List<SavedStudentData> CollectStudentData()
    {
        List<SavedStudentData> result = new();

        foreach (Student student in StudentManager.Instance.Students)
        {
            result.Add(new SavedStudentData
            {
                id = student.id,
                studentName = student.studentName,
                positionName = student.positionName,
                grade = student.grade,
                portraitColor = student.portraitColor,
                portraitIndex = student.portraitIndex,
                height = student.height,
                weight = student.weight,
                mental = student.mental,
                shoot = student.shoot,
                speed = student.speed,
                jump = student.jump,
                stamina = student.stamina,
                potentialTier = student.potential_tier,
                potential = student.potential,
                condition = student.condition,
                trust = student.trust,
                activeEffectIds = new List<string>(student.activeEffectIds),
                conditionRecoveryBonus = student.conditionRecoveryBonus,
                trainingEfficiencyBonus = student.trainingEfficiencyBonus,
                isTrainingBlocked = student.isTrainingBlocked,
            });
        }

        return result;
    }

    private static List<SavedSlotAssignment> CollectSlotAssignments()
    {
        List<SavedSlotAssignment> result = new();

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

        return new SavedFacilityData
        {
            schoolLevel = FacilitySystem.Instance.GetLevel("school"),
            gymLevel = FacilitySystem.Instance.GetLevel("gym"),
            cafeteriaLevel = FacilitySystem.Instance.GetLevel("cafeteria"),
            counselingCenterLevel = FacilitySystem.Instance.GetLevel("counselingcenter"),
        };
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

    private static void ApplyStudentData(
        List<SavedStudentData> savedStudents,
        List<SavedSlotAssignment> savedSlots)
    {
        StudentManager.Instance.ClearAllStudents();

        // 슬롯 배치 복원 시 id로 Student 객체를 빠르게 참조하기 위한 딕셔너리
        Dictionary<int, Student> studentById = new();
        int maxId = 0;

        foreach (SavedStudentData saved in savedStudents)
        {
            Student student = new Student
            {
                id = saved.id,
                studentName = saved.studentName,
                positionName = saved.positionName,
                grade = saved.grade,
                portraitColor = saved.portraitColor,
                portraitIndex = saved.portraitIndex,
                height = saved.height,
                weight = saved.weight,
                mental = saved.mental,
                shoot = saved.shoot,
                speed = saved.speed,
                jump = saved.jump,
                stamina = saved.stamina,
                potential_tier = saved.potentialTier,
                potential = saved.potential,
                condition = saved.condition,
                trust = saved.trust,
                activeEffectIds = new List<string>(saved.activeEffectIds),
                conditionRecoveryBonus = saved.conditionRecoveryBonus,
                trainingEfficiencyBonus = saved.trainingEfficiencyBonus,
                isTrainingBlocked = saved.isTrainingBlocked,
            };

            StudentManager.Instance.AddStudent(student);
            studentById[student.id] = student;

            if (student.id > maxId)
                maxId = student.id;
        }

        // 로드 후 신규 학생 생성 시 기존 id와 충돌하지 않도록 카운터 복원
        StudentFactory.RestoreStudentIdCounter(maxId + 1);
        StudentFactory.RebuildRuntimeCaches(StudentManager.Instance.Students);

        foreach (SavedSlotAssignment assignment in savedSlots)
        {
            if (!studentById.TryGetValue(assignment.studentId, out Student student))
                continue;

            StudentManager.Instance.AssignSlot(assignment.slotIndex, student);
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
}