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
    [SerializeField] private Button _btnClose;

    [Header("Card")]
    [SerializeField] private Transform _cardRoot;
    [SerializeField] private GameObject _cardPrefab;

    [Header("Complete Button")]
    [SerializeField] private Button _btnComplete;
    [SerializeField] private TMP_Text _txtComplete;

    // 열릴 때 ShowStats(Image 2) 로 시작할지 여부 (false = Normal(Image 1) 기본)
    [Header("View Settings")]
    [SerializeField] private bool _showStatsOnOpen = false;

    private readonly List<GameObject> _spawnedCards = new List<GameObject>();
    private readonly List<Student> _selectedStudents = new List<Student>();
    private readonly Dictionary<Student, StudentCard> _cardMap = new Dictionary<Student, StudentCard>();

    private int _maxSelectCount = 0;

    public event Action<List<Student>> OnSelectionConfirmed;
    public event Action OnCancelled;

    public Transform CardRoot => _cardRoot;
    public GameObject CardPrefab => _cardPrefab;

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

        if (_btnClose != null)
        {
            _btnClose.onClick.RemoveAllListeners();
            _btnClose.onClick.AddListener(() =>
            {
                OnCancelled?.Invoke();
                CloseAndDestroy();
            });
        }

        if (_btnComplete != null)
        {
            _btnComplete.onClick.RemoveAllListeners();
            _btnComplete.onClick.AddListener(HandleComplete);
        }

        _selectedStudents.Clear();
        _cardMap.Clear();
        SpawnCards();
        RefreshCompleteButton();

        StartCoroutine(ForceScrollTopRoutine());
    }

    // StudentManager에서 학생 목록 가져와 카드 생성
    private void SpawnCards()
    {
        ClearCards();

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
            CreateCard(student);
    }

    // 카드 생성 (프리팹 있으면 사용, 없으면 임시 버튼)
    private void CreateCard(Student student)
    {
        GameObject cardObj;
        StudentCard studentCard = null;

        if (_cardPrefab != null)
        {
            cardObj = Instantiate(_cardPrefab, _cardRoot);
            studentCard = cardObj.GetComponent<StudentCard>();

            if (studentCard != null)
            {
                studentCard.SetStudentData(student);
                studentCard.SetViewState(_showStatsOnOpen
                    ? StudentCard.CardViewState.ShowStats
                    : StudentCard.CardViewState.Normal);

                Student captured = student;
                studentCard.OnCardClicked += card => HandleCardClicked(captured, card);
                _cardMap[student] = studentCard;
            }
        }
        else
        {
            cardObj = CreateTempCard(student);

            Button btn = cardObj.GetComponent<Button>() ?? cardObj.AddComponent<Button>();
            Student captured = student;
            btn.onClick.RemoveAllListeners();
            btn.onClick.AddListener(() => HandleTempCardClicked(captured));
        }

        cardObj.SetActive(true);
        _spawnedCards.Add(cardObj);
    }

    // 임시 테스트 카드 (프리팹 없을 때)
    private GameObject CreateTempCard(Student student)
    {
        GameObject cardObj = new GameObject($"Card_{student.studentName}");
        cardObj.transform.SetParent(_cardRoot, false);

        RectTransform rt = cardObj.AddComponent<RectTransform>();
        rt.sizeDelta = new Vector2(160f, 200f);

        Image bg = cardObj.AddComponent<Image>();
        bg.color = new Color(0.85f, 0.85f, 0.85f, 1f);

        GameObject txtObj = new GameObject("TxtName");
        txtObj.transform.SetParent(cardObj.transform, false);
        RectTransform txtRt = txtObj.AddComponent<RectTransform>();
        txtRt.anchorMin = Vector2.zero;
        txtRt.anchorMax = Vector2.one;
        txtRt.offsetMin = new Vector2(5f, 5f);
        txtRt.offsetMax = new Vector2(-5f, -5f);

        TMP_Text txt = txtObj.AddComponent<TextMeshProUGUI>();
        txt.text = $"{student.studentName}\n{student.positionName}\n멘탈:{student.mental}\n슛:{student.shoot}\n속도:{student.speed}\n점프:{student.jump}";
        txt.fontSize = 14f;
        txt.color = Color.black;
        txt.alignment = TextAlignmentOptions.Center;
        txt.textWrappingMode = TextWrappingModes.Normal;

        return cardObj;
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

    // 임시 카드 클릭 처리 (배경색으로 선택 표시)
    private void HandleTempCardClicked(Student student)
    {
        if (_selectedStudents.Contains(student))
        {
            _selectedStudents.Remove(student);
            SetTempCardColor(student, false);
        }
        else
        {
            if (_maxSelectCount > 0 && _selectedStudents.Count >= _maxSelectCount)
                return;

            _selectedStudents.Add(student);
            SetTempCardColor(student, true);
        }

        RefreshCompleteButton();
    }

    // 임시 카드 선택 시각 표현
    private void SetTempCardColor(Student student, bool selected)
    {
        foreach (Transform child in _cardRoot)
        {
            if (child.name != $"Card_{student.studentName}") continue;

            Image bg = child.GetComponent<Image>();
            if (bg != null)
            {
                bg.color = selected
                    ? new Color(0.4f, 0.6f, 1f, 1f)
                    : new Color(0.85f, 0.85f, 0.85f, 1f);
            }
            break;
        }
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

        Debug.Log($"[StudentSelectPopup] 선택 완료: {_selectedStudents.Count}명");
        foreach (Student s in _selectedStudents)
            Debug.Log($"  - {s.studentName} (ID:{s.id})");

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

    // 스크롤 초기화 (기존 팀원 코드 유지)
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