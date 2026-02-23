using System.Collections.Generic;
using UnityEngine;

// 학생 관리(배치) 팝업
// - 카드 기본: Normal(초상화만)
// - 카드 선택: Managing(배치중 오버레이)
// - 이미 배치된 학생 카드: Managing 오버레이 항상 표시
// - 슬롯 클릭:
//   - 슬롯에 학생이 있으면: 학생 빼기 팝업
//   - 슬롯이 비어있으면: 선택된 학생 배치(중복 배치 방지: 기존 슬롯에서 자동 해제 후 이동)
// - 추천 슬롯 강조: 선택된 학생 포지션과 슬롯 포지션 일치 시 강조
public class StudentManagementPopup : UIBase
{
    [Header("학생 카드 영역")]
    [SerializeField] private Transform _cardRoot;
    [SerializeField] private GameObject _cardPrefab;

    [Header("포지션 슬롯")]
    [SerializeField] private List<StudentSlot> _fieldSlots = new();

    [Header("학생 상세 팝업")]
    [SerializeField] private SelectStudentInfoPopup _studentInfoPopup;

    private readonly List<GameObject> _spawnedCards = new();
    private readonly Dictionary<Student, StudentCard> _cardMap = new();

    private Student _selectedStudent;
    private Sprite _selectedStudentPortrait;

    private bool _isInited;

    public override void Init()
    {
        if (_isInited) return;
        _isInited = true;

        base.Init();

        SpawnStudentCards();
        BindSlotEvents();
        RefreshCardStates();
        RefreshRecommendHighlights();
    }

    public override void Open()
    {
        base.Open();

        if (!_isInited)
            Init();

        SpawnStudentCards();
        RefreshCardStates();
        RefreshRecommendHighlights();
    }

    private void SpawnStudentCards()
    {
        ClearCards();

        if (StudentManager.Instance == null)
            return;

        if (_cardPrefab == null || _cardRoot == null)
            return;

        foreach (Student student in StudentManager.Instance.Students)
        {
            GameObject obj = Instantiate(_cardPrefab, _cardRoot);
            StudentCard card = obj.GetComponent<StudentCard>();

            if (card != null)
            {
                card.SetStudentData(student);
                card.SetViewState(StudentCard.CardViewState.Normal);

                card.OnCardClicked -= HandleCardClicked;
                card.OnCardClicked += HandleCardClicked;

                _cardMap[student] = card;
            }

            obj.SetActive(true);
            _spawnedCards.Add(obj);
        }
    }

    private void HandleCardClicked(StudentCard card)
    {
        if (card == null) return;

        Student student = card.GetStudentData();
        if (student == null) return;

        if (_selectedStudent == student)
        {
            ClearSelection();
            return;
        }

        _selectedStudent = student;
        _selectedStudentPortrait = card.GetPortraitSprite();

        RefreshCardStates();
        RefreshRecommendHighlights();
        ShowStudentInfo(student);
    }

    private void ShowStudentInfo(Student student)
    {
        if (_studentInfoPopup == null)
            return;

        _studentInfoPopup.Init();
        _studentInfoPopup.Setup("선택한 학생", student, null);
        _studentInfoPopup.transform.SetAsLastSibling();
        _studentInfoPopup.Open();
    }

    private void BindSlotEvents()
    {
        foreach (StudentSlot slot in _fieldSlots)
        {
            if (slot == null) continue;

            slot.OnSlotClicked -= HandleSlotClicked;
            slot.OnSlotClicked += HandleSlotClicked;
        }
    }

    private void HandleSlotClicked(StudentSlot slot)
    {
        if (slot == null) return;

        // 슬롯에 이미 학생이 있으면: 무조건 학생 빼기 팝업
        // (같은 슬롯에 같은 학생을 배치하려는 경우도 여기로 들어옴)
        if (!slot.IsEmpty)
        {
            ShowRemoveConfirmPopup(slot);
            return;
        }

        // 빈 슬롯인데 선택된 학생이 없으면 아무 것도 안 함
        if (_selectedStudent == null)
            return;

        // 중복 배치 방지: 다른 슬롯에 이미 배치된 학생이면 그 슬롯에서 먼저 빼고 이동
        StudentSlot existing = FindSlotByStudent(_selectedStudent);
        if (existing != null && existing != slot)
        {
            existing.ClearSlot();
        }

        slot.AssignStudent(_selectedStudent, _selectedStudentPortrait);

        ClearSelection();
        RefreshCardStates();
        RefreshRecommendHighlights();
    }

    private void ShowRemoveConfirmPopup(StudentSlot slot)
    {
        if (UIManager.Instance == null)
            return;

        Student assigned = slot.AssignedStudent;
        string name = assigned != null ? assigned.studentName : "학생";

        UIManager.Instance.ShowPopup(new PopupData(
            title: "학생 빼기",
            content: $"{name}을(를) 이 슬롯에서 빼시겠습니까?",
            buttons: new List<PopupButtonInfo>
            {
            new PopupButtonInfo("취소", null),
            new PopupButtonInfo("확인", () =>
            {
                slot.ClearSlot();
                ClearSelection(); // 선택중 카드도 Normal로
                RefreshCardStates();
                RefreshRecommendHighlights();
            })
            }
        ));
    }

    private StudentSlot FindSlotByStudent(Student student)
    {
        if (student == null) return null;

        foreach (StudentSlot slot in _fieldSlots)
        {
            if (slot == null) continue;
            if (slot.AssignedStudent == student)
                return slot;
        }

        return null;
    }

    private bool IsStudentAssigned(Student student)
    {
        return FindSlotByStudent(student) != null;
    }

    private void ClearSelection()
    {
        _selectedStudent = null;
        _selectedStudentPortrait = null;

        RefreshCardStates();
        RefreshRecommendHighlights();
    }

    private void RefreshCardStates()
    {
        foreach (var pair in _cardMap)
        {
            Student student = pair.Key;
            StudentCard card = pair.Value;

            if (card == null) continue;

            bool isSelected = _selectedStudent != null && student == _selectedStudent;
            bool isAssigned = IsStudentAssigned(student);

            // 이미 배치된 학생은 Managing 오버레이 유지
            if (isSelected || isAssigned)
                card.SetViewState(StudentCard.CardViewState.Managing);
            else
                card.SetViewState(StudentCard.CardViewState.Normal);
        }
    }

    private void RefreshRecommendHighlights()
    {
        foreach (StudentSlot slot in _fieldSlots)
        {
            if (slot == null) continue;

            bool isRecommended = _selectedStudent != null && slot.IsRecommendedFor(_selectedStudent);
            slot.SetRecommendHighlight(isRecommended);
        }
    }

    private void ClearCards()
    {
        foreach (GameObject obj in _spawnedCards)
        {
            if (obj != null)
                Destroy(obj);
        }

        _spawnedCards.Clear();
        _cardMap.Clear();
    }
}