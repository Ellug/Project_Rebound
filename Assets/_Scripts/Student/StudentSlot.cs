using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class StudentSlot : MonoBehaviour, IPointerClickHandler
{
    public enum SlotType
    {
        WaitList,
        FieldPosition,
        SubMember
    }

    [SerializeField] private SlotType _slotType;

    [Header("Field Position Only")]
    [SerializeField] private string _slotPositionName;

    [Header("Recommend Highlight")]
    [SerializeField] private GameObject _recommendHighlightRoot;

    [Header("Assigned Student Icon")]
    [SerializeField] private Image _imgAssignedIcon;

    private Student _assignedStudent;
    private Sprite _assignedIconSprite;

    public Student AssignedStudent => _assignedStudent;
    public bool IsEmpty => _assignedStudent == null;
    public SlotType Type => _slotType;
    public string SlotPositionName => _slotPositionName;
    public Sprite AssignedIconSprite => _assignedIconSprite;

    public event Action<StudentSlot> OnSlotClicked;

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

    public void SetRecommendHighlight(bool isOn)
    {
        if (_recommendHighlightRoot == null)
            return;

        if (_recommendHighlightRoot == gameObject)
            return;

        _recommendHighlightRoot.SetActive(isOn);
    }

    public void AssignStudent(Student student, Sprite iconSprite)
    {
        _assignedStudent = student;
        _assignedIconSprite = iconSprite;

        ApplyAssignedIcon(iconSprite);

        Debug.Log($"[StudentSlot] {_slotPositionName} 슬롯에 {student.studentName} 배치됨.");
    }

    public void ClearSlot()
    {
        _assignedStudent = null;
        _assignedIconSprite = null;

        ApplyAssignedIcon(null);

        Debug.Log($"[StudentSlot] {_slotPositionName} 슬롯 비워짐.");
    }

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

    public void OnPointerClick(PointerEventData eventData)
    {
        OnSlotClicked?.Invoke(this);
    }
}