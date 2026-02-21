using System;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;

/// <summary>
/// 학생 카드 컴포넌트
/// 팝업 종류에 따라 3가지 뷰 상태를 전환한다.
///
/// Normal    : 초상화만 표시 (Image 1 - 학생 선택 팝업 기본)
/// ShowStats : 스탯 오버레이 표시 (Image 2 - 카드 선택 시)
/// Placing   : 배치 완료 체크 오버레이
/// </summary>
[RequireComponent(typeof(Image))]
public class StudentCard : MonoBehaviour, IPointerClickHandler
{
    public enum CardViewState
    {
        Normal,
        ShowStats,
        Placing
    }

    [Header("공통 - 초상화")]
    [SerializeField] private Image _portraitImage;

    [Header("ShowStats 오버레이")]
    [SerializeField] private GameObject _statsOverlayPanel;
    [SerializeField] private TMP_Text _nameText;
    [SerializeField] private TMP_Text _gradeText;
    [SerializeField] private TMP_Text _positionText;
    // ConditionGauge (부모) : 배경 이미지
    // Fill (자식)           : Filled 타입, fillAmount로 게이지 표시, 색상 제어
    [SerializeField] private Image _conditionGaugeBg;   // ConditionGauge 오브젝트
    [SerializeField] private Image _conditionGaugeFill; // Fill 오브젝트 — Image Type: Filled, Fill Method: Horizontal
    [SerializeField] private TMP_Text _mentalText;      // stat_id 01
    [SerializeField] private TMP_Text _shootText;       // stat_id 02
    [SerializeField] private TMP_Text _speedText;       // stat_id 03
    [SerializeField] private TMP_Text _jumpText;        // stat_id 04
    [SerializeField] private TMP_Text _staminaText;     // stat_id 05
    [SerializeField] private GameObject _potentialBadgeRoot;
    [SerializeField] private TMP_Text _potentialBadgeText;

    [Header("Placing 오버레이")]
    [SerializeField] private GameObject _placingOverlayPanel;

    private Student _studentData;
    private CardViewState _currentState = CardViewState.Normal;

    public Student StudentData => _studentData;
    public CardViewState CurrentState => _currentState;

    public event Action<StudentCard> OnCardClicked;

    void Awake()
    {
        SetViewState(CardViewState.Normal);
    }

    // 공개 API

    public void SetStudentData(Student student)
    {
        _studentData = student;
        RefreshShowStatsUI();
    }

    public void SetViewState(CardViewState state)
    {
        _currentState = state;

        SafeSetActive(_statsOverlayPanel, state == CardViewState.ShowStats);
        SafeSetActive(_placingOverlayPanel, state == CardViewState.Placing);

        if (_portraitImage != null)
            _portraitImage.gameObject.SetActive(true);

        if (state == CardViewState.ShowStats && _studentData != null)
            RefreshShowStatsUI();
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        OnCardClicked?.Invoke(this);
    }

    // UI 갱신

    private void RefreshShowStatsUI()
    {
        if (_studentData == null) return;

        SafeSetText(_nameText, _studentData.studentName);
        SafeSetText(_gradeText, $"{_studentData.grade}학년");
        SafeSetText(_positionText, _studentData.positionName);

        RefreshConditionBar();
        RefreshStatTexts();
        RefreshPotentialBadge();
    }

    // 컨디션 게이지 갱신
    // _conditionGaugeFill(자식 Fill)의 fillAmount와 색상만 제어
    // _conditionGaugeBg(부모)는 고정 배경이므로 별도 조작 불필요
    private void RefreshConditionBar()
    {
        if (_conditionGaugeFill == null) return;

        // 최댓값 = mental + 20 (StudentFactory 기준)
        int condMax = Mathf.Max(_studentData.mental + 20, _studentData.condition, 1);
        _conditionGaugeFill.fillAmount = (float)_studentData.condition / condMax;
    }

    // 스탯 텍스트 갱신 (StudentStatTable.csv stat_id 01~05 순서)
    private void RefreshStatTexts()
    {
        SafeSetText(_mentalText, $"멘탈 {_studentData.mental}");
        SafeSetText(_shootText, $"슈팅 {_studentData.shoot}");
        SafeSetText(_speedText, $"속도 {_studentData.speed}");
        SafeSetText(_jumpText, $"점프력 {_studentData.jump}");
        SafeSetText(_staminaText, $"지구력 {_studentData.stamina}");
    }

    // 잠재력 뱃지 표시 여부 갱신 (tier 1 = 최고 등급만 강조)
    private void RefreshPotentialBadge()
    {
        bool showBadge = _studentData.potential_tier == 1
                         && !string.IsNullOrEmpty(_studentData.potential);

        SafeSetActive(_potentialBadgeRoot, showBadge);

        if (showBadge && _potentialBadgeText != null)
            _potentialBadgeText.text = _studentData.potential;
    }

    
    private static void SafeSetText(TMP_Text target, string value)
    {
        if (target != null) target.text = value;
    }

    private static void SafeSetActive(GameObject target, bool active)
    {
        if (target != null) target.SetActive(active);
    }
}