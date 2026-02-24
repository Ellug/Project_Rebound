using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class StudentCard : MonoBehaviour
{
    // 카드 표시 상태
    public enum CardViewState
    {
        Normal,     // 기본 (초상화만)
        ShowStats,  // 스탯 오버레이 표시
        Placing,    // 영입 선택 중
        Managing    // 학생 관리 배치중
    }

    public event Action<StudentCard> OnCardClicked; // 카드 클릭 이벤트

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

    [Header("Placing 오버레이 (영입 선택 중)")]
    [SerializeField] private GameObject _placingOverlayPanel;

    [Header("Managing 오버레이 (학생 관리 배치중)")]
    [SerializeField] private GameObject _managingOverlayPanel;

    private Student _studentData;                      // 연결된 학생 데이터
    private CardViewState _currentState = CardViewState.Normal;

    void Awake()
    {
        // Button 컴포넌트 보장
        Button btn = GetComponent<Button>();
        if (btn == null)
            btn = gameObject.AddComponent<Button>();

        // 클릭 시 자기 자신 전달
        btn.onClick.RemoveAllListeners();
        btn.onClick.AddListener(() => OnCardClicked?.Invoke(this));
    }

    // 학생 데이터 설정
    public void SetStudentData(Student student)
    {
        _studentData = student;
        RefreshDisplay();
    }

    // 카드 상태 변경
    public void SetViewState(CardViewState state)
    {
        _currentState = state;
        RefreshDisplay();
    }

    public CardViewState GetViewState() => _currentState;
    public Student GetStudentData() => _studentData;

    // 현재 초상화 스프라이트 반환
    public Sprite GetPortraitSprite()
    {
        return _portraitImage != null ? _portraitImage.sprite : null;
    }

    // 상태에 따라 오버레이 표시 갱신
    private void RefreshDisplay()
    {
        SafeSetActive(_statsOverlayPanel, _currentState == CardViewState.ShowStats);
        SafeSetActive(_placingOverlayPanel, _currentState == CardViewState.Placing);
        SafeSetActive(_managingOverlayPanel, _currentState == CardViewState.Managing);

        // ShowStats 상태일 때만 스탯 갱신
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

    // 컨디션 게이지 갱신
    private void RefreshConditionGauge(Student student)
    {
        if (_conditionGaugeFill == null) return;

        int condMax = Mathf.Max(student.mental + 20, student.condition, 1);
        _conditionGaugeFill.fillAmount = (float)student.condition / condMax;
    }

    // 잠재력 배지 표시 여부
    private void RefreshPotentialBadge(Student student)
    {
        bool hasPotential = !string.IsNullOrEmpty(student.potential);
        SafeSetActive(_potentialBadgeRoot, hasPotential);

        if (hasPotential)
            SetText(_txtPotentialBadge, student.potential);
    }

    // 안전 텍스트 세팅
    private static void SetText(TMP_Text target, string text)
    {
        if (target != null) target.text = text;
    }

    // 안전 Active 처리
    private static void SafeSetActive(GameObject target, bool active)
    {
        if (target != null) target.SetActive(active);
    }
}