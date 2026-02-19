using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;

// UI 상 학생 카드 표시 및 드래그앤드롭
// 학생 이미지 컴포넌트 자동 추가
[RequireComponent(typeof(Image))]
// 드래그 처리용 컴포넌트 자동 추가
[RequireComponent(typeof(CanvasGroup))]
public class StudentCard : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler, IPointerClickHandler
{
    [Header("Basic Info (Always Visible)")]
    [Tooltip("학생 초상화 이미지가 들어갈 컴포넌트")]
    [SerializeField] private Image _portraitImage;

    [Header("Stats Overlay (Hidden by default)")]
    [Tooltip("클릭 시 나타날 검은색 반투명 패널")]
    [SerializeField] private GameObject _overlayPanel;

    [Header("UI References")]
    [SerializeField] private TMP_Text _nameText;
    [SerializeField] private TMP_Text _gradeText;
    [SerializeField] private TMP_Text _positionText;
    [SerializeField] private TMP_Text _mentalText;
    [SerializeField] private TMP_Text _shootText;
    [SerializeField] private TMP_Text _speedText;
    [SerializeField] private TMP_Text _jumpText;
    [SerializeField] private TMP_Text _staminaText;


    // 드래그 관련 변수
    private CanvasGroup _canvasGroup;
    private RectTransform _rectTransform;
    private Transform _originalParent;
    private Canvas _rootCanvas;
    private ScrollRect _parentScrollRect; // 스크롤 뷰 충돌 방지용 참조


    private bool _isSelected = false; // 현재 선택 상태 추적
    private bool _isDraggingCard = false; // 현재 카드를 드래그 중인지, 스크롤 중인지 판별


    // 참조하는 학생 데이터
    private Student _studentData;
    public Student StudentData => _studentData;
    public bool IsSelected => _isSelected;


    void Awake()
    {
        _rectTransform = GetComponent<RectTransform>();
        _canvasGroup = GetComponent<CanvasGroup>();
        _rootCanvas = GetComponentInParent<Canvas>().rootCanvas;
        _parentScrollRect = GetComponentInParent<ScrollRect>();

        if (_overlayPanel.activeSelf)
        {
            _overlayPanel.SetActive(false);
        }
    }


    // ==========================================
    // [임시 테스트용]
    // ==========================================
    void Start()
    {

        if (_studentData == null)
        {
            Student dummyStudent = new Student()
            {
                studentName = "강백호",
                grade = 1,
                positionName = "PF",
                stamina = 95,
                mental = 80,
                shoot = 50,
                speed = 90,
                jump = 99,
                condition = 100
            };

            SetStudentData(dummyStudent);
        }
    }
    // ==========================================



    // 학생 데이터 설정 및 UI 갱신
    public void SetStudentData(Student student)
    {
        _studentData = student;
        // TODO: 추후 Student 데이터에 초상화 스프라이트(Image) 필드가 생기면 여기서 연결
        // _portraitImage.sprite = student.portraitSprite; 

        // 데이터가 바뀌면 스탯 정보도 갱신
        UpdateUI();
    }

    // UI 갱신
    private void UpdateUI()
    {
        if (_studentData == null) return;

        _nameText.text = _studentData.studentName;
        _gradeText.text = $"{_studentData.grade}학년";
        _positionText.text = _studentData.positionName;
        _mentalText.text = $"멘탈: {_studentData.mental}";
        _shootText.text = $"슛: {_studentData.shoot}";
        _speedText.text = $"속도: {_studentData.speed}";
        _jumpText.text = $"점프: {_studentData.jump}";
        _staminaText.text = $"스테미나: {_studentData.stamina}";
    }

    // 외부에서 Student 데이터 변경 후 호출
    public void RefreshUI()
    {
        UpdateUI();
    }

    // 인스펙터에서 연결해도 되고 이벤트 시스템 이용해도 되고 일단 예시 코드
    public void OnClickStudentCard()
    {
        // TODO: 학생 상세 정보 UI 열기 등
        ShowStudentDetail();
    }

    private void ShowStudentDetail()
    {
        // 학생 상세 정보 표시 일단 디버그 로그만 (호출 방식 참고)
        Debug.Log($"=== {_studentData.studentName} 상세 정보 ===");
        Debug.Log($"학년: {_studentData.grade}, 포지션: {_studentData.positionName}");
        Debug.Log($"신체: 키 {_studentData.height}cm, 몸무게 {_studentData.weight}kg");
        Debug.Log($"스탯: 멘탈 {_studentData.mental}, 슛 {_studentData.shoot}, " +
                  $"속도 {_studentData.speed}, 점프 {_studentData.jump}, 스태미너 {_studentData.stamina}");
        Debug.Log($"잠재력: Tier {_studentData.potential_tier} - {_studentData.potential}");
        Debug.Log($"컨디션: {_studentData.condition}, 신뢰도: {_studentData.trust}");
    }


    #region Drag & Drop 로직

    // 클릭 시 선택/해제 토글 
    public void OnPointerClick(PointerEventData eventData)
    {
        // 드래그 중이 아닐 때만 클릭 인정
        if (!_isDraggingCard)
        {
            ToggleSelection();
        }
    }

    // 선택 상태 토글 및 UI 반영
    private void ToggleSelection()
    {
        _isSelected = !_isSelected;
        _overlayPanel.SetActive(_isSelected);

        if (_isSelected)
        {
            RefreshUI();
        }
    }

   
    public void OnBeginDrag(PointerEventData eventData)
    {
        // 마우스 이동 방향을 분석하여 가로 이동이 세로 이동보다 크면 '카드 드래그'로 판정
        if (Mathf.Abs(eventData.delta.x) > Mathf.Abs(eventData.delta.y))
        {
            _isDraggingCard = true;

            if (!_isSelected)
            {
                ToggleSelection();
            }

            _originalParent = transform.parent;

            transform.SetParent(_rootCanvas.transform);
            transform.SetAsLastSibling();

            _canvasGroup.blocksRaycasts = false;
            _canvasGroup.alpha = 0.8f;
        }
        else
        {
            // 세로 이동이 크면 '리스트 스크롤'로 판정하여 부모 ScrollRect로 이벤트를 넘김
            _isDraggingCard = false;
            if (_parentScrollRect != null)
            {
                _parentScrollRect.OnBeginDrag(eventData);
            }
        }
    }

    // 3. 드래그 중
    public void OnDrag(PointerEventData eventData)
    {
        if (_isDraggingCard)
        {
            _rectTransform.anchoredPosition += eventData.delta / _rootCanvas.scaleFactor;
        }
        else if (_parentScrollRect != null)
        {
            _parentScrollRect.OnDrag(eventData);
        }
    }

    // 4. 드래그 종료
    public void OnEndDrag(PointerEventData eventData)
    {
        if (_isDraggingCard)
        {
            _canvasGroup.blocksRaycasts = true;
            _canvasGroup.alpha = 1.0f;

            if (_isSelected)
            {
                ToggleSelection();
            }

            // 올바른 슬롯에 드롭되지 않아 부모가 캔버스 그대로인 경우 복귀 처리
            if (transform.parent == _rootCanvas.transform)
            {
                transform.SetParent(_originalParent);
                _rectTransform.anchoredPosition = Vector2.zero;
            }

            _isDraggingCard = false;
        }
        else if (_parentScrollRect != null)
        {
            _parentScrollRect.OnEndDrag(eventData);
        }
    }
    #endregion
}


