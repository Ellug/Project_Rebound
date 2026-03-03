using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

// 학생 관리(배치) 팝업
// - "포지션 정보" 버튼 클릭 시: 인스펙터에 세팅한 페이지를 UIPopupRequest.Guide로 표시
// - 기존 PositionGuidePopup(씬 배치) 제거/통합된 구조에 맞춘 구현
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

    // ✅ 포지션 안내는 원래 "인스펙터 페이지" 기반이었음 → 그대로 유지
    // ✅ PositionGuidePopup 삭제(통합) 이후: UIPopupRequest.Guide로 띄우는 데이터 소스로 흡수
    [Header("포지션 안내 (Guide Pages - Inspector)")]
    [SerializeField] private string _positionGuideTitle = "포지션 안내";
    [SerializeField] private List<PositionGuidePage> _positionGuidePages = new();

    [Serializable]
    public sealed class PositionGuidePage
    {
        public string title;

        [TextArea(3, 10)]
        public string content;

        [TextArea(1, 5)]
        public string subMessage;

        public Sprite image;
    }

    [Header("토너먼트 시작")]
    [SerializeField] private Button _btnPlacementComplete;              // 배치 완료 버튼

    private readonly List<GameObject> _spawnedCards = new();            // 생성된 카드
    private readonly Dictionary<Student, StudentCard> _cardMap = new(); // 학생-카드 매핑

    private Student _selectedStudent;                                   // 현재 선택된 학생
    private Sprite _selectedStudentPortrait;                            // 선택 학생 초상화

    private Action _onTournamentStart;                                  // 토너먼트 시작 콜백
    private bool _isTournamentMode;                                     // 토너먼트 진입 모드 여부

    private bool _isInited;

    // GameManager 슬롯 자동 배치 및 LobbyUI 경유 접근용
    public List<StudentSlot> GetFieldSlots() => _fieldSlots;

    // 토너먼트 시작 콜백 주입
    public void SetTournamentStartCallback(Action onTournamentStart)
    {
        _onTournamentStart = onTournamentStart;
        _isTournamentMode = onTournamentStart != null;
    }

    public override void Init()
    {
        if (_isInited) return;
        _isInited = true;

        base.Init();

        BindPositionGuideButton();
        BindTournamentStartButton();

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
        RestoreSlotAssignments();
        RefreshCardStates();
        RefreshRecommendHighlights();

        // 토너먼트 모드일 때만 배치 완료 버튼 표시
        RefreshTournamentStartButton();
    }

    public override void Close()
    {
        base.Close();

        // 닫힐 때 토너먼트 모드 초기화 (일반 열기 시 버튼 잔존 방지)
        _onTournamentStart = null;
        _isTournamentMode = false;
        RefreshTournamentStartButton();
    }

    // ───────────────────────────────────────────────────────────────
    // 포지션 안내 (Guide)
    // ───────────────────────────────────────────────────────────────

    private void BindPositionGuideButton()
    {
        if (_btnPositionGuide == null)
            return;

        _btnPositionGuide.onClick.RemoveAllListeners();
        _btnPositionGuide.onClick.AddListener(OpenPositionGuide);
    }

    // "포지션 정보" 버튼 클릭 → Guide 팝업 오픈
    private void OpenPositionGuide()
    {
        if (UIManager.Instance == null)
        {
            Debug.LogWarning("[StudentManagementPopup] UIManager.Instance가 없어 포지션 안내를 표시할 수 없습니다.");
            return;
        }

        List<UIPopupRequest.GuidePage> pages = BuildPositionGuidePages();
        if (pages == null || pages.Count == 0)
        {
            Debug.LogWarning("[StudentManagementPopup] 포지션 안내 페이지가 비어있습니다. (Inspector 설정 필요)");
            return;
        }

        UIPopupRequest req = UIPopupRequest.Guide(
            title: string.IsNullOrWhiteSpace(_positionGuideTitle) ? "포지션 안내" : _positionGuideTitle,
            pages: pages,
            onClose: null,
            onCancel: null
        );

        // 버튼 위치 고정: Cancel 숨김, 마지막 페이지 Close로 종료
        req.ShowCancel = false;
        req.AutoCloseOnPrimary = true;
        req.AutoCloseOnCancel = true;

        UIManager.Instance.ShowPopup(req);
    }

    // 인스펙터 페이지 -> UIPopupRequest.GuidePage 변환
    private List<UIPopupRequest.GuidePage> BuildPositionGuidePages()
    {
        List<UIPopupRequest.GuidePage> result = new();

        if (_positionGuidePages == null || _positionGuidePages.Count == 0)
            return result;

        for (int i = 0; i < _positionGuidePages.Count; i++)
        {
            PositionGuidePage p = _positionGuidePages[i];
            if (p == null) continue;

            result.Add(new UIPopupRequest.GuidePage
            {
                Title = p.title,
                Message = p.content,
                SubMessage = p.subMessage,
                PreviewSprite = p.image
            });
        }

        return result;
    }

    // ───────────────────────────────────────────────────────────────
    // 토너먼트 시작 버튼
    // ───────────────────────────────────────────────────────────────

    private void BindTournamentStartButton()
    {
        if (_btnPlacementComplete == null)
            return;

        _btnPlacementComplete.onClick.RemoveAllListeners();
        _btnPlacementComplete.onClick.AddListener(HandleTournamentStartClicked);

        // 기본 숨김
        _btnPlacementComplete.gameObject.SetActive(false);
    }

    private void HandleTournamentStartClicked()
    {
        if (_onTournamentStart == null) return;

        Action callback = _onTournamentStart;

        // 콜백 실행 전 팝업 닫기 및 상태 초기화
        Close();

        callback.Invoke();
    }

    private void RefreshTournamentStartButton()
    {
        if (_btnPlacementComplete == null) return;

        bool allSlotsFilled = _isTournamentMode && AreAllSlotsFilled();
        _btnPlacementComplete.gameObject.SetActive(allSlotsFilled);
    }

    private bool AreAllSlotsFilled()
    {
        if (_fieldSlots == null || _fieldSlots.Count == 0)
            return false;

        foreach (StudentSlot slot in _fieldSlots)
        {
            if (slot == null || slot.IsEmpty)
                return false;
        }

        return true;
    }

    // ───────────────────────────────────────────────────────────────
    // 이하: 기존 StudentManagementPopup 로직 (너가 이미 쓰던 코드 그대로)
    // - 학생 카드 생성/선택/슬롯 배치/추천 강조/복원 등
    // ───────────────────────────────────────────────────────────────

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
        _studentInfoPopup.Setup("선택한 학생", student, _selectedStudentPortrait);
        _studentInfoPopup.transform.SetAsLastSibling();

        if (!_studentInfoPopup.gameObject.activeSelf)
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
            int existingIndex = _fieldSlots.IndexOf(existing);
            existing.ClearSlot();
            if (StudentManager.Instance != null)
                StudentManager.Instance.ClearSlot(existingIndex);
        }

        slot.AssignStudent(_selectedStudent, _selectedStudentPortrait);
        if (StudentManager.Instance != null)
            StudentManager.Instance.AssignSlot(_fieldSlots.IndexOf(slot), _selectedStudent);

        CloseStudentInfoPopup();

        ClearSelection();
        RefreshCardStates();
        RefreshRecommendHighlights();
        RefreshTournamentStartButton();
    }

    private void ShowRemoveConfirmPopup(StudentSlot slot)
    {
        if (UIManager.Instance == null)
            return;

        UIManager.Instance.ShowPopup(new PopupData(
            title: "배치 변경 안내",
            content: "이미 배치된 학생이 있습니다. \n선택한 학생으로 교체하시겠습니까?",
            buttons: new List<PopupButtonInfo>
            {
                new PopupButtonInfo(() => { }),
                new PopupButtonInfo(() =>
                {
                    int slotIndex = _fieldSlots.IndexOf(slot);

                    slot.ClearSlot();
                    if (StudentManager.Instance != null)
                        StudentManager.Instance.ClearSlot(slotIndex);

                    if (_selectedStudent != null)
                    {
                        StudentSlot existing = FindSlotByStudent(_selectedStudent);
                        if (existing != null && existing != slot)
                        {
                            int existingIndex = _fieldSlots.IndexOf(existing);
                            existing.ClearSlot();
                            if (StudentManager.Instance != null)
                                StudentManager.Instance.ClearSlot(existingIndex);
                        }

                        slot.AssignStudent(_selectedStudent, _selectedStudentPortrait);
                        if (StudentManager.Instance != null)
                            StudentManager.Instance.AssignSlot(slotIndex, _selectedStudent);

                        CloseStudentInfoPopup();
                    }

                    ClearSelection();
                    RefreshCardStates();
                    RefreshRecommendHighlights();
                    RefreshTournamentStartButton();
                })
            }
        ));
    }

    private void RestoreSlotAssignments()
    {
        if (StudentManager.Instance == null)
            return;

        for (int i = 0; i < _fieldSlots.Count; i++)
        {
            StudentSlot slot = _fieldSlots[i];
            if (slot == null) continue;

            Student student = StudentManager.Instance.GetAssignedStudent(i);
            if (student == null)
            {
                slot.ClearSlot();
                continue;
            }

            Sprite portrait = null;
            if (_cardMap.TryGetValue(student, out StudentCard card) && card != null)
                portrait = card.GetPortraitSprite();

            slot.AssignStudent(student, portrait);
            RefreshTournamentStartButton();
        }
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

    private void CloseStudentInfoPopup()
    {
        if (_studentInfoPopup == null)
            return;

        _studentInfoPopup.Close();
    }
}