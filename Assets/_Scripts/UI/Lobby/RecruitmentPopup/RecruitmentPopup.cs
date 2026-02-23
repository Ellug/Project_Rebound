using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class RecruitmentPopup : UIPopup
{
    [Header("Scroll")]
    [SerializeField] private ScrollRect _scrollRect;

    [Header("Header")]
    [SerializeField] private TMP_Text _txtName;
    [SerializeField] private TMP_Text _txtSelectCount;

    [Header("Close")]
    [SerializeField] private Button _btnClose;

    [Header("Card")]
    [SerializeField] private Transform _cardRoot;
    [SerializeField] private GameObject _cardPrefab;

    [Header("Complete Button")]
    [SerializeField] private Button _btnComplete;
    [SerializeField] private TMP_Text _txtComplete;

    private readonly List<GameObject> _spawnedCards = new();
    private readonly List<Student> _selectedStudents = new();
    private readonly Dictionary<Student, StudentCard> _cardMap = new();

    private int _maxRecruitCount = 0;

    public event Action<List<Student>> OnRecruitmentConfirmed;
    public event Action OnCancelled;

    public void SetMaxRecruitCount(int max)
    {
        _maxRecruitCount = Mathf.Max(0, max);
    }

    public override void Init()
    {
        base.Init();

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

        StartCoroutine(ForceScrollTopRoutine());
    }

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
            CreateCandidateCard(student);
    }

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
            _cardMap[student] = studentCard;
        }

        cardObj.SetActive(true);
        _spawnedCards.Add(cardObj);
    }

    private void HandleCardClicked(Student student, StudentCard card)
    {
        if (_selectedStudents.Contains(student))
        {
            _selectedStudents.Remove(student);
            card.SetViewState(StudentCard.CardViewState.Normal);
        }
        else
        {
            if (IsMaxReached())
            {
                ShowMaxReachedPopup();
                return;
            }

            _selectedStudents.Add(student);
            card.SetViewState(StudentCard.CardViewState.Placing);
        }

        RefreshHeader();
        RefreshCompleteButton();
    }

    private bool IsMaxReached()
    {
        return _maxRecruitCount > 0 && _selectedStudents.Count >= _maxRecruitCount;
    }

    // 영입 팝업이 "로비보다 위"에 유지되도록, 오버레이 팝업 띄우기 직전에 최상단으로 올린다.
    private void BringToFrontForOverlay()
    {
        transform.SetAsLastSibling();
    }

    private void ShowMaxReachedPopup()
    {
        if (UIManager.Instance == null) return;

        BringToFrontForOverlay();

        UIManager.Instance.ShowPopup(new PopupData(
            title: "최대 인원 도달",
            content: "더 이상 모집이 불가능합니다.",
            buttons: new List<PopupButtonInfo>
            {
                new PopupButtonInfo("확인", null)
            }
        ));
    }

    private void HandleCompleteButton()
    {
        if (_selectedStudents.Count == 0) return;
        if (UIManager.Instance == null) return;

        List<Student> snapshot = new(_selectedStudents);

        BringToFrontForOverlay();

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

    private void ShowJoinCompletePopup(List<Student> recruits)
    {
        if (UIManager.Instance == null) return;

        // 여기서도 영입 팝업은 유지된 채, 그 위로 완료 팝업이 한 번 더 뜨는 구조
        BringToFrontForOverlay();

        UIManager.Instance.ShowPopup(new PopupData(
            title: "학생 영입",
            content: "새로운 학생이 팀에 합류했습니다.\n여기 팀 운영에 큰 변화를 불러올 것입니다.",
            buttons: new List<PopupButtonInfo>
            {
                new PopupButtonInfo("확인", () =>
                {
                    // 이 순간에만 영입 팝업을 닫는다.
                    OnRecruitmentConfirmed?.Invoke(recruits);
                    CloseAndDestroy();
                })
            }
        ));
    }

    private void HandleCloseButton()
    {
        if (UIManager.Instance == null) return;

        BringToFrontForOverlay();

        UIManager.Instance.ShowPopup(new PopupData(
            title: "영입 취소",
            content: "해당 학생 선택을 취소하시겠습니까?",
            buttons: new List<PopupButtonInfo>
            {
                new PopupButtonInfo("포기", null),
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

    private void RefreshHeader()
    {
        if (_txtSelectCount != null)
        {
            string maxDisplay = _maxRecruitCount > 0 ? _maxRecruitCount.ToString() : "7";
            _txtSelectCount.text = $"{_selectedStudents.Count}/{maxDisplay}";
        }
    }

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

    private void ClearCards()
    {
        foreach (GameObject card in _spawnedCards)
        {
            if (card != null) Destroy(card);
        }
        _spawnedCards.Clear();
    }

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