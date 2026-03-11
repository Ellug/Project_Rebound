using System;
using System.Collections.Generic;
using System.Linq;
using AYellowpaper.SerializedCollections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class StudentManager : Singleton<StudentManager>
{
    // 신뢰도 기반 퇴부 판정 기준값들
    private const int TrustLowThreshold = 10;
    private const int TrustCriticalThreshold = 0;
    private const int LowTrustExpelDays = 7;
    private const int CriticalTrustExpelDays = 3;

    [SerializeField] private List<Student> _students = new();

    // 슬롯 배치 정보: 슬롯 인덱스 -> 학생
    [SerializeField, SerializedDictionary("슬롯 인덱스", "학생")]
    private SerializedDictionary<int, Student> _slotAssignments = new();

    // 학생별 신뢰도 저하/붕괴 연속 일수 추적용
    [SerializeField, SerializedDictionary("학생 ID", "저신뢰 연속 일수")]
    private SerializedDictionary<int, int> _lowTrustStreakByStudentId = new();
    [SerializeField, SerializedDictionary("학생 ID", "중대저신뢰 연속 일수")]
    private SerializedDictionary<int, int> _criticalTrustStreakByStudentId = new();

    // 씬 전환 시 턴/날짜 이벤트 재연결용 참조
    private TurnManager _turnManager;
    private DateManager _dateManager;

    public event Action<List<Student>> OnStudentsChanged;
    public event Action<Student> OnStudentAdded;
    public event Action<Student> OnStudentRemoved;
    public event Action<Student> OnStudentModified;
    public event Action<SerializedDictionary<int, Student>> OnSlotAssignmentsChanged;

    public List<Student> Students => _students; // 얘는 영입 완료해서 보유한 학생.
    public int GetStudentCount() => _students.Count;

    // 슬롯 배치

    // 슬롯에 학생 배치 저장
    public void AssignSlot(int slotIndex, Student student)
    {
        if (student == null)
        {
            ClearSlot(slotIndex);
            return;
        }

        _slotAssignments[slotIndex] = student;
        OnSlotAssignmentsChanged?.Invoke(_slotAssignments);
    }

    // 슬롯 배치 해제
    public void ClearSlot(int slotIndex)
    {
        if (!_slotAssignments.ContainsKey(slotIndex))
            return;

        _slotAssignments.Remove(slotIndex);
        OnSlotAssignmentsChanged?.Invoke(_slotAssignments);
        Debug.Log($"[StudentManager] 슬롯 {slotIndex} 배치 해제.");
    }

    private void RemoveStudentFromSlots(Student student)
    {
        if (student == null) return;

        // 이 학생이 배치된 슬롯 인덱스들을 찾음
        var keysToRemove = _slotAssignments.Where(pair => pair.Value == student).Select(pair => pair.Key).ToList();

        foreach (int key in keysToRemove)
        {
            ClearSlot(key); // 찾은 슬롯 비우기
        }
    }

    // 특정 슬롯의 배치된 학생 반환 (없으면 null)
    public Student GetAssignedStudent(int slotIndex)
    {
        _slotAssignments.TryGetValue(slotIndex, out Student student);
        return student;
    }

    // 배치된 학생이 있는지 여부
    public bool IsSlotAssigned(int slotIndex) => _slotAssignments.ContainsKey(slotIndex);

    // 전체 슬롯 배치 정보 반환
    public IReadOnlyDictionary<int, Student> SlotAssignments => _slotAssignments;

    protected override void OnSingletonAwake()
    {
        // StudentManager 초기화 로직
        SceneManager.sceneLoaded += HandleSceneLoaded;
        RebindTurnManager();
        Debug.Log("[StudentManager] Initialized");
    }

    void OnDestroy()
    {
        SceneManager.sceneLoaded -= HandleSceneLoaded;
        UnbindTurnManager();
        Cleanup();
    }

    // 새로운 학생 추가
    // 팩토리에서 생성한 거 리턴받으면, 그걸 여기에 애드 하던
    // 아니면 팩토리에서 생성하면서 Add 같이 해버리던? => 안될듯. 드래그앤드롭해서 영입 확정하는 순간 Add 하는 게 맞는듯.
    public void AddStudent(Student student)
    {
        ClearTrustTracking(student);
        _students.Add(student);
        OnStudentAdded?.Invoke(student);
        OnStudentsChanged?.Invoke(_students);

        Debug.Log($"[StudentManager] Added student: {student.studentName} (ID: {student.id})");
    }

    // 학생 ID로 삭제
    public bool RemoveStudent(int studentId)
    {
        var student = _students.FirstOrDefault(s => s.id == studentId);
        if (student == null)
        {
            Debug.LogWarning($"[StudentManager] Student with ID {studentId} not found");
            return false;
        }

        RemoveStudentFromSlots(student);
        ClearTrustTracking(student);
        _students.Remove(student);
        OnStudentRemoved?.Invoke(student);
        OnStudentsChanged?.Invoke(_students);

        Debug.Log($"[StudentManager] Removed student: {student.studentName} (ID: {student.id})");
        return true;
    }

    // 학생 객체로 삭제
    public bool RemoveStudent(Student student)
    {
        if (student == null || !_students.Contains(student))
            return false;

        RemoveStudentFromSlots(student);
        ClearTrustTracking(student);
        _students.Remove(student);
        OnStudentRemoved?.Invoke(student);
        OnStudentsChanged?.Invoke(_students);

        Debug.Log($"[StudentManager] Removed student: {student.studentName} (ID: {student.id})");
        return true;
    }

    public void GraduateSeniors()
    {
        var seniors = _students.Where(s => s.grade == 3).ToList();

        foreach (var student in seniors)
        {
            RemoveStudentFromSlots(student);
            ClearTrustTracking(student);

            _students.Remove(student);
            OnStudentRemoved?.Invoke(student);
        }

        OnStudentsChanged?.Invoke(_students);
        Debug.Log($"[StudentManager] 졸업 처리 완료: {seniors.Count}명 제거");
    }


    // 모든 학생 삭제
    public void ClearAllStudents()
    {
        _students.Clear();

        _slotAssignments.Clear();
        ClearAllTrustTracking();
        OnStudentsChanged?.Invoke(_students);
        Debug.Log("[StudentManager] Cleared all students");
    }

    // 외부에서 Student 객체 직접 수정 후 호출
    public void NotifyStudentModified(Student student)
    {
        if (student == null || !_students.Contains(student))
        {
            Debug.LogWarning("[StudentManager] Cannot notify for student not in list");
            return;
        }

        OnStudentModified?.Invoke(student);
        OnStudentsChanged?.Invoke(_students);
    }

    // 남은 재학생 학년 진급 처리
    public void PromoteStudents()
    {
        bool isPromoted = false;
        foreach (var student in _students)
        {
            if (student.grade < 3)
            {
                student.grade++;
                OnStudentModified?.Invoke(student);
                isPromoted = true;
            }
        }

        if (isPromoted)
        {
            OnStudentsChanged?.Invoke(_students);
        }

        Debug.Log("[StudentManager] 재학생 진급 처리 완료");
    }

    private void HandleSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        // 씬이 바뀔 때마다 현재 TurnManager/DateManager에 다시 바인딩
        RebindTurnManager();
    }

    private void RebindTurnManager()
    {
        UnbindTurnManager();

        _turnManager = FindFirstObjectByType<TurnManager>();
        if (_turnManager == null) return;

        _dateManager = _turnManager.DateManager;
        if (_dateManager == null) return;

        _dateManager.OnDateAdvanced -= HandleDateAdvanced;
        _dateManager.OnDateAdvanced += HandleDateAdvanced;
    }

    private void UnbindTurnManager()
    {
        if (_dateManager != null)
            _dateManager.OnDateAdvanced -= HandleDateAdvanced;

        _dateManager = null;
        _turnManager = null;
    }

    private void HandleDateAdvanced(DateTime currentDate, int dayIndex)
    {
        // 하루가 지날 때마다 신뢰도 기반 퇴부 규칙 평가
        EvaluateTrustBasedExpulsions();
    }

    private void EvaluateTrustBasedExpulsions()
    {
        // 학생별 연속 일수를 갱신하고 퇴부 대상 목록을 계산
        if (_students == null || _students.Count == 0)
            return;

        List<(Student student, bool isCritical)> expulsionTargets = new();

        foreach (Student student in _students)
        {
            if (student == null)
                continue;

            int studentId = student.id;

            if (student.trust <= TrustCriticalThreshold)
            {
                _lowTrustStreakByStudentId.Remove(studentId);
                int criticalStreak = IncrementTrustStreak(_criticalTrustStreakByStudentId, studentId);

                if (criticalStreak >= CriticalTrustExpelDays)
                    expulsionTargets.Add((student, true));

                continue;
            }

            _criticalTrustStreakByStudentId.Remove(studentId);

            if (student.trust <= TrustLowThreshold)
            {
                int lowStreak = IncrementTrustStreak(_lowTrustStreakByStudentId, studentId);

                if (lowStreak >= LowTrustExpelDays)
                    expulsionTargets.Add((student, false));

                continue;
            }

            _lowTrustStreakByStudentId.Remove(studentId);
        }

        foreach ((Student student, bool isCritical) in expulsionTargets)
            ExpelStudentByTrustRule(student, isCritical);
    }

    private static int IncrementTrustStreak(SerializedDictionary<int, int> streakMap, int studentId)
    {
        // 특정 학생의 연속 일수 카운트를 1일 증가
        if (streakMap.TryGetValue(studentId, out int currentStreak))
            currentStreak++;
        else
            currentStreak = 1;

        streakMap[studentId] = currentStreak;
        return currentStreak;
    }

    private void ExpelStudentByTrustRule(Student student, bool isCritical)
    {
        // 규칙 충족 학생을 실제로 퇴부 처리하고 후속 로직 실행
        if (student == null) return;

        bool removed = RemoveStudent(student);
        if (!removed) return;

        if (isCritical)
            TryReduceStudentCapacityByCriticalExpulsion(student);

        ShowExpulsionPopup(student, isCritical);
    }

    private void ShowExpulsionPopup(Student student, bool isCritical)
    {
        // 퇴부 학생의 이름/학년/스펙/사유를 기본 팝업으로 안내
        string reasonText = isCritical
            ? "신뢰도 0 이하 상태가 3일 이상 지속되어 퇴부 처리되었습니다."
            : "신뢰도 10 이하 상태가 7일 이상 지속되어 퇴부 처리되었습니다.";

        string message =
            $"{student.grade}학년 {student.studentName} 학생이 퇴부했습니다.\n" +
            $"멘탈 {student.mental} / 슈팅 {student.shoot} / 속도 {student.speed} / 점프력 {student.jump} / 지구력 {student.stamina}\n\n" +
            $"사유: {reasonText}";

        string title = isCritical ? "학생 퇴부 (중대)" : "학생 퇴부";
        UIPopupRequest req = UIPopupRequest.Default(
            title: title,
            message: message,
            onPrimary: null,
            onCancel: null,
            subMessage: null,
            previewSprite: null,
            showCancel: false,
            primaryKind: UIPopupRequest.PrimaryButtonKind.Confirm
        );
        req.AutoCloseOnPrimary = true;
        req.AutoCloseOnCancel = true;

        UIManager.Instance.ShowPopup(req);
    }

    private void TryReduceStudentCapacityByCriticalExpulsion(Student expelledStudent)
    {
        // 신뢰도 0 퇴부일 때만 모집 정원 감소를 시도
        RecruitmentManager recruitmentManager = FindFirstObjectByType<RecruitmentManager>();
        if (recruitmentManager == null)
        {
            Debug.LogWarning($"[StudentManager] RecruitmentManager를 찾지 못해 정원 감소를 적용하지 못했음. expelled={expelledStudent.studentName}");
            return;
        }

        recruitmentManager.TryDecreaseMaxRecruitCountByTrustExpulsion();
    }

    private void ClearTrustTracking(Student student)
    {
        // 특정 학생의 신뢰도 연속 일수 추적 데이터를 제거
        if (student == null) return;

        _lowTrustStreakByStudentId.Remove(student.id);
        _criticalTrustStreakByStudentId.Remove(student.id);
    }

    private void ClearAllTrustTracking()
    {
        // 전체 학생의 신뢰도 연속 일수 추적 데이터를 초기화
        _lowTrustStreakByStudentId.Clear();
        _criticalTrustStreakByStudentId.Clear();
    }

    // Title로 돌아갈 때 명시적 해제
    public void Cleanup()
    {
        // 이벤트 구독 해제
        OnStudentsChanged = null;
        OnStudentAdded = null;
        OnStudentRemoved = null;
        OnStudentModified = null;
        OnSlotAssignmentsChanged = null;

        // 데이터 초기화
        _students.Clear();
        _slotAssignments.Clear();
        ClearAllTrustTracking();

        // StudentFactory 초기화
        StudentFactory.ResetUsedNames();
        StudentFactory.ResetStudentIdCounter();
    }
}
