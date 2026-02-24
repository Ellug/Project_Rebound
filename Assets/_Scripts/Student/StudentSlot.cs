using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class StudentSlot : MonoBehaviour, IPointerClickHandler
{
    // 슬롯 종류
    public enum SlotType
    {
        WaitList,       // 대기 명단
        FieldPosition,  // 필드 포지션
        SubMember       // 교체 멤버
    }

    [SerializeField] private SlotType _slotType;

    [Header("Field Position Only")]
    [SerializeField] private string _slotPositionName;           // 포지션 이름 (예: FW, GK 등)

    [Header("Recommend Highlight")]
    [SerializeField] private GameObject _recommendHighlightRoot; // 추천 강조 오브젝트

    [Header("Assigned Student Icon")]
    [SerializeField] private Image _imgAssignedIcon;             // 배치된 학생 아이콘

    private Student _assignedStudent;                            // 현재 배치된 학생
    private Sprite _assignedIconSprite;                          // 아이콘 캐싱

    public Student AssignedStudent => _assignedStudent;
    public bool IsEmpty => _assignedStudent == null;
    public SlotType Type => _slotType;
    public string SlotPositionName => _slotPositionName;
    public Sprite AssignedIconSprite => _assignedIconSprite;

    public event Action<StudentSlot> OnSlotClicked;             // 슬롯 클릭 이벤트

    // 해당 학생이 이 슬롯에 추천 대상인지 판단
    public bool IsRecommendedFor(Student student)
    {
        if (_slotType != SlotType.FieldPosition)
            return false;

        if (student == null)
            return false;

        if (string.IsNullOrEmpty(_slotPositionName))
            return false;

        if (string.IsNullOrEmpty(student.positionName))
            return false;

        return string.Equals(
            student.positionName,
            _slotPositionName,
            StringComparison.OrdinalIgnoreCase
        );
    }

    // 추천 강조 표시 On/Off
    public void SetRecommendHighlight(bool isOn)
    {
        if (_recommendHighlightRoot == null)
            return;

        if (_recommendHighlightRoot == gameObject)
            return;

        _recommendHighlightRoot.SetActive(isOn);
    }

    // 학생 배치
    public void AssignStudent(Student student, Sprite iconSprite)
    {
        _assignedStudent = student;
        _assignedIconSprite = iconSprite;

        ApplyAssignedIcon(iconSprite);

        Debug.Log($"[StudentSlot] {_slotPositionName} 슬롯에 {student.studentName} 배치됨.");
    }

    // 슬롯 비우기
    public void ClearSlot()
    {
        _assignedStudent = null;
        _assignedIconSprite = null;

        ApplyAssignedIcon(null);

        Debug.Log($"[StudentSlot] {_slotPositionName} 슬롯 비워짐.");
    }

    // 아이콘 적용
    private void ApplyAssignedIcon(Sprite sprite)
    {
        if (_imgAssignedIcon == null)
            return;

        bool has = sprite != null;

        _imgAssignedIcon.gameObject.SetActive(has);
        _imgAssignedIcon.sprite = sprite;

        if (has)
            _imgAssignedIcon.preserveAspect = true;
    }

    // UI 클릭 처리
    public void OnPointerClick(PointerEventData eventData)
    {
        OnSlotClicked?.Invoke(this);
    }
}