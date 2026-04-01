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

    [Header("Common")]
    [SerializeField] private Image _portraitImage;

    [Header("Stats Overlay")]
    [SerializeField] private GameObject _statsOverlayPanel;
    [SerializeField] private TMP_Text _txtName;
    [SerializeField] private TMP_Text _txtGrade;

    [SerializeField] private Image _conditionGaugeFill;
    [SerializeField] private Image _conditionGaugeDeltaFill;
    [SerializeField] private Sprite _conditionGaugeDeltaIncreaseSprite;
    [SerializeField] private Sprite _conditionGaugeDeltaDecreaseSprite;

    [SerializeField] private TMP_Text _txtMental;
    [SerializeField] private TMP_Text _txtShoot;
    [SerializeField] private TMP_Text _txtSpeed;
    [SerializeField] private TMP_Text _txtJump;
    [SerializeField] private TMP_Text _txtStamina;

    [SerializeField] private TMP_Text _txtMentalDelta;
    [SerializeField] private TMP_Text _txtShootDelta;
    [SerializeField] private TMP_Text _txtSpeedDelta;
    [SerializeField] private TMP_Text _txtJumpDelta;
    [SerializeField] private TMP_Text _txtStaminaDelta;

    [SerializeField] private PortraitLibrary _portraitLibrary;

    [SerializeField] private Color _deltaPositiveColor = new(0.25f, 0.55f, 1.00f);
    [SerializeField] private Color _deltaNegativeColor = new(0.90f, 0.25f, 0.25f);

    [Header("Placing Overlay")]
    [SerializeField] private GameObject _placingOverlayPanel;

    [Header("Managing Overlay")]
    [SerializeField] private GameObject _managingOverlayPanel;


    [SerializeField] private GameObject _disease;
    [SerializeField] private GameObject _injury;

    private Student _studentData;
    private StudentCardPreviewDelta _previewDelta;
    private CardViewState _currentState = CardViewState.Normal;

    void Awake()
    {
        Button btn = GetComponent<Button>();
        if (btn == null)
            btn = gameObject.AddComponent<Button>();

        btn.onClick.RemoveAllListeners();
        btn.onClick.AddListener(() => OnCardClicked?.Invoke(this));
    }
    void OnEnable()
    {
        if (StudentManager.Instance != null)
            StudentManager.Instance.OnStudentModified += HandleStudentModified;
    }

    void OnDisable()
    {
        if (StudentManager.Instance != null)
            StudentManager.Instance.OnStudentModified -= HandleStudentModified;
    }

    private void HandleStudentModified(Student student)
    {
        // 내 카드에 할당된 학생의 데이터가 바뀌었다면 즉시 UI 새로고침
        if (_studentData != null && _studentData.id == student.id)
        {
            _studentData = student;
            RefreshDisplay();
        }
    }
    public void SetStudentData(Student student)
    {
        _studentData = student;
        _previewDelta = default;
        ApplyPortrait(student);
        RefreshDisplay();
    }

    public void SetPreviewDelta(StudentCardPreviewDelta previewDelta)
    {
        _previewDelta = previewDelta;
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
        RefreshAbnormalIndicator();

        if (_currentState == CardViewState.ShowStats && _studentData != null)
            PopulateStatsOverlay(_studentData);
    }

    private void PopulateStatsOverlay(Student student)
    {
        SetText(_txtName, student.studentName);
        SetText(_txtGrade, $"{student.grade}학년");

        SetText(_txtMental, FormatStatText("멘탈", student.mental));
        SetText(_txtShoot, FormatStatText("슈팅", student.shoot));
        SetText(_txtSpeed, FormatStatText("속도", student.speed));
        SetText(_txtJump, FormatStatText("점프력", student.jump));
        SetText(_txtStamina, FormatStatText("지구력", student.stamina));

        RefreshStatDeltaTexts();
        RefreshConditionGauge(student);
    }

    private void RefreshStatDeltaTexts()
    {
        ApplyDeltaText(_txtMentalDelta, ResolveStatDelta(StudentCoreStat.Mental, _previewDelta.mental));
        ApplyDeltaText(_txtShootDelta, ResolveStatDelta(StudentCoreStat.Shoot, _previewDelta.shoot));
        ApplyDeltaText(_txtSpeedDelta, ResolveStatDelta(StudentCoreStat.Speed, _previewDelta.speed));
        ApplyDeltaText(_txtJumpDelta, ResolveStatDelta(StudentCoreStat.Jump, _previewDelta.jump));
        ApplyDeltaText(_txtStaminaDelta, ResolveStatDelta(StudentCoreStat.Stamina, _previewDelta.stamina));
    }

    private int ResolveStatDelta(StudentCoreStat stat, int deltaOrExp)
    {
        if (!_previewDelta.treatStatFieldsAsExp || _studentData == null || deltaOrExp == 0)
            return deltaOrExp;

        return StudentStatExpSystem.PredictStatLevelDelta(_studentData, stat, deltaOrExp);
    }

    private void RefreshConditionGauge(Student student)
    {
        if (_conditionGaugeFill == null) return;

        int current = Student.ClampCondition(student.condition);
        int preview = Student.ClampCondition(current + _previewDelta.condition);

        float current01 = Mathf.Clamp01((float)current / Student.ConditionMax);
        float preview01 = Mathf.Clamp01((float)preview / Student.ConditionMax);

        if (_conditionGaugeDeltaFill == null || _previewDelta.condition == 0)
        {
            if (_conditionGaugeDeltaFill != null)
                _conditionGaugeDeltaFill.gameObject.SetActive(false);

            _conditionGaugeFill.fillAmount = current01;
            return;
        }

        _conditionGaugeDeltaFill.gameObject.SetActive(true);

        if (_previewDelta.condition < 0)
        {
            if (_conditionGaugeDeltaDecreaseSprite != null)
                _conditionGaugeDeltaFill.sprite = _conditionGaugeDeltaDecreaseSprite;

            _conditionGaugeDeltaFill.color = _deltaNegativeColor;
            _conditionGaugeDeltaFill.fillAmount = current01;
            _conditionGaugeFill.fillAmount = preview01;
            return;
        }

        if (_conditionGaugeDeltaIncreaseSprite != null)
            _conditionGaugeDeltaFill.sprite = _conditionGaugeDeltaIncreaseSprite;

        _conditionGaugeDeltaFill.color = _deltaPositiveColor;
        _conditionGaugeDeltaFill.fillAmount = preview01;
        _conditionGaugeFill.fillAmount = current01;
    }

    private void ApplyDeltaText(TMP_Text target, int delta)
    {
        if (target == null) return;

        if (delta == 0)
        {
            target.gameObject.SetActive(false);
            return;
        }

        target.gameObject.SetActive(true);
        target.text = delta > 0 ? $"+{delta}" : delta.ToString();
        target.color = delta > 0 ? _deltaPositiveColor : _deltaNegativeColor;
    }

    private static string FormatStatText(string statName, int currentValue)
    {
        return $"{statName} {currentValue}";
    }

    private static void SetText(TMP_Text target, string text)
    {
        if (target != null)
            target.text = text;
    }

    private static void SafeSetActive(GameObject target, bool active)
    {
        if (target != null)
            target.SetActive(active);
    }

    // 상태이상 학생 카드에 표시
    private void RefreshAbnormalIndicator()
    {
        if (_studentData == null)
        {
            SafeSetActive(_disease, false);
            SafeSetActive(_injury, false);
            return;
        }

        SafeSetActive(_disease, _studentData.abnormalState == Student.AbnormalType.Disease);
        SafeSetActive(_injury, _studentData.abnormalState == Student.AbnormalType.Injury);
    }

    private void ApplyPortrait(Student student)
    {
        if (_portraitImage == null || _portraitLibrary == null || student == null)
            return;

        _portraitImage.sprite = _portraitLibrary.Get(student.portraitColor, student.portraitIndex);
    }
}
