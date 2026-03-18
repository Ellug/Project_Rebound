using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// 학생 영입 팝업 (후보 카드 표시/선택/확정 흐름)
public class RecruitmentPopup : UIPopup
{
    [Header("Scroll")]
    [SerializeField] private ScrollRect _scrollRect;          // 스크롤 영역

    [Header("Header")]
    [SerializeField] private TMP_Text _txtSelectCount;   // 현재 인원
    [SerializeField] private TMP_Text _txtCapacity;      // 정원

    [Header("Close")]
    [SerializeField] private Button _btnRecuitmentClose;                // 닫기 버튼

    [Header("Card")]
    [SerializeField] private Transform _cardRoot;             // 카드 부모 (Grid/VerticalLayout 등)
    [SerializeField] private GameObject _cardPrefab;          // 카드 프리팹

    [Header("Complete Button")]
    [SerializeField] private Button _btnComplete;             // 선택 완료 버튼
    [SerializeField] private TMP_Text _txtComplete;           // 완료 버튼 텍스트

    [Header("Overlays")]
    [SerializeField] private SelectStudentInfoPopup _studentInfoPopup; // 학생 상세 정보 오버레이

    private readonly List<GameObject> _spawnedCards = new();             // 생성된 카드 인스턴스 목록
    private readonly List<Student> _selectedStudents = new();            // 현재 선택된 학생 목록
    private readonly Dictionary<Student, StudentCard> _cardMap = new();  // 학생 -> 카드 매핑 (상태 갱신용)
    private IReadOnlyList<Student> _candidates;                           // 외부에서 주입받는 영입 후보 목록

    private int _maxRecruitCount = 0; // 최대 모집 가능 인원 (0이면 제한 없음)

    public event Action<List<Student>> OnRecruitmentConfirmed; // 최종 확정 콜백 (선택 학생 스냅샷 전달)
    public event Action OnCancelled;                            // 영입 취소(닫기) 콜백

    private int _capacity = 8;
    private int _ownedCount = 0;

    // 후보 리스트 주입 (StudentManager와 분리: 후보는 Popup에서만 사용)
    public void SetCandidates(IReadOnlyList<Student> candidates)
    {
        _candidates = candidates;
    }

    public void SetMaxRecruitCount(int max)
    {
        _maxRecruitCount = Mathf.Max(0, max);
    }

    public override void Init()
    {
        base.Init();
        DisableBackKey = true; // Esc/Back으로 영입창 닫힘 방지

        // 버튼 이벤트 바인딩 (중복 등록 방지)
        if (_btnRecuitmentClose != null)
        {
            _btnRecuitmentClose.onClick.RemoveAllListeners();
            _btnRecuitmentClose.onClick.AddListener(HandleCloseButton);
        }

        if (_btnComplete != null)
        {
            _btnComplete.onClick.RemoveAllListeners();
            _btnComplete.onClick.AddListener(HandleCompleteButton);
        }

        // 내부 상태 초기화
        _selectedStudents.Clear();
        _cardMap.Clear();

        // 후보 카드 생성 및 UI 갱신
        SpawnCandidateCards();
        RefreshHeader();
        RefreshCompleteButton();
    }

    public override void Open()
    {
        base.Open();
        StartCoroutine(ForceScrollTopRoutineSafe()); // 레이아웃 갱신 후 스크롤 최상단 고정
    }

    // 후보 학생 카드 생성
    private void SpawnCandidateCards()
    {
        ClearCards();

        if (_candidates == null || _candidates.Count == 0)
        {
            Debug.LogWarning("[RecruitmentPopup] 영입 후보 학생이 없습니다.");
            return;
        }

        foreach (Student student in _candidates)
        {
            CreateCandidateCard(student);
        }
    }

    // 카드 1개 생성 및 클릭 이벤트 연결
    private void CreateCandidateCard(Student student)
    {
        if (_cardPrefab == null) return;

        GameObject cardObj = Instantiate(_cardPrefab, _cardRoot);
        StudentCard studentCard = cardObj.GetComponent<StudentCard>();

        if (studentCard != null)
        {
            studentCard.SetStudentData(student);
            studentCard.SetViewState(StudentCard.CardViewState.Normal);

            // 람다 캡처 안전 처리
            Student captured = student;
            studentCard.OnCardClicked += card => HandleCardClicked(captured, card);

            _cardMap[captured] = studentCard;
        }

        cardObj.SetActive(true);
        _spawnedCards.Add(cardObj);
    }

    // 카드 클릭 처리: 정보 팝업 오픈 (선택은 정보 팝업 내 버튼에서 처리)
    private void HandleCardClicked(Student student, StudentCard card)
    {
        if (student == null || card == null) return;

        // 이미 선택된 경우 → 정보 팝업 열고 취소 확인 버튼 연결
        if (_selectedStudents.Contains(student))
        {
            ShowStudentInfoPopup(student, isSelected: true);
            return;
        }

        // 정원 꽉 차면 추가 선택 불가
        if (!CanSelectMore())
        {
            ShowMaxReachedPopup();
            return;
        }

        // 팝업 내 최대 모집 인원 제한(설정값)도 같이 체크
        if (IsMaxReached())
        {
            ShowMaxReachedPopup();
            return;
        }

        // 선택하지 않고 정보 팝업만 열고, 선택 버튼 연결
        ShowStudentInfoPopup(student, isSelected: false);
    }

    // 학생 정보 팝업 표시
    // isSelected: true -> 선택 해제 / false -> 학생 선택
    private void ShowStudentInfoPopup(Student student, bool isSelected)
    {
        if (_studentInfoPopup == null)
        {
            Debug.LogWarning("[RecruitmentPopup] _studentInfoPopup이 연결되지 않았습니다.");
            return;
        }

        Sprite portrait = null;
        if (_cardMap.TryGetValue(student, out StudentCard card) && card != null)
            portrait = card.GetPortraitSprite();

        _studentInfoPopup.Init();
        _studentInfoPopup.Setup("학생 정보", student, portrait);
        _studentInfoPopup.transform.SetAsLastSibling();

        if (isSelected)
        {
            // 이미 선택된 학생 → 선택 버튼을 "선택 해제" 흐름으로 연결
            _studentInfoPopup.SetSelectAction(() =>
                ShowUnselectConfirmPopup(student, card),
                buttonText: "선택 해제"
            );
        }
        else
        {
            // 미선택 학생 → 선택 버튼 클릭 시 선택 처리 + 팝업 닫기
            _studentInfoPopup.SetSelectAction(() =>
                {
                    SelectStudent(student, card);
                    CloseStudentInfoPopup();
                },
                buttonText: "학생 선택"
            );
        }

        // 이미 열려있으면 슬라이드 없이 데이터만 갱신, 닫혀있으면 슬라이드 인
        if (!_studentInfoPopup.gameObject.activeSelf)
            _studentInfoPopup.Open();
    }

    // 학생 선택 처리 (카드 상태 변경 + UI 갱신)
    private void SelectStudent(Student student, StudentCard card)
    {
        if (_selectedStudents.Contains(student))
            return;

        _selectedStudents.Add(student);
        card.SetViewState(StudentCard.CardViewState.Placing);

        RefreshHeader();
        RefreshCompleteButton();
    }

    // 선택 취소 확인 팝업
    private void ShowUnselectConfirmPopup(Student student, StudentCard card)
    {
        if (UIManager.Instance == null) return;

        UIManager.Instance.ShowPopup(new PopupData(
            title: "영입 취소",
            content: "해당 학생 선택을 취소하시겠습니까?",
            buttons: new List<PopupButtonInfo>
            {
                new PopupButtonInfo(() => { }),
                new PopupButtonInfo(() =>
                {
                    UnselectStudent(student, card);
                    CloseStudentInfoPopup();
                })
            }
        ));
    }

    // 선택 취소 처리
    private void UnselectStudent(Student student, StudentCard card)
    {
        if (!_selectedStudents.Contains(student))
            return;

        _selectedStudents.Remove(student);
        card.SetViewState(StudentCard.CardViewState.Normal);

        RefreshHeader();
        RefreshCompleteButton();
    }

    // 학생 정보 팝업 슬라이드 아웃으로 닫기
    private void CloseStudentInfoPopup()
    {
        if (_studentInfoPopup != null && _studentInfoPopup.gameObject.activeSelf)
            _studentInfoPopup.Close();
    }

    // 최대 모집 인원 도달 여부
    private bool IsMaxReached()
    {
        return _maxRecruitCount > 0 && _selectedStudents.Count >= _maxRecruitCount;
    }

    // 최대 인원 경고 팝업
    private void ShowMaxReachedPopup()
    {
        if (UIManager.Instance == null) return;

        UIManager.Instance.ShowPopup(new PopupData(
            title: "최대 인원 도달",
            content: "더 이상 모집이 불가능합니다.",
            buttons: new List<PopupButtonInfo>
            {
                new PopupButtonInfo(() => { })
            }
        ));
    }

    // 영입 완료 버튼 클릭 → 최종 확정 팝업으로 진행
    private void HandleCompleteButton()
    {
        if (_selectedStudents.Count == 0) return;
        if (UIManager.Instance == null) return;

        // 콜백 전달용 스냅샷 (리스트 변경 방지)
        List<Student> snapshot = new(_selectedStudents);

        // 영입 완료 팝업
        ShowJoinCompletePopup(snapshot);
    }

    // 최종 영입 완료 팝업
    private void ShowJoinCompletePopup(List<Student> recruits)
    {
        if (UIManager.Instance == null) return;

        UIManager.Instance.ShowPopup(new PopupData(
            title: "학생 영입",
            content: "새로운 학생이 팀에 합류했습니다.\n여기 팀 운영에 큰 변화를 불러올 것입니다.",
            buttons: new List<PopupButtonInfo>
            {
                new PopupButtonInfo(() =>
                {
                    OnRecruitmentConfirmed?.Invoke(recruits);
                    CloseAndDestroy();
                })
            }
        ));
    }

    // 닫기 버튼 처리 (영입 종료 확인)
    private void HandleCloseButton()
    {
        if (UIManager.Instance == null) return;

        var req = UIPopupRequest.Default(
            title: "영입 취소",
            message: "영입을 종료하고 로비로 돌아가시겠습니까?",
            onPrimary: () =>
            {
                OnCancelled?.Invoke();
                CloseAndDestroy();
            },
            onCancel: () => { },
            subMessage: null,
            previewImageId: null,
            showCancel: true,
            primaryKind: UIPopupRequest.PrimaryButtonKind.Confirm
        );

        req.AutoCloseOnPrimary = true;
        req.AutoCloseOnCancel = true;

        UIManager.Instance.ShowPopup(req);
    }

    protected override void OnCloseButtonClicked()
    {
        HandleCloseButton();
    }

    // 상단 선택 인원 표시 갱신
    private void RefreshHeader()
    {
        if (_txtSelectCount != null)
            _txtSelectCount.text = $"{_ownedCount + _selectedStudents.Count}";

        if (_txtCapacity != null)
            _txtCapacity.text = $"{_capacity}";
    }

    // 완료 버튼 표시 상태 갱신 (선택이 있을 때만 노출)
    private void RefreshCompleteButton()
    {
        bool hasSelection = _selectedStudents.Count > 0;
        // 보유 + 선택 인원이 5명 이상이어야 영입 확정 가능
        int totalCount = _ownedCount + _selectedStudents.Count;
        bool meetsMinimum = totalCount >= 5;
        bool rosterFull = GetRemainingCapacity() <= 0;

        // 버튼 표시 조건: (선택 있음) && (보유+선택 5명 이상) && 정원 초과 아님
        bool showButton = hasSelection && meetsMinimum && !rosterFull;

        if (_btnComplete != null)
        {
            _btnComplete.gameObject.SetActive(showButton);
        }

        if (_txtComplete != null && showButton)
        {
            if (_maxRecruitCount > 0)
            {
                _txtComplete.text = $"선택 완료 ({_selectedStudents.Count}/{_maxRecruitCount})";
            }
            else
            {
                _txtComplete.text = $"선택 완료 ({_selectedStudents.Count}명)";
            }
        }
    }

    // 팝업 종료 및 정리
    private void CloseAndDestroy()
    {
        // 외부 구독 해제 (재사용/중복 호출 방지)
        OnRecruitmentConfirmed = null;
        OnCancelled = null;

        // 내부 상태 초기화
        _selectedStudents.Clear();
        _cardMap.Clear();

        ClearCards();

        if (UIManager.Instance != null)
        {
            UIManager.Instance.Close(this);
        }
        else
        {
            Close();
            Destroy(gameObject);
        }
    }

    // 생성된 카드 제거
    private void ClearCards()
    {
        foreach (GameObject card in _spawnedCards)
        {
            if (card != null) Destroy(card);
        }
        _spawnedCards.Clear();
    }

    // 안전한 스크롤 초기화 (레이아웃 갱신 타이밍 문제 대응)
    private IEnumerator ForceScrollTopRoutineSafe()
    {
        yield return null;

        if (!isActiveAndEnabled)
            yield break;

        Canvas.ForceUpdateCanvases();
        ForceScrollTop();

        yield return null;

        if (!isActiveAndEnabled)
            yield break;

        Canvas.ForceUpdateCanvases();
        ForceScrollTop();
    }

    private void ForceScrollTop()
    {
        if (_scrollRect == null) return;
        _scrollRect.StopMovement();
        _scrollRect.verticalNormalizedPosition = 1f;
        _scrollRect.velocity = Vector2.zero;
    }

    public void SetRosterCapacity(int capacity, int ownedCount)
    {
        _capacity = Mathf.Max(0, capacity);
        _ownedCount = Mathf.Max(0, ownedCount);
    }

    private int GetRemainingCapacity()
    {
        return Mathf.Max(0, _capacity - _ownedCount);
    }

    private bool CanSelectMore()
    {
        // 보유 + 선택 < 정원 이어야 추가 선택 가능
        return (_ownedCount + _selectedStudents.Count) < _capacity;
    }


    // 강제 영입 모드 설정
    public void SetForceRecruitMode()
    {
        DisableBackKey = true; 
    }
}
