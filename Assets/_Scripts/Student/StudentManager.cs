using System;
using System.Collections.Generic;
using System.Linq;
using AYellowpaper.SerializedCollections;
using UnityEngine;

public class StudentManager : Singleton<StudentManager>
{
    [SerializeField] private List<Student> _students = new();

    // 슬롯 배치 정보: 슬롯 인덱스 -> 학생
    [SerializeField, SerializedDictionary("슬롯 인덱스", "학생")]
    private SerializedDictionary<int, Student> _slotAssignments = new();

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
        Debug.Log("[StudentManager] Initialized");
    }

    void OnDestroy()
    {
        Cleanup();
    }

    // 새로운 학생 추가
    // 팩토리에서 생성한 거 리턴받으면, 그걸 여기에 애드 하던
    // 아니면 팩토리에서 생성하면서 Add 같이 해버리던? => 안될듯. 드래그앤드롭해서 영입 확정하는 순간 Add 하는 게 맞는듯.
    public void AddStudent(Student student)
    {
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
                // 학년이 변경되었으므로 UI 갱신 이벤트 발생
                NotifyStudentModified(student);

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

        // StudentFactory 초기화
        StudentFactory.ResetUsedNames();
        StudentFactory.ResetStudentIdCounter();
    }
}
