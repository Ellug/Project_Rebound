using System;
using System.Collections.Generic;
using AYellowpaper.SerializedCollections;
using UnityEngine;
using UnityEngine.UI;

// 학생 관리(배치) 팝업
// "포지션 정보" 버튼 클릭 시: 인스펙터에 세팅한 페이지를 UIPopupRequest.Guide로 표시
// 기존 PositionGuidePopup(씬 배치) 제거/통합된 구조에 맞춘 구현
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

    [Header("닫기")]
    [SerializeField] private Button _btnClose;

    // 인스펙터에서 페이지 데이터 설정 후 UIPopupRequest.Guide로 표시
    [Header("포지션 안내 (Guide Pages - Inspector)")]
    [SerializeField] private string _positionGuideTitle = "포지션 안내";
    [SerializeField] private List<PositionGuidePage> _positionGuidePages = new();

    // 포지션 안내 팝업에 표시할 페이지 데이터
    [Serializable]
    public sealed class PositionGuidePage
    {
        public string title;

        [TextArea(3, 10)]
        public string content;

        [TextArea(1, 5)]
        public string subMessage;

        public string imageId;  // Addressable 파일명 ID
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
        BindCloseButton();
        BindTournamentStartButton();
        BindStudentManagerEvents();

        SpawnStudentCards();
        BindSlotEvents();
        RestoreSlotAssignments();
        RefreshCardStates();
        RefreshRecommendHighlights();
        RefreshTournamentStartButton();
        RefreshCloseButton();
    }

    public override void Open()
    {
        base.Open();

        if (!_isInited)
            Init();

        RefreshAllViews();
    }

    public override void Close()
    {
        base.Close();

        // 닫힐 때 토너먼트 모드 초기화 (일반 열기 시 버튼 잔존 방지)
        _onTournamentStart = null;
        _isTournamentMode = false;
        RefreshTournamentStartButton();
        RefreshCloseButton();
    }

    private void OnDestroy()
    {
        UnbindStudentManagerEvents();
    }

    // 포지션 안내 (Guide)
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

        foreach (PositionGuidePage p in _positionGuidePages)
        {
            if (p == null) continue;

            result.Add(new UIPopupRequest.GuidePage
            {
                Title = p.title,
                Message = p.content,
                SubMessage = p.subMessage,
                PreviewImageId = string.IsNullOrWhiteSpace(p.imageId) ? null : p.imageId.Trim()
            });
        }

        return result;
    }

    // 토너먼트 시작 버튼
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

        // 콜백 실행 전 팝업을 먼저 닫아 상태 초기화
        Close();
        callback.Invoke();
    }

    // 토너먼트 모드이고 모든 슬롯이 채워졌을 때만 배치 완료 버튼 활성화
    private void RefreshTournamentStartButton()
    {
        if (_btnPlacementComplete == null) return;

        bool allSlotsFilled = _isTournamentMode && AreAllSlotsFilled();
        _btnPlacementComplete.gameObject.SetActive(allSlotsFilled);
    }

    // 모든 필드 슬롯에 학생이 배치되어 있는지 확인
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

    // StudentManager 이벤트 구독
    private void BindStudentManagerEvents()
    {
        if (StudentManager.Instance == null)
            return;

        StudentManager.Instance.OnStudentsChanged -= HandleStudentsChanged;
        StudentManager.Instance.OnStudentsChanged += HandleStudentsChanged;

        StudentManager.Instance.OnSlotAssignmentsChanged -= HandleSlotAssignmentsChanged;
        StudentManager.Instance.OnSlotAssignmentsChanged += HandleSlotAssignmentsChanged;
    }

    private void UnbindStudentManagerEvents()
    {
        if (StudentManager.Instance == null)
            return;

        StudentManager.Instance.OnStudentsChanged -= HandleStudentsChanged;
        StudentManager.Instance.OnSlotAssignmentsChanged -= HandleSlotAssignmentsChanged;
    }

    private void HandleStudentsChanged(List<Student> students)
    {
        if (!gameObject.activeInHierarchy)
            return;

        RefreshAllViews();
    }

    private void HandleSlotAssignmentsChanged(SerializedDictionary<int, Student> _)
    {
        if (!gameObject.activeInHierarchy)
            return;

        RestoreSlotAssignments();
        RefreshCardStates();
        RefreshRecommendHighlights();
        RefreshTournamentStartButton();
    }

    private void RefreshAllViews()
    {
        SpawnStudentCards();
        RestoreSlotAssignments();
        RefreshCardStates();
        RefreshRecommendHighlights();
        RefreshTournamentStartButton();
        RefreshCloseButton();
    }

    // StudentManager의 학생 목록을 기반으로 카드 프리팹을 인스턴스화
    private void SpawnStudentCards()
    {
        ClearCards();

        if (StudentManager.Instance == null) return;
        if (_cardPrefab == null || _cardRoot == null) return;

        foreach (Student student in StudentManager.Instance.Students)
        {
            GameObject obj = Instantiate(_cardPrefab, _cardRoot);
            StudentCard card = obj.GetComponent<StudentCard>();

            if (card != null)
            {
                card.SetStudentData(student);
                card.SetViewState(StudentCard.CardViewState.Normal);

                // 중복 등록 방지 후 클릭 이벤트 구독
                card.OnCardClicked -= HandleCardClicked;
                card.OnCardClicked += HandleCardClicked;

                _cardMap[student] = card;
            }

            obj.SetActive(true);
            _spawnedCards.Add(obj);
        }
    }

    // 카드 클릭 → 선택 학생 갱신 후 상세 정보 표시
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

    // 학생 상세 정보 팝업 열기
    private void ShowStudentInfo(Student student)
    {
        if (_studentInfoPopup == null) return;

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

            // 중복 등록 방지 후 슬롯 클릭 이벤트 구독
            slot.OnSlotClicked -= HandleSlotClicked;
            slot.OnSlotClicked += HandleSlotClicked;
        }
    }

    // 슬롯 클릭 처리
    // 빈 슬롯이면 선택 학생 배치, 이미 배치된 슬롯이면 제거/교체 확인 팝업 표시
    private void HandleSlotClicked(StudentSlot slot)
    {
        if (slot == null) return;

        if (!slot.IsEmpty)
        {
            // 클릭한 슬롯에 배치된 학생이 현재 선택된 학생 본인이면 → 제거 확인 팝업
            // 다른 학생이 선택되어 있으면 → 교체 확인 팝업
            if (_selectedStudent == null || slot.AssignedStudent == _selectedStudent)
                ShowRemoveConfirmPopup(slot);
            else
                ShowReplaceConfirmPopup(slot);
            return;
        }

        if (_selectedStudent == null) return;

        // 기존 슬롯에서 먼저 제거
        StudentSlot existing = FindSlotByStudent(_selectedStudent);
        if (existing != null && existing != slot)
        {
            int existingIndex = _fieldSlots.IndexOf(existing);
            existing.ClearSlot();
            StudentManager.Instance?.ClearSlot(existingIndex);
        }

        // 새 슬롯에 배치
        slot.AssignStudent(_selectedStudent, _selectedStudentPortrait);
        StudentManager.Instance?.AssignSlot(_fieldSlots.IndexOf(slot), _selectedStudent);

        CloseStudentInfoPopup();
        ClearSelection();
        RefreshCardStates();
        RefreshRecommendHighlights();
        RefreshTournamentStartButton();
    }

    // 선택된 학생 없이 배치된 슬롯 클릭 시 → 슬롯에서 빼기 확인 팝업
    private void ShowRemoveConfirmPopup(StudentSlot slot)
    {
        if (UIManager.Instance == null) return;

        UIManager.Instance.ShowPopup(new PopupData(
            title: "배치 제외 안내",
            content: "이미 같은 학생이 배치되어 있습니다.\n슬롯에서 제외하시겠습니까?",
            buttons: new List<PopupButtonInfo>
            {
                new PopupButtonInfo(() => { }),
                new PopupButtonInfo(() =>
                {
                    int slotIndex = _fieldSlots.IndexOf(slot);
                    slot.ClearSlot();
                    StudentManager.Instance?.ClearSlot(slotIndex);

                    RefreshCardStates();
                    RefreshRecommendHighlights();
                    RefreshTournamentStartButton();
                })
            }
        ));
    }

    // 선택된 학생이 있을 때 배치된 슬롯 클릭 시 → 교체 확인 팝업
    private void ShowReplaceConfirmPopup(StudentSlot slot)
    {
        if (UIManager.Instance == null) return;

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
                    StudentManager.Instance?.ClearSlot(slotIndex);

                    if (_selectedStudent != null)
                    {
                        // 선택 학생이 다른 슬롯에 있으면 먼저 제거
                        StudentSlot existing = FindSlotByStudent(_selectedStudent);
                        if (existing != null && existing != slot)
                        {
                            int existingIndex = _fieldSlots.IndexOf(existing);
                            existing.ClearSlot();
                            StudentManager.Instance?.ClearSlot(existingIndex);
                        }

                        slot.AssignStudent(_selectedStudent, _selectedStudentPortrait);
                        StudentManager.Instance?.AssignSlot(slotIndex, _selectedStudent);

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

    // StudentManager에 저장된 슬롯 배치 정보를 UI에 복원
    private void RestoreSlotAssignments()
    {
        Debug.Log($"[StudentManagementPopup] RestoreSlotAssignments | slotAssignments={(StudentManager.Instance != null ? StudentManager.Instance.SlotAssignments.Count : -1)}");

        if (StudentManager.Instance == null) return;

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

            // _cardMap에서 portrait 조회
            // null이어도 StudentSlot.AssignStudent() 내부에서 PortraitLibrary로 자동 조회(fallback)
            Sprite portrait = null;
            if (_cardMap.TryGetValue(student, out StudentCard card) && card != null)
                portrait = card.GetPortraitSprite();

            slot.AssignStudent(student, portrait);
        }

        RefreshTournamentStartButton();
    }

    // 특정 학생이 배치된 슬롯을 반환. 없으면 null
    private StudentSlot FindSlotByStudent(Student student)
    {
        if (student == null) return null;

        foreach (StudentSlot slot in _fieldSlots)
        {
            if (slot != null && slot.AssignedStudent == student)
                return slot;
        }

        return null;
    }

    // 슬롯에 배치된 학생인지 여부 확인
    private bool IsStudentAssigned(Student student) => FindSlotByStudent(student) != null;

    // 선택 학생 초기화 후 추천 강조 갱신
    private void ClearSelection()
    {
        _selectedStudent = null;
        _selectedStudentPortrait = null;
        RefreshRecommendHighlights();
    }

    // 배치 여부에 따라 카드 뷰 상태(Normal / Managing) 갱신
    private void RefreshCardStates()
    {
        foreach (var pair in _cardMap)
        {
            if (pair.Value == null) continue;

            bool isAssigned = IsStudentAssigned(pair.Key);
            pair.Value.SetViewState(isAssigned
                ? StudentCard.CardViewState.Managing
                : StudentCard.CardViewState.Normal);
        }
    }

    // 선택된 학생에게 추천되는 슬롯에만 하이라이트 표시
    private void RefreshRecommendHighlights()
    {
        foreach (StudentSlot slot in _fieldSlots)
        {
            if (slot == null) continue;

            bool isRecommended = _selectedStudent != null && slot.IsRecommendedFor(_selectedStudent);
            slot.SetRecommendHighlight(isRecommended);
        }
    }

    // 생성된 카드 오브젝트 및 매핑 정보 전체 삭제
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
        _studentInfoPopup?.Close();
    }

    // 팝업 닫기 버튼 바인딩
    private void BindCloseButton()
    {
        if (_btnClose == null) return;

        _btnClose.onClick.RemoveAllListeners();
        _btnClose.onClick.AddListener(() =>
        {
            Close();

            // LobbyUI 탭 스프라이트도 갱신
            LobbyUI lobbyUI = GetComponentInParent<LobbyUI>();
            lobbyUI?.OnClickStudentClose();
        });
    }

    // 토너먼트 모드에서는 닫기 버튼 숨김 (배치 완료만 허용)
    private void RefreshCloseButton()
    {
        if (_btnClose == null) return;

        _btnClose.gameObject.SetActive(!_isTournamentMode);
    }
}