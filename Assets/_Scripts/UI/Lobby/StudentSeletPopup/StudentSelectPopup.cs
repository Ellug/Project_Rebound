using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

// 학생 선택 팝업
// 기존 팀원 코드(ScrollRect, CardRoot, CardPrefab, BtnClose) 구조 유지
// 학생 카드 프리팹 완성 전까지 임시 버튼으로 테스트 가능
public class StudentSelectPopup : UIPopup
{
    [Header("Scroll")]
    [SerializeField] private ScrollRect _scrollRect;

    [Header("Close")]
    [SerializeField] private Button _btnClose;

    [Header("Card")]
    [SerializeField] private Transform _cardRoot;       // Content (GridLayoutGroup)
    [SerializeField] private GameObject _cardPrefab;    // 학생 카드 프리팹 (없으면 임시 버튼 생성)

    [Header("Complete Button")]
    [SerializeField] private Button _btnComplete;       // 선택 완료 버튼 (기본 비활성화)
    [SerializeField] private TMP_Text _txtComplete;     // 완료 버튼 텍스트

    // 생성된 카드 오브젝트 목록
    private readonly List<GameObject> _spawnedCards = new List<GameObject>();

    // 선택된 학생 목록
    private readonly List<Student> _selectedStudents = new List<Student>();

    // 최대 선택 인원 (0이면 무제한)
    // private int _maxSelectableCount = 0;

    // 선택/해제 시각적 표현을 위한 매핑
    private readonly Dictionary<Student, GameObject> _studentCardMap = new Dictionary<Student, GameObject>();

    // 최대 선택 가능 인원 (0이면 무제한)
    private int _maxSelectCount = 0;

    // 이벤트
    public event Action<List<Student>> OnSelectionConfirmed;
    public event Action OnCancelled;

    // 최대 선택 인원 설정 (Init 전에 호출)
    public void SetMaxSelectableCount(int count)
    {
        _maxSelectCount = Mathf.Max(0, count);
    }

    // 기존 프로퍼티 유지
    public Transform CardRoot => _cardRoot;
    public GameObject CardPrefab => _cardPrefab;

    // 최대 선택 인원 설정 (Init 전에 호출)
    public void SetMaxSelectCount(int max)
    {
        _maxSelectCount = max;
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
        _studentCardMap.Clear();
        SpawnCards();
        UpdateCompleteButton();

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
        {
            GameObject cardObj = CreateCard(student);
            _spawnedCards.Add(cardObj);
            _studentCardMap[student] = cardObj;
        }
    }

    // 카드 생성 (프리팹 있으면 사용, 없으면 임시 버튼)
    private GameObject CreateCard(Student student)
    {
        GameObject cardObj;

        if (_cardPrefab != null)
        {
            // 팀원이 만든 카드 프리팹 사용
            cardObj = Instantiate(_cardPrefab, _cardRoot);

            // StudentCard 컴포넌트가 있으면 데이터 세팅
            StudentCard studentCard = cardObj.GetComponent<StudentCard>();
            if (studentCard != null)
            {
                studentCard.SetStudentData(student);
            }
        }
        else
        {
            // 임시 테스트 카드 생성
            cardObj = CreateTempCard(student);
        }

        // 선택 기능 추가 (버튼 클릭 시 토글)
        Button btn = cardObj.GetComponent<Button>();
        if (btn == null)
            btn = cardObj.AddComponent<Button>();

        Student captured = student;
        btn.onClick.RemoveAllListeners();
        btn.onClick.AddListener(() => HandleCardClicked(captured));

        cardObj.SetActive(true);
        return cardObj;
    }

    // 임시 테스트 카드 (프리팹 없을 때)
    private GameObject CreateTempCard(Student student)
    {
        GameObject cardObj = new GameObject($"Card_{student.studentName}");
        cardObj.transform.SetParent(_cardRoot, false);

        // RectTransform
        RectTransform rt = cardObj.AddComponent<RectTransform>();
        rt.sizeDelta = new Vector2(160f, 200f);

        // 배경 이미지
        Image bg = cardObj.AddComponent<Image>();
        bg.color = new Color(0.85f, 0.85f, 0.85f, 1f);

        // 이름 텍스트
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

    // 카드 클릭 (토글 선택)
    private void HandleCardClicked(Student student)
    {
        if (_selectedStudents.Contains(student))
        {
            // 선택 해제
            _selectedStudents.Remove(student);
            SetCardSelected(student, false);
        }
        else
        {
            // 최대 인원 체크 (0이면 무제한)
            if (_maxSelectCount > 0 && _selectedStudents.Count >= _maxSelectCount)
            {
                Debug.Log($"[StudentSelectPopup] 최대 {_maxSelectCount}명까지 선택 가능");
                return;
            }

            // 선택
            _selectedStudents.Add(student);
            SetCardSelected(student, true);
        }

        UpdateCompleteButton();
    }

    // 카드 선택 시각 표현
    private void SetCardSelected(Student student, bool selected)
    {
        if (!_studentCardMap.TryGetValue(student, out GameObject cardObj)) return;

        Image bg = cardObj.GetComponent<Image>();
        if (bg != null)
        {
            bg.color = selected
                ? new Color(0.4f, 0.6f, 1f, 1f)   // 선택: 파란색
                : new Color(0.85f, 0.85f, 0.85f, 1f); // 미선택: 회색
        }
    }

    // 선택 완료 버튼 (1명 이상 선택 시 활성화)
    private void UpdateCompleteButton()
    {
        bool hasSelection = _selectedStudents.Count > 0;

        if (_btnComplete != null)
            _btnComplete.gameObject.SetActive(hasSelection);

        if (_txtComplete != null && hasSelection)
        {
            if (_maxSelectCount > 0)
                _txtComplete.text = $"선택 완료 ({_selectedStudents.Count}/{_maxSelectCount})";
            else
                _txtComplete.text = $"선택 완료 ({_selectedStudents.Count}명)";
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
        _studentCardMap.Clear();
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