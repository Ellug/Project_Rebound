using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;


public class StudentCard : MonoBehaviour
{
    public enum CardViewState
    {
        Normal,
        ShowStats,
        Placing,
        Managing
    }

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

    [Header("Placing 오버레이 (영입 선택 중)")]
    [SerializeField] private GameObject _placingOverlayPanel;

    [Header("Managing 오버레이 (학생 관리 배치중)")]
    [SerializeField] private GameObject _managingOverlayPanel;

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

    public void SetStudentData(Student student)
    {
        _studentData = student;
        RefreshDisplay();
    }

    public void SetViewState(CardViewState state)
    {
        _currentState = state;
        RefreshDisplay();
    }

    public CardViewState GetViewState() => _currentState;
    public Student GetStudentData() => _studentData;

    public Sprite GetPortraitSprite()
    {
        return _portraitImage != null ? _portraitImage.sprite : null;
    }

    private void RefreshDisplay()
    {
        SafeSetActive(_statsOverlayPanel, _currentState == CardViewState.ShowStats);
        SafeSetActive(_placingOverlayPanel, _currentState == CardViewState.Placing);
        SafeSetActive(_managingOverlayPanel, _currentState == CardViewState.Managing);

        if (_currentState == CardViewState.ShowStats && _studentData != null)
            PopulateStatsOverlay(_studentData);
    }


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

    private void RefreshConditionGauge(Student student)
    {
        if (_conditionGaugeFill == null) return;

        int condMax = Mathf.Max(student.mental + 20, student.condition, 1);
        _conditionGaugeFill.fillAmount = (float)student.condition / condMax;
    }

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