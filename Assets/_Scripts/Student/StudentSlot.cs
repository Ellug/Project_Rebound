using System;
using UnityEngine;
using UnityEngine.EventSystems;

public class StudentSlot : MonoBehaviour, IPointerClickHandler
{
    public enum SlotType
    {
        WaitList,
        StartingMember,
        SubMember
    }

    [SerializeField] private SlotType _slotType;

    // 슬롯에 배치된 학생 데이터를 저장
    private Student _assignedStudent;
    public Student AssignedStudent => _assignedStudent;
    public bool IsEmpty => _assignedStudent == null;
    public SlotType Type => _slotType;

    // 외부(매니저/팝업)에서 슬롯 클릭을 감지할 수 있도록 이벤트 제공
    public event Action<StudentSlot> OnSlotClicked;

    public void AssignStudent(Student student)
    {
        _assignedStudent = student;
        // TODO: 슬롯 위에 해당 학생의 초상화 이미지를 띄우는 UI 갱신 로직 추가
        Debug.Log($"[StudentSlot] {_slotType} 슬롯에 {student.studentName} 배치됨.");
    }

    public void ClearSlot()
    {
        _assignedStudent = null;
        // TODO: 슬롯 이미지를 다시 빈칸으로 갱신
        Debug.Log($"[StudentSlot] {_slotType} 슬롯 비워짐.");
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        OnSlotClicked?.Invoke(this);
    }
}