using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

// 학생 선택 팝업
public class StudentSelectPopup : UIPopup
{
    [Header("Scroll")]
    [SerializeField] private ScrollRect _scrollRect;

    [Header("Close")]
    [SerializeField] private Button _buttonClose;

    [Header("Card")]
    [SerializeField] private Transform _cardRoot;
    [SerializeField] private GameObject _cardPrefab;

    [Header("Complete Button")]
    [SerializeField] private Button _btnComplete;
    [SerializeField] private TMP_Text _txtComplete;

    // 열릴 때 ShowStats(Image 2) 로 시작할지 여부 (false = Normal(Image 1) 기본)
    [Header("View Settings")]
    [SerializeField] private bool _showStatsOnOpen = false;

    // 최대 선택 가능 인원 (0이면 무제한)
    private int _maxSelectCount = 0;

    private readonly List<GameObject> _spawnedCards = new List<GameObject>();
    private readonly List<Student> _selectedStudents = new List<Student>();
    private readonly Dictionary<Student, StudentCard> _cardMap = new Dictionary<Student, StudentCard>();

    public event Action<List<Student>> OnSelectionConfirmed;
    public event Action OnCancelled;

    public void SetMaxSelectCount(int max)
    {
        _maxSelectCount = Mathf.Max(0, max);
    }

    // 기존 호환용
    public void SetMaxSelectableCount(int count)
    {
        SetMaxSelectCount(count);
    }

    public override void Init()
    {
        base.Init();

        // 버튼 이벤트 중복 등록 방지
        BindButtons();

        // 팝업 재사용 가능성을 고려해 상태 초기화
        _selectedStudents.Clear();
        _cardMap.Clear();

        SpawnCards();
        RefreshCompleteButton();

        // 생성 직후 레이아웃 반영 전이라 코루틴으로 2프레임 보정
        StartCoroutine(ForceScrollTopRoutine());
    }

    private void BindButtons()
    {
        if (_buttonClose != null)
        {
            _buttonClose.onClick.RemoveAllListeners();
            _buttonClose.onClick.AddListener(() =>
            {
                // 닫기 = 취소 처리
                OnCancelled?.Invoke();
                CloseAndDestroy();
            });
        }

        if (_btnComplete != null)
        {
            _btnComplete.onClick.RemoveAllListeners();
            _btnComplete.onClick.AddListener(HandleComplete);
        }
    }

    // StudentManager에서 학생 목록 가져와 카드 생성
    private void SpawnCards()
    {
        ClearCards();

        if (_cardPrefab == null)
        {
            Debug.LogError("[StudentSelectPopup] Card Prefab이 설정되지 않았습니다.");
            return;
        }

        if (StudentManager.Instance == null)
        {
            Debug.LogWarning("[StudentSelectPopup] StudentManager가 없습니다.");
            return;
        }

        IReadOnlyList<Student> students = StudentManager.Instance.Students;

        if (students == null || students.Count == 0)
        {
            Debug.LogWarning("[StudentSelectPopup] 학생이 없습니다.");
            return;
        }

        foreach (Student student in students)
        {
            CreateCard(student);
        }
    }

    // 카드 생성
    private void CreateCard(Student student)
    {
        if (_cardRoot == null)
        {
            Debug.LogError("[StudentSelectPopup] Card Root가 설정되지 않았습니다.");
            return;
        }

        GameObject cardObj = Instantiate(_cardPrefab, _cardRoot);
        StudentCard studentCard = cardObj.GetComponent<StudentCard>();

        if (studentCard == null)
        {
            Debug.LogError("[StudentSelectPopup] StudentCard 컴포넌트가 없습니다.");
            Destroy(cardObj);
            return;
        }

        studentCard.SetStudentData(student);

        // 오픈 시 기본 뷰 상태 결정
        studentCard.SetViewState(_showStatsOnOpen
            ? StudentCard.CardViewState.ShowStats
            : StudentCard.CardViewState.Normal);

        // 람다 캡처 안전 처리
        Student captured = student;
        studentCard.OnCardClicked += card =>
        {
            HandleCardClicked(captured, card);
        };

        _cardMap[student] = studentCard;
        _spawnedCards.Add(cardObj);
    }

    // StudentCard 프리팹 방식 클릭 처리 — Normal ↔ ShowStats 토글
    private void HandleCardClicked(Student student, StudentCard card)
    {
        if (_selectedStudents.Contains(student))
        {
            _selectedStudents.Remove(student);
            card.SetViewState(StudentCard.CardViewState.Normal);
        }
        else
        {
            // 최대 선택 제한 체크
            if (_maxSelectCount > 0 && _selectedStudents.Count >= _maxSelectCount)
            {
                Debug.Log($"[StudentSelectPopup] 최대 {_maxSelectCount}명까지 선택 가능");
                return;
            }

            _selectedStudents.Add(student);
            card.SetViewState(StudentCard.CardViewState.ShowStats);
        }

        RefreshCompleteButton();
    }

    // 선택 완료 버튼 표시 상태 갱신 (1명 이상 선택 시 활성화)
    private void RefreshCompleteButton()
    {
        bool hasSelection = _selectedStudents.Count > 0;

        if (_btnComplete != null)
            _btnComplete.gameObject.SetActive(hasSelection);

        if (_txtComplete != null && hasSelection)
        {
            _txtComplete.text = _maxSelectCount > 0
                ? $"선택 완료 ({_selectedStudents.Count}/{_maxSelectCount})"
                : $"선택 완료 ({_selectedStudents.Count}명)";
        }
    }

    // 선택 완료
    private void HandleComplete()
    {
        if (_selectedStudents.Count == 0) return;

        // 외부에서 리스트를 수정해도 내부 상태가 오염되지 않도록 복사본 전달
        OnSelectionConfirmed?.Invoke(new List<Student>(_selectedStudents));
        CloseAndDestroy();
    }

    // X 버튼 (UIPopup 공통)
    protected override void OnCloseButtonClicked()
    {
        OnCancelled?.Invoke();
        CloseAndDestroy();
    }

    private void CloseAndDestroy()
    {
        // 이벤트 핸들러 참조 해제(메모리/중복 호출 방지)
        OnSelectionConfirmed = null;
        OnCancelled = null;

        _selectedStudents.Clear();
        _cardMap.Clear();
        ClearCards();

        Close();
        Destroy(gameObject);
    }

    private void ClearCards()
    {
        foreach (GameObject card in _spawnedCards)
        {
            if (card != null) Destroy(card);
        }
        _spawnedCards.Clear();
    }

    // 스크롤 초기화
    private IEnumerator ForceScrollTopRoutine()
    {
        yield return null;
        Canvas.ForceUpdateCanvases();
        ForceScrollTop();

        yield return null;
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