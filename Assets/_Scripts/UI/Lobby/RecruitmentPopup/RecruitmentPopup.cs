using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// 학생 영입 팝업
public class RecruitmentPopup : UIPopup
{
    [Header("Scroll")]
    [SerializeField] private ScrollRect _scrollRect;          // 스크롤 영역

    [Header("Header")]
    [SerializeField] private TMP_Text _txtName;               // 타이틀 (미사용 가능)
    [SerializeField] private TMP_Text _txtSelectCount;        // 선택 인원 표시

    [Header("Close")]
    [SerializeField] private Button _btnClose;                // 닫기 버튼

    [Header("Card")]
    [SerializeField] private Transform _cardRoot;             // 카드 부모
    [SerializeField] private GameObject _cardPrefab;          // 카드 프리팹

    [Header("Complete Button")]
    [SerializeField] private Button _btnComplete;             // 완료 버튼
    [SerializeField] private TMP_Text _txtComplete;

    [Header("Overlays")]
    [SerializeField] private SelectStudentInfoPopup _studentInfoPopup; // 학생 정보 오버레이

    private readonly List<GameObject> _spawnedCards = new();          // 생성된 카드 목록
    private readonly List<Student> _selectedStudents = new();         // 선택된 학생 목록
    private readonly Dictionary<Student, StudentCard> _cardMap = new(); // 학생-카드 매핑

    private int _maxRecruitCount = 0; // 최대 모집 가능 인원

    public event Action<List<Student>> OnRecruitmentConfirmed; // 영입 확정 콜백
    public event Action OnCancelled;                            // 취소 콜백

    public void SetMaxRecruitCount(int max)
    {
        _maxRecruitCount = Mathf.Max(0, max);
    }

    public override void Init()
    {
        base.Init();

        // 버튼 이벤트 바인딩
        if (_btnClose != null)
        {
            _btnClose.onClick.RemoveAllListeners();
            _btnClose.onClick.AddListener(HandleCloseButton);
        }

        if (_btnComplete != null)
        {
            _btnComplete.onClick.RemoveAllListeners();
            _btnComplete.onClick.AddListener(HandleCompleteButton);
        }

        _selectedStudents.Clear();
        _cardMap.Clear();

        SpawnCandidateCards();
        RefreshHeader();
        RefreshCompleteButton();
    }

    public override void Open()
    {
        base.Open();
        StartCoroutine(ForceScrollTopRoutineSafe()); // 스크롤 초기화
    }

    // 후보 학생 카드 생성
    private void SpawnCandidateCards()
    {
        ClearCards();

        if (StudentManager.Instance == null)
        {
            Debug.LogWarning("[RecruitmentPopup] StudentManager가 없습니다.");
            return;
        }

        IReadOnlyList<Student> students = StudentManager.Instance.Students;
        if (students == null || students.Count == 0)
        {
            Debug.LogWarning("[RecruitmentPopup] 영입 후보 학생이 없습니다.");
            return;
        }

        foreach (Student student in students)
        {
            CreateCandidateCard(student);
        }
    }

    // 카드 1개 생성
    private void CreateCandidateCard(Student student)
    {
        if (_cardPrefab == null) return;

        GameObject cardObj = Instantiate(_cardPrefab, _cardRoot);
        StudentCard studentCard = cardObj.GetComponent<StudentCard>();

        if (studentCard != null)
        {
            studentCard.SetStudentData(student);
            studentCard.SetViewState(StudentCard.CardViewState.Normal);

            Student captured = student;
            studentCard.OnCardClicked += card => HandleCardClicked(captured, card);

            _cardMap[captured] = studentCard;
        }

        cardObj.SetActive(true);
        _spawnedCards.Add(cardObj);
    }

    // 카드 클릭 처리
    private void HandleCardClicked(Student student, StudentCard card)
    {
        if (student == null || card == null) return;

        // 이미 선택된 경우 → 취소 확인 팝업
        if (_selectedStudents.Contains(student))
        {
            ShowUnselectConfirmPopup(student, card);
            return;
        }

        // 최대 인원 초과 방지
        if (IsMaxReached())
        {
            ShowMaxReachedPopup();
            return;
        }

        SelectStudent(student, card);
        ShowSelectStudentPopup(student); // 정보 오버레이 표시
    }

    // 학생 정보 팝업 표시
    private void ShowSelectStudentPopup(Student student)
    {
        if (_studentInfoPopup == null)
        {
            Debug.LogWarning("[RecruitmentPopup] _studentInfoPopup이 연결되지 않았습니다.");
            return;
        }

        _studentInfoPopup.Init();
        _studentInfoPopup.Setup("선택한 학생", student, null);
        _studentInfoPopup.transform.SetAsLastSibling();
        _studentInfoPopup.Open();
    }

    // 학생 선택 처리
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
                new PopupButtonInfo("취소", null),
                new PopupButtonInfo("확인", () => UnselectStudent(student, card))
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
                new PopupButtonInfo("확인", null)
            }
        ));
    }

    // 영입 완료 버튼 클릭
    private void HandleCompleteButton()
    {
        if (_selectedStudents.Count == 0) return;
        if (UIManager.Instance == null) return;

        List<Student> snapshot = new(_selectedStudents);

        UIManager.Instance.ShowPopup(new PopupData(
            title: "학생 영입",
            content: $"선택한 학생 {snapshot.Count}명을 영입하시겠습니까?",
            buttons: new List<PopupButtonInfo>
            {
                new PopupButtonInfo("포기", null),
                new PopupButtonInfo("확인", () => ShowJoinCompletePopup(snapshot))
            }
        ));
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
                new PopupButtonInfo("확인", () =>
                {
                    OnRecruitmentConfirmed?.Invoke(recruits);
                    CloseAndDestroy();
                })
            }
        ));
    }

    // 닫기 버튼 처리
    private void HandleCloseButton()
    {
        if (UIManager.Instance == null) return;

        UIManager.Instance.ShowPopup(new PopupData(
            title: "영입 취소",
            content: "영입을 종료하고 로비로 돌아가시겠습니까?",
            buttons: new List<PopupButtonInfo>
            {
                new PopupButtonInfo("취소", null),
                new PopupButtonInfo("확인", () =>
                {
                    OnCancelled?.Invoke();
                    CloseAndDestroy();
                })
            }
        ));
    }

    protected override void OnCloseButtonClicked()
    {
        HandleCloseButton();
    }

    // 상단 선택 인원 표시 갱신
    private void RefreshHeader()
    {
        if (_txtSelectCount != null)
        {
            string maxDisplay = _maxRecruitCount > 0 ? _maxRecruitCount.ToString() : "7";
            _txtSelectCount.text = $"{_selectedStudents.Count}/{maxDisplay}";
        }
    }

    // 완료 버튼 표시 상태 갱신
    private void RefreshCompleteButton()
    {
        bool hasSelection = _selectedStudents.Count > 0;

        if (_btnComplete != null)
            _btnComplete.gameObject.SetActive(hasSelection);

        if (_txtComplete != null && hasSelection)
        {
            _txtComplete.text = _maxRecruitCount > 0
                ? $"선택 완료 ({_selectedStudents.Count}/{_maxRecruitCount})"
                : $"선택 완료 ({_selectedStudents.Count}명)";
        }
    }

    // 팝업 종료 및 정리
    private void CloseAndDestroy()
    {
        OnRecruitmentConfirmed = null;
        OnCancelled = null;

        _selectedStudents.Clear();
        _cardMap.Clear();

        ClearCards();
        Close();
        Destroy(gameObject);
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

    // 안전한 스크롤 초기화
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
}