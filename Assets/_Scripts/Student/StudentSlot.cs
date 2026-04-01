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

    [SerializeField] private GameObject _disease;
    [SerializeField] private GameObject _injury;

    [Header("Portrait")]
    [SerializeField] private PortraitLibrary _portraitLibrary;   // iconSprite null 시 자동 조회용

    private Student _assignedStudent;                            // 현재 배치된 학생
    private Sprite _assignedIconSprite;                          // 아이콘 캐싱

    public Student AssignedStudent => _assignedStudent;
    public bool IsEmpty => _assignedStudent == null;
    public SlotType Type => _slotType;
    public string SlotPositionName => _slotPositionName;
    public Sprite AssignedIconSprite => _assignedIconSprite;

    public event Action<StudentSlot> OnSlotClicked;             // 슬롯 클릭 이벤트


    void OnEnable()
    {
        if (StudentManager.Instance != null)
            StudentManager.Instance.OnStudentModified += HandleStudentModified;
    }

    void OnDisable()
    {
        if (StudentManager.Instance != null)
            StudentManager.Instance.OnStudentModified -= HandleStudentModified;
    }

    private void HandleStudentModified(Student student)
    {
        // 슬롯에 있는 학생의 데이터가 바뀌면 부상 아이콘 상태 새로고침
        if (_assignedStudent != null && _assignedStudent.id == student.id)
        {
            _assignedStudent = student;
            RefreshAbnormalIndicator();
        }
    }
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
    // iconSprite가 null이면 PortraitLibrary에서 자동 조회
    // — RestoreSlotAssignments()에서 portrait을 못 가져오는 경우 대응
    public void AssignStudent(Student student, Sprite iconSprite)
    {
        _assignedStudent = student;

        if (iconSprite == null && student != null && _portraitLibrary != null)
            iconSprite = _portraitLibrary.Get(student.portraitColor, student.portraitIndex);

        _assignedIconSprite = iconSprite;
        ApplyAssignedIcon(iconSprite);
        RefreshAbnormalIndicator();

        Debug.Log($"[StudentSlot] {_slotPositionName} 슬롯에 {student.studentName} 배치됨.");
    }

    // 슬롯 비우기
    public void ClearSlot()
    {
        _assignedStudent = null;
        _assignedIconSprite = null;

        ApplyAssignedIcon(null);
        RefreshAbnormalIndicator();

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

    private void RefreshAbnormalIndicator()
    {
        if (_assignedStudent == null)
        {
            SafeSetActive(_disease, false);
            SafeSetActive(_injury, false);
            return;
        }

        SafeSetActive(_disease, _assignedStudent.abnormalState == Student.AbnormalType.Disease);
        SafeSetActive(_injury, _assignedStudent.abnormalState == Student.AbnormalType.Injury);
    }

    private static void SafeSetActive(GameObject target, bool active)
    {
        if (target != null)
            target.SetActive(active);
    }

    // UI 클릭 처리
    public void OnPointerClick(PointerEventData eventData)
    {
        OnSlotClicked?.Invoke(this);
    }
}