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

    // 제거: SetLoadedData() — 호출부 없는 데드코드였음

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