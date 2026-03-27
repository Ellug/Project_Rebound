using System.Collections;
using System.Collections.Generic;
using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;

// 학생 상세 정보 오버레이 팝업
public class SelectStudentInfoPopup : UIBase
{
    [Header("Header")]
    [SerializeField] private TMP_Text _txtTitle;                      // 상단 타이틀
    [SerializeField] private Button _btnInfoClose;                    // 닫기 버튼

    [Header("Left - Portrait")]
    [SerializeField] private Image _imgPortrait;                      // 초상화
    [SerializeField] private GameObject _disease;
    [SerializeField] private GameObject _injury;

    [Header("Right - Summary")]
    [SerializeField] private TMP_Text _txtName;                       // 이름
    [SerializeField] private TMP_Text _txtGrade;                      // 학년

    [Header("Right - Condition Gauge")]
    [SerializeField] private Image _imgConditionFill;                 // 컨디션 게이지 Fill
    [SerializeField] private int _conditionMaxValue = Student.ConditionMax; // 컨디션 최대값

    [Header("Right - Stat List")]
    [SerializeField] private Transform _statListRoot;                 // 스탯 리스트 부모
    [SerializeField] private SelectStudentStatRow _statRowPrefab;     // 스탯 행 프리팹

    [Header("Select Button")]
    [SerializeField] private Button _btnSelect;                       // 학생 선택 버튼 (영입 팝업 전용)
    [SerializeField] private TMP_Text _txtSelectButton;               // 선택 버튼 텍스트 (선택/해제 라벨)

    [Header("애니메이션")]
    [SerializeField] private PopupAnimator _animator;

    private readonly List<SelectStudentStatRow> _spawnedRows = new(); // 생성된 스탯 행
    private bool _isInited;

    public override void Init()
    {
        if (_isInited) return;
        _isInited = true;

        base.Init();

        // 닫기 버튼 바인딩
        if (_btnInfoClose != null)
        {
            _btnInfoClose.onClick.RemoveAllListeners();
            _btnInfoClose.onClick.AddListener(CloseSelf);

            _btnInfoClose.interactable = true;
            _btnInfoClose.enabled = true;

            if (_btnInfoClose.targetGraphic != null)
                _btnInfoClose.targetGraphic.raycastTarget = true;
        }
        else
        {
            Debug.LogWarning("[SelectStudentInfoPopup] _btnClose가 연결되지 않았습니다.");
        }

        // Init에서 버튼 숨기지 않음 → SetSelectAction에서만 제어
        // (Init은 최초 1회만 실행되므로 여기서 끄면 이후 SetSelectAction이 호출돼도
        //  Active 상태가 보장되지 않는 문제 방지)
    }

    public override void Open()
    {
        // Init 누락 방지
        if (!_isInited)
            Init();

        // SetActive(true) 전에 Initialize를 먼저 호출해야 _shownPos를 올바르게 읽음
        // Instantiate 후 Awake가 정상 호출되면 내부 _isInited 플래그로 중복 실행 방지
        _animator.Initialize();

        // 활성화 + 최상단
        gameObject.SetActive(true);
        PlayPopupOpenSfx();
        transform.SetAsLastSibling();

        // SetActive(true) 직후 레이아웃 미반영 문제로 PlayIn 내부에서 한 프레임 대기 후 실행
        _animator.PlayIn();
    }

    public override void Close()
    {
        if (!gameObject.activeSelf)
            return;

        PlayPopupCloseSfx();

        // 슬라이드 아웃 → 끝나면 비활성
        _animator.PlayOut(() =>
        {
            gameObject.SetActive(false);
        });
    }

    // 외부에서 데이터 세팅
    public void Setup(string title, Student student, Sprite portrait)
    {
        if (_txtTitle != null)
            _txtTitle.text = string.IsNullOrEmpty(title) ? "학생 정보" : title;

        ApplyPortrait(portrait);
        ApplyAbnormalIndicator(student);
        ApplySummary(student);
        ApplyConditionGauge(student);
        BuildStatList(student);
    }

    // 학생 선택 버튼 액션/라벨 주입 (영입 팝업에서 호출)
    // action이 null이면 버튼 숨김, 아니면 버튼 표시 후 클릭 시 action 실행
    public void SetSelectAction(Action action, string buttonText = null)
    {
        if (_btnSelect == null) return;

        _btnSelect.onClick.RemoveAllListeners();

        if (action == null)
        {
            _btnSelect.gameObject.SetActive(false);
            return;
        }

        _btnSelect.gameObject.SetActive(true);
        if (!string.IsNullOrEmpty(buttonText) && _txtSelectButton != null)
            _txtSelectButton.text = buttonText;

        _btnSelect.onClick.AddListener(() => action.Invoke());
    }

    // 닫기 처리
    private void CloseSelf()
    {
        // 닫힐 때 선택 버튼 액션 초기화 (다음 오픈 시 오염 방지)
        SetSelectAction(null);
        ClearStatRows();
        Close(); // 내려가며 닫힘
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

    private void ApplyAbnormalIndicator(Student student)
    {
        if (student == null)
        {
            SafeSetActive(_disease, false);
            SafeSetActive(_injury, false);
            return;
        }

        SafeSetActive(_disease, student.abnormalState == Student.AbnormalType.Disease);
        SafeSetActive(_injury, student.abnormalState == Student.AbnormalType.Injury);
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

        int max = Mathf.Clamp(_conditionMaxValue, 1, Student.ConditionMax);
        int clamped = Student.ClampCondition(student.condition);

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

    private static void SafeSetActive(GameObject target, bool active)
    {
        if (target != null)
            target.SetActive(active);
    }
}