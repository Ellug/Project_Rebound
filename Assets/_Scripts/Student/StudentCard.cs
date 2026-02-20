using System;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;

[RequireComponent(typeof(Image))]
public class StudentCard : MonoBehaviour, IPointerClickHandler
{
    // 카드의 시각적 상태 정의
    public enum CardViewState
    {
        Normal,     // 기본 (초상화만)
        ShowStats,  // 스탯 표시 (훈련 창 등)
        Placing     // 배치 중 (검은 화면에 체크 표시)
    }

    [Header("Basic Info")]
    [SerializeField] private Image _portraitImage;

    [Header("Overlays")]
    [SerializeField] private GameObject _statsOverlayPanel;
    [SerializeField] private GameObject _placingOverlayPanel; // 배치 중(V표시) 패널

    [Header("UI References")]
    [SerializeField] private TMP_Text _nameText;
    [SerializeField] private TMP_Text _gradeText;
    [SerializeField] private TMP_Text _positionText;
    [SerializeField] private TMP_Text _staminaText;
    [SerializeField] private TMP_Text _mentalText;
    [SerializeField] private TMP_Text _shootText;
    [SerializeField] private TMP_Text _jumpText;
    [SerializeField] private TMP_Text _speedText;
    [SerializeField] private TMP_Text _conditionText;

    private Student _studentData;
    public Student StudentData => _studentData;

    // 외부(매니저/팝업)에서 카드 클릭을 감지할 수 있도록 이벤트 제공
    public event Action<StudentCard> OnCardClicked;

    void Awake()
    {
        SetViewState(CardViewState.Normal);
    }

    public void SetStudentData(Student student)
    {
        _studentData = student;
        UpdateUI();
    }

    private void UpdateUI()
    {
        if (_studentData == null) return;

        if (_nameText != null) _nameText.text = _studentData.studentName;
        if (_gradeText != null) _gradeText.text = $"{_studentData.grade}학년";
        if (_positionText != null) _positionText.text = _studentData.positionName;

        if (_staminaText != null) _staminaText.text = $"지구력 {_studentData.stamina}";
        if (_mentalText != null) _mentalText.text = $"멘탈 {_studentData.mental}";
        if (_shootText != null) _shootText.text = $"슈팅 {_studentData.shoot}";
        if (_jumpText != null) _jumpText.text = $"점프력 {_studentData.jump}";
        if (_speedText != null) _speedText.text = $"속도 {_studentData.speed}";
        if (_conditionText != null) _conditionText.text = $"컨디션 {_studentData.condition}";
    }

    // 외부에서 카드의 시각적 상태를 강제로 변경할 때 사용
    public void SetViewState(CardViewState state)
    {
        _statsOverlayPanel.SetActive(state == CardViewState.ShowStats);
        _placingOverlayPanel.SetActive(state == CardViewState.Placing);
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        // 스스로 뭔가를 결정하지 않고, 자신을 클릭했다고 외부로 알리기만 함
        OnCardClicked?.Invoke(this);
    }
}