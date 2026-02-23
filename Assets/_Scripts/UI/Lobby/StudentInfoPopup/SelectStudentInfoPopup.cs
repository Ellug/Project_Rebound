using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class SelectStudentInfoPopup : UIBase
{
    [Header("Header")]
    [SerializeField] private TMP_Text _txtTitle;
    [SerializeField] private Button _btnClose;

    [Header("Left - Portrait")]
    [SerializeField] private Image _imgPortrait;

    [Header("Right - Summary")]
    [SerializeField] private TMP_Text _txtName;
    [SerializeField] private TMP_Text _txtGrade;

    [Header("Right - Condition Gauge")]
    [SerializeField] private Image _imgConditionFill;          // Right/ConditionGauge/GaugeFill
    [SerializeField] private int _conditionMaxValue = 130;

    [Header("Right - Stat List")]
    [SerializeField] private Transform _statListRoot;
    [SerializeField] private SelectStudentStatRow _statRowPrefab;

    private readonly List<SelectStudentStatRow> _spawnedRows = new();
    private bool _isInited;

    private void Awake()
    {
        // Raycast 방해 가능 요소 제거
        if (_imgPortrait != null)
            _imgPortrait.raycastTarget = false;

        if (_txtTitle != null)
            _txtTitle.raycastTarget = false;

        if (_txtName != null)
            _txtName.raycastTarget = false;

        if (_txtGrade != null)
            _txtGrade.raycastTarget = false;

        if (_imgConditionFill != null)
            _imgConditionFill.raycastTarget = false;
    }

    public override void Init()
    {
        if (_isInited) return;
        _isInited = true;

        base.Init();

        if (_btnClose != null)
        {
            _btnClose.onClick.RemoveAllListeners();
            _btnClose.onClick.AddListener(CloseSelf);

            _btnClose.interactable = true;
            _btnClose.enabled = true;

            if (_btnClose.targetGraphic != null)
                _btnClose.targetGraphic.raycastTarget = true;
        }
        else
        {
            Debug.LogWarning("[SelectStudentInfoPopup] _btnClose가 연결되지 않았습니다.");
        }
    }

    public override void Open()
    {
        base.Open();

        // Init 호출 누락 방지
        if (!_isInited)
            Init();
    }

    public void Setup(string title, Student student, Sprite portrait)
    {
        if (_txtTitle != null)
            _txtTitle.text = string.IsNullOrEmpty(title) ? "선택한 학생" : title;

        ApplyPortrait(portrait);
        ApplySummary(student);
        ApplyConditionGauge(student);
        BuildStatList(student);
    }

    private void CloseSelf()
    {
        ClearStatRows();
        Close(); // = SetActive(false)
    }

    private void ApplyPortrait(Sprite portrait)
    {
        if (_imgPortrait == null) return;

        bool has = portrait != null;
        _imgPortrait.gameObject.SetActive(true);
        _imgPortrait.sprite = portrait;
        _imgPortrait.preserveAspect = true;
        _imgPortrait.color = has ? Color.white : new Color(1f, 1f, 1f, 0.15f);
    }

    private void ApplySummary(Student student)
    {
        if (student == null) return;

        if (_txtName != null) _txtName.text = student.studentName;
        if (_txtGrade != null) _txtGrade.text = $"{student.grade}학년";
    }

    private void ApplyConditionGauge(Student student)
    {
        if (student == null) return;

        int conditionValue = student.condition;

        int max = Mathf.Max(1, _conditionMaxValue);
        int clamped = Mathf.Clamp(conditionValue, 0, max);

        if (_imgConditionFill != null)
        {
            _imgConditionFill.type = Image.Type.Filled;
            _imgConditionFill.fillMethod = Image.FillMethod.Horizontal;
            _imgConditionFill.fillOrigin = (int)Image.OriginHorizontal.Left;
            _imgConditionFill.fillAmount = clamped / (float)max;
        }
    }

    private void BuildStatList(Student student)
    {
        ClearStatRows();

        if (student == null || _statRowPrefab == null || _statListRoot == null)
            return;

        SpawnRow("지구력", student.stamina);
        SpawnRow("멘탈", student.mental);
        SpawnRow("슈팅", student.shoot);
        SpawnRow("점프력", student.jump);
        SpawnRow("속도", student.speed);
    }

    private void SpawnRow(string label, int value)
    {
        SelectStudentStatRow row = Instantiate(_statRowPrefab, _statListRoot);
        row.Setup(label, value);
        row.gameObject.SetActive(true);
        _spawnedRows.Add(row);
    }

    private void ClearStatRows()
    {
        foreach (SelectStudentStatRow row in _spawnedRows)
        {
            if (row != null) Destroy(row.gameObject);
        }
        _spawnedRows.Clear();
    }
}