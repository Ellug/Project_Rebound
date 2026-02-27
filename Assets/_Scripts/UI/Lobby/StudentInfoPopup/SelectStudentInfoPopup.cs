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

    [Header("Select Button")]
    [SerializeField] private Button _btnSelect;                       // 학생 선택 버튼 (영입 팝업 전용)

    [Header("Slide Animation")]
    [SerializeField] private RectTransform _panelRoot;                // 실제로 움직일 루트(패널)
    [SerializeField] private float _hiddenOffsetY = -600f;            // 아래로 숨길 거리(픽셀)
    [SerializeField] private bool _disableRaycastWhileTween = true;   // 애니메이션 중 입력 차단(선택)

    // 위아래로 슬라이드 되는 애니메이션 설정
    [SerializeField] private float _slideInDuration = 0.2f;
    [SerializeField] private float _slideOutDuration = 0.28f;
    [SerializeField] private Ease _slideInEase = Ease.OutCubic;       // 슬라이드 인 이징
    [SerializeField] private Ease _slideOutEase = Ease.InCubic;       // 슬라이드 아웃 이징

    private readonly List<SelectStudentStatRow> _spawnedRows = new(); // 생성된 스탯 행
    private bool _isInited;

    private Vector2 _shownPos;
    private Vector2 _hiddenPos;

    private Tweener _slideTween;  // 현재 진행 중인 슬라이드 Tween
    private CanvasGroup _canvasGroup;

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

        // 움직일 루트 기본값 보정 (Panel Root 미연결 시 자신의 RectTransform으로 fallback)
        if (_panelRoot == null)
            _panelRoot = GetComponent<RectTransform>();

        // "표시 위치"는 에디터에서 잡힌 현재 위치
        _shownPos = _panelRoot.anchoredPosition;
        _hiddenPos = _shownPos + new Vector2(0f, _hiddenOffsetY);

        // 초기에는 숨김 위치로 이동 (비활성 상태에서 위치 선설정)
        _panelRoot.anchoredPosition = _hiddenPos;

        // 입력 차단용 (선택)
        if (_disableRaycastWhileTween)
        {
            _canvasGroup = GetComponent<CanvasGroup>();
            if (_canvasGroup == null)
                _canvasGroup = gameObject.AddComponent<CanvasGroup>();
        }

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

        // Init에서 버튼 숨기지 않음 → SetSelectAction에서만 제어
        // (Init은 최초 1회만 실행되므로 여기서 끄면 이후 SetSelectAction이 호출돼도
        //  Active 상태가 보장되지 않는 문제 방지)
    }

    public override void Open()
    {
        // Init 누락 방지
        if (!_isInited)
            Init();

        // Panel Root가 없으면 자신의 RectTransform으로 fallback
        if (_panelRoot == null)
            _panelRoot = GetComponent<RectTransform>();

        // 활성화 + 최상단
        gameObject.SetActive(true);
        transform.SetAsLastSibling();

        // 슬라이드 인은 다음 프레임에 실행
        // → SetActive(true) 직후에는 레이아웃이 미반영된 anchoredPosition을 읽을 수 있으므로
        //   한 프레임 뒤에 실제 위치를 읽고 슬라이드 시작
        StartCoroutine(OpenSlideRoutine());
    }

    private IEnumerator OpenSlideRoutine()
    {
        // 한 프레임 대기해 레이아웃 확정
        yield return null;

        Canvas.ForceUpdateCanvases();

        // 시작은 아래(숨김)에서
        _panelRoot.anchoredPosition = _hiddenPos;

        // 슬라이드 인
        PlaySlide(_shownPos, _slideInDuration, _slideInEase, null);
    }

    public override void Close()
    {
        if (!gameObject.activeSelf)
            return;

        // 슬라이드 아웃 → 끝나면 비활성
        PlaySlide(_hiddenPos, _slideOutDuration, _slideOutEase, () =>
        {
            // 다음 Open을 위해 Panel 위치 초기화
            _panelRoot.anchoredPosition = _hiddenPos;
            gameObject.SetActive(false);
        });
    }

    // 외부에서 데이터 세팅
    public void Setup(string title, Student student, Sprite portrait)
    {
        if (_txtTitle != null)
            _txtTitle.text = string.IsNullOrEmpty(title) ? "학생 정보" : title;

        ApplyPortrait(portrait);
        ApplySummary(student);
        ApplyConditionGauge(student);
        BuildStatList(student);
    }

    // 학생 선택 버튼 액션 주입 (영입 팝업에서 호출)
    // action이 null이면 버튼 숨김, 아니면 버튼 표시 후 클릭 시 action 실행
    public void SetSelectAction(Action action)
    {
        if (_btnSelect == null) return;

        _btnSelect.onClick.RemoveAllListeners();

        if (action == null)
        {
            _btnSelect.gameObject.SetActive(false);
            return;
        }

        _btnSelect.gameObject.SetActive(true);
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

    // DoTween 기반 슬라이드 실행
    // 진행 중인 Tween이 있으면 즉시 Kill 후 새로 시작
    private void PlaySlide(Vector2 targetPos, float duration, Ease ease, Action onComplete)
    {
        // 기존 Tween 즉시 중단
        _slideTween?.Kill();

        // 입력 차단 시작
        SetRaycastBlock(false);

        _slideTween = _panelRoot
            .DOAnchorPos(targetPos, duration)
            .SetEase(ease)
            .SetUpdate(true) // TimeScale 영향 제외 (unscaledDeltaTime 대응)
            .OnComplete(() =>
            {
                // 입력 차단 해제
                SetRaycastBlock(true);
                _slideTween = null;
                onComplete?.Invoke();
            });
    }

    // CanvasGroup 기반 입력 차단 On/Off
    private void SetRaycastBlock(bool allow)
    {
        if (_canvasGroup == null) return;

        _canvasGroup.blocksRaycasts = allow;
        _canvasGroup.interactable = allow;
    }

    private void OnDestroy()
    {
        // 오브젝트 파괴 시 Tween 정리
        _slideTween?.Kill();
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