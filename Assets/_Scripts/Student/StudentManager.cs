using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class StudentManager : Singleton<StudentManager>
{
    [SerializeField] private List<Student> _students = new();

    public event Action<List<Student>> OnStudentsChanged;
    public event Action<Student> OnStudentAdded;
    public event Action<Student> OnStudentRemoved;
    public event Action<Student> OnStudentModified;

    public IReadOnlyList<Student> Students => _students;
    public int GetStudentCount() => _students.Count;


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

    // Title로 돌아갈 때 명시적 해제
    public void Cleanup()
    {
        // 이벤트 구독 해제
        OnStudentsChanged = null;
        OnStudentAdded = null;
        OnStudentRemoved = null;
        OnStudentModified = null;

        // 데이터 초기화
        _students.Clear();

        // StudentFactory 초기화
        StudentFactory.ResetUsedNames();
        StudentFactory.ResetStudentIdCounter();
    }
}
