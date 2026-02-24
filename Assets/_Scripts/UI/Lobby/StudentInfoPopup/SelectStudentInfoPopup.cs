using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// 학생 상세 정보 오버레이 팝업
public class SelectStudentInfoPopup : UIBase
{
    [Header("Header")]
    [SerializeField] private TMP_Text _txtTitle;                      // 상단 타이틀
    [SerializeField] private Button _btnClose;                        // 닫기 버튼

    [Header("Left - Portrait")]
    [SerializeField] private Image _imgPortrait;                      // 초상화

    [Header("Right - Summary")]
    [SerializeField] private TMP_Text _txtName;                       // 이름
    [SerializeField] private TMP_Text _txtGrade;                      // 학년

    [Header("Right - Condition Gauge")]
    [SerializeField] private Image _imgConditionFill;                 // 컨디션 게이지 Fill
    [SerializeField] private int _conditionMaxValue = 130;            // 컨디션 최대값

    [Header("Right - Stat List")]
    [SerializeField] private Transform _statListRoot;                 // 스탯 리스트 부모
    [SerializeField] private SelectStudentStatRow _statRowPrefab;     // 스탯 행 프리팹

    private readonly List<SelectStudentStatRow> _spawnedRows = new(); // 생성된 스탯 행
    private bool _isInited;

    private void Awake()
    {
        // Raycast 차단 요소 제거 (버튼 클릭 방해 방지)
        if (_imgPortrait != null) _imgPortrait.raycastTarget = false;
        if (_txtTitle != null) _txtTitle.raycastTarget = false;
        if (_txtName != null) _txtName.raycastTarget = false;
        if (_txtGrade != null) _txtGrade.raycastTarget = false;
        if (_imgConditionFill != null) _imgConditionFill.raycastTarget = false;
    }

    public override void Init()
    {
        if (_isInited) return;
        _isInited = true;

        base.Init();

        // 닫기 버튼 바인딩
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

        // Init 누락 방지
        if (!_isInited)
            Init();
    }

    // 외부에서 데이터 세팅
    public void Setup(string title, Student student, Sprite portrait)
    {
        if (_txtTitle != null)
            _txtTitle.text = string.IsNullOrEmpty(title) ? "선택한 학생" : title;

        ApplyPortrait(portrait);
        ApplySummary(student);
        ApplyConditionGauge(student);
        BuildStatList(student);
    }

    // 닫기 처리
    private void CloseSelf()
    {
        ClearStatRows();
        Close(); // 비활성화
    }

    // 초상화 적용
    private void ApplyPortrait(Sprite portrait)
    {
        if (_imgPortrait == null) return;

        bool has = portrait != null;
        _imgPortrait.gameObject.SetActive(true);
        _imgPortrait.sprite = portrait;
        _imgPortrait.preserveAspect = true;

        // 이미지 없으면 반투명 표시
        _imgPortrait.color = has ? Color.white : new Color(1f, 1f, 1f, 0.15f);
    }

    // 기본 정보 표시
    private void ApplySummary(Student student)
    {
        if (student == null) return;

        if (_txtName != null) _txtName.text = student.studentName;
        if (_txtGrade != null) _txtGrade.text = $"{student.grade}학년";
    }

    // 컨디션 게이지 계산
    private void ApplyConditionGauge(Student student)
    {
        if (student == null) return;

        int max = Mathf.Max(1, _conditionMaxValue);
        int clamped = Mathf.Clamp(student.condition, 0, max);

        if (_imgConditionFill != null)
        {
            _imgConditionFill.type = Image.Type.Filled;
            _imgConditionFill.fillMethod = Image.FillMethod.Horizontal;
            _imgConditionFill.fillOrigin = (int)Image.OriginHorizontal.Left;
            _imgConditionFill.fillAmount = clamped / (float)max;
        }
    }

    // 스탯 리스트 생성
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

    // 스탯 행 1개 생성
    private void SpawnRow(string label, int value)
    {
        SelectStudentStatRow row = Instantiate(_statRowPrefab, _statListRoot);
        row.Setup(label, value);
        row.gameObject.SetActive(true);
        _spawnedRows.Add(row);
    }

    // 생성된 스탯 행 정리
    private void ClearStatRows()
    {
        foreach (SelectStudentStatRow row in _spawnedRows)
        {
            if (row != null) Destroy(row.gameObject);
        }
        _spawnedRows.Clear();
    }
}