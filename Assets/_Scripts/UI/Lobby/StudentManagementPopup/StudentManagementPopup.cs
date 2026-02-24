using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

// 학생 관리(배치) 팝업
public class StudentManagementPopup : UIBase
{
    [Header("학생 카드 영역")]
    [SerializeField] private Transform _cardRoot;                       // 카드 부모
    [SerializeField] private GameObject _cardPrefab;                    // 카드 프리팹

    [Header("포지션 슬롯")]
    [SerializeField] private List<StudentSlot> _fieldSlots = new();     // 배치 슬롯 목록

    [Header("학생 상세 팝업")]
    [SerializeField] private SelectStudentInfoPopup _studentInfoPopup;  // 상세 정보 팝업

    [Header("포지션 안내")]
    [SerializeField] private Button _btnPositionGuide;                  // "포지션 정보" 버튼
    [SerializeField] private PositionGuidePopup _positionGuidePopup;    // 씬에 미리 배치된 팝업

    private readonly List<GameObject> _spawnedCards = new();            // 생성된 카드
    private readonly Dictionary<Student, StudentCard> _cardMap = new(); // 학생-카드 매핑

    private Student _selectedStudent;                                   // 현재 선택된 학생
    private Sprite _selectedStudentPortrait;                            // 선택 학생 초상화

    private bool _isInited;

    public override void Init()
    {
        if (_isInited) return;
        _isInited = true;

        base.Init();
        BindPositionGuideButton();
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

    // 포지션 정보 버튼 이벤트 연결
    private void BindPositionGuideButton()
    {
        if (_btnPositionGuide == null)
            return;

        _btnPositionGuide.onClick.RemoveAllListeners();
        _btnPositionGuide.onClick.AddListener(OpenPositionGuidePopup);
    }

    // 씬에 미리 배치된 PositionGuidePopup을 활성화하여 표시
    private void OpenPositionGuidePopup()
    {
        if (_positionGuidePopup == null)
        {
            Debug.LogWarning("[StudentManagementPopup] PositionGuidePopup 참조가 없습니다.");
            return;
        }

        // 비활성 상태라면 먼저 활성화
        if (!_positionGuidePopup.gameObject.activeSelf)
            _positionGuidePopup.gameObject.SetActive(true);

        // 최상단으로 올려서 다른 UI 위에 표시
        _positionGuidePopup.transform.SetAsLastSibling();

        // Init 누락 방지
        _positionGuidePopup.Init();
        _positionGuidePopup.Open();
    }

    // 학생 카드 생성
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

    // 카드 클릭 처리
    private void HandleCardClicked(StudentCard card)
    {
        if (card == null) return;

        Student student = card.GetStudentData();
        if (student == null) return;

        _selectedStudent = student;
        _selectedStudentPortrait = card.GetPortraitSprite();

        RefreshCardStates();
        RefreshRecommendHighlights();
        ShowStudentInfo(student);
    }

    // 상세 정보 팝업 표시
    private void ShowStudentInfo(Student student)
    {
        if (_studentInfoPopup == null)
            return;

        _studentInfoPopup.Init();
        _studentInfoPopup.Setup("선택한 학생", student, null);
        _studentInfoPopup.transform.SetAsLastSibling();
        _studentInfoPopup.Open();
    }

    // 슬롯 클릭 이벤트 바인딩
    private void BindSlotEvents()
    {
        foreach (StudentSlot slot in _fieldSlots)
        {
            if (slot == null) continue;

            slot.OnSlotClicked -= HandleSlotClicked;
            slot.OnSlotClicked += HandleSlotClicked;
        }
    }

    // 슬롯 클릭 처리
    private void HandleSlotClicked(StudentSlot slot)
    {
        if (slot == null) return;

        if (!slot.IsEmpty)
        {
            ShowRemoveConfirmPopup(slot);
            return;
        }

        if (_selectedStudent == null)
            return;

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
                    ClearSelection();
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

        RefreshRecommendHighlights();
    }

    // 배치된 학생은 Managing, 나머지는 Normal
    private void RefreshCardStates()
    {
        foreach (var pair in _cardMap)
        {
            Student student = pair.Key;
            StudentCard card = pair.Value;

            if (card == null) continue;

            bool isAssigned = IsStudentAssigned(student);
            card.SetViewState(isAssigned
                ? StudentCard.CardViewState.Managing
                : StudentCard.CardViewState.Normal);
        }
    }

    // 추천 슬롯 강조 갱신
    private void RefreshRecommendHighlights()
    {
        foreach (StudentSlot slot in _fieldSlots)
        {
            if (slot == null) continue;

            bool isRecommended = _selectedStudent != null && slot.IsRecommendedFor(_selectedStudent);
            slot.SetRecommendHighlight(isRecommended);
        }
    }

    // 생성된 카드 정리
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