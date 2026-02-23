using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// 학생 카드 프리팹 컴포넌트
// CardViewState:
//   Normal    - 기본 초상화만 표시
//   ShowStats - 스탯 오버레이 표시 (훈련 선택 등)
//   Placing   - 영입 선택 중 체크 오버레이 표시
public class StudentCard : MonoBehaviour
{
    public enum CardViewState
    {
        Normal,
        ShowStats,
        Placing // 영입 선택 중 (체크 오버레이)
    }

    // 클릭 이벤트 (자기 자신을 파라미터로 전달)
    public event Action<StudentCard> OnCardClicked;

    [Header("공통 - 초상화")]
    [SerializeField] private Image _portraitImage;

    [Header("ShowStats 오버레이")]
    [SerializeField] private GameObject _statsOverlayPanel;
    [SerializeField] private TMP_Text _txtName;
    [SerializeField] private TMP_Text _txtGrade;
    [SerializeField] private TMP_Text _txtPosition;
    [SerializeField] private Image _conditionGaugeBg;
    [SerializeField] private Image _conditionGaugeFill;
    [SerializeField] private TMP_Text _txtMental;
    [SerializeField] private TMP_Text _txtShoot;
    [SerializeField] private TMP_Text _txtSpeed;
    [SerializeField] private TMP_Text _txtJump;
    [SerializeField] private TMP_Text _txtStamina;
    [SerializeField] private GameObject _potentialBadgeRoot;
    [SerializeField] private TMP_Text _txtPotentialBadge;

    [Header("Placing 오버레이")]
    [SerializeField] private GameObject _placingOverlayPanel; // 체크 아이콘 + "선택 중" 텍스트

    private Student _studentData;
    private CardViewState _currentState = CardViewState.Normal;

    void Awake()
    {
        Button btn = GetComponent<Button>();
        if (btn == null)
            btn = gameObject.AddComponent<Button>();

        btn.onClick.RemoveAllListeners();
        btn.onClick.AddListener(() => OnCardClicked?.Invoke(this));
    }

    // 학생 데이터 바인딩
    public void SetStudentData(Student student)
    {
        _studentData = student;
        RefreshDisplay();
    }

    // 뷰 상태 전환
    public void SetViewState(CardViewState state)
    {
        _currentState = state;
        RefreshDisplay();
    }

    public CardViewState GetViewState() => _currentState;
    public Student GetStudentData() => _studentData;

    // 현재 상태에 맞게 오버레이 패널 활성화/비활성화
    private void RefreshDisplay()
    {
        SafeSetActive(_statsOverlayPanel, _currentState == CardViewState.ShowStats);
        SafeSetActive(_placingOverlayPanel, _currentState == CardViewState.Placing);

        if (_currentState == CardViewState.ShowStats && _studentData != null)
            PopulateStatsOverlay(_studentData);
    }

    // 스탯 오버레이 데이터 채우기
    private void PopulateStatsOverlay(Student student)
    {
        SetText(_txtName, student.studentName);
        SetText(_txtGrade, $"{student.grade}학년");
        SetText(_txtPosition, student.positionName);
        SetText(_txtMental, student.mental.ToString());
        SetText(_txtShoot, student.shoot.ToString());
        SetText(_txtSpeed, student.speed.ToString());
        SetText(_txtJump, student.jump.ToString());
        SetText(_txtStamina, student.stamina.ToString());

        RefreshConditionGauge(student);
        RefreshPotentialBadge(student);
    }

    // 컨디션 게이지 fillAmount 갱신
    private void RefreshConditionGauge(Student student)
    {
        if (_conditionGaugeFill == null) return;

        int condMax = Mathf.Max(student.mental + 20, student.condition, 1);
        _conditionGaugeFill.fillAmount = (float)student.condition / condMax;
    }

    // 잠재력 배지 표시 (잠재력 있을 때만)
    private void RefreshPotentialBadge(Student student)
    {
        bool hasPotential = !string.IsNullOrEmpty(student.potential);
        SafeSetActive(_potentialBadgeRoot, hasPotential);

        if (hasPotential)
            SetText(_txtPotentialBadge, student.potential);
    }

    private static void SetText(TMP_Text target, string text)
    {
        if (target != null) target.text = text;
    }

    private static void SafeSetActive(GameObject target, bool active)
    {
        if (target != null) target.SetActive(active);
    }
}