using System;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;

// 하단 슬라이드업 노드 상세 패널 (기획서 4-3. 하단 영역 상세)
// 노드 선택 시 이름, 효과 설명, 소모 비용, 해금 불가 사유를 표시
public class HeadCoachNodeInfoPopup : MonoBehaviour
{
    [SerializeField] private TMP_Text _txtName;
    [SerializeField] private TMP_Text _txtDescription;
    [SerializeField] private TMP_Text _txtCost;
    [SerializeField] private TMP_Text _txtUnlockBlockReason;    // 해금 불가 사유 (선행 노드 미습득 등)
    [SerializeField] private Button _btnUnlock;                 // 해금 가능 시 interactable=true / 불가 시 false
    [SerializeField] private Button _btnClose;                  // 닫기 버튼

    [Header("Slide Animation")]
    [SerializeField] private RectTransform _panelRoot;                // 실제로 움직일 루트(패널)
    [SerializeField] private float _hiddenOffsetY = -400f;            // 아래로 숨길 거리(픽셀)
    [SerializeField] private bool _disableRaycastWhileTween = true;   // 애니메이션 중 입력 차단

    // 위아래로 슬라이드 되는 애니메이션 설정
    [SerializeField] private float _slideInDuration = 0.2f;
    [SerializeField] private float _slideOutDuration = 0.28f;
    [SerializeField] private Ease _slideInEase = Ease.OutCubic;       // 슬라이드 인 이징
    [SerializeField] private Ease _slideOutEase = Ease.InCubic;       // 슬라이드 아웃 이징

    private HeadCoachNode _selectedNode;
    private Action<int> _onUnlockRequested;

    private Vector2 _shownPos;
    private Vector2 _hiddenPos;
    private Tweener _slideTween;  // 현재 진행 중인 슬라이드 Tween
    private CanvasGroup _canvasGroup;
    private bool _isInited;

    void Awake()
    {
        Init();
    }

    private void Init()
    {
        if (_isInited) return;
        _isInited = true;

        // 움직일 루트 기본값 보정 (Panel Root 미연결 시 자신의 RectTransform으로 fallback)
        if (_panelRoot == null)
            _panelRoot = GetComponent<RectTransform>();

        // "표시 위치"는 에디터에서 잡힌 현재 위치
        _shownPos = _panelRoot.anchoredPosition;
        _hiddenPos = _shownPos + new Vector2(0f, _hiddenOffsetY);

        // 초기에는 숨김 위치로 이동 (비활성 상태에서 위치 선설정)
        _panelRoot.anchoredPosition = _hiddenPos;
        gameObject.SetActive(false);

        // 입력 차단용 (선택)
        if (_disableRaycastWhileTween)
        {
            _canvasGroup = GetComponent<CanvasGroup>();
            if (_canvasGroup == null)
                _canvasGroup = gameObject.AddComponent<CanvasGroup>();
        }

        if (_btnUnlock != null)
            _btnUnlock.onClick.AddListener(OnUnlockClicked);

        // 닫기 버튼 바인딩
        if (_btnClose != null)
            _btnClose.onClick.AddListener(Hide);
        else
            Debug.LogWarning("[HeadCoachNodeInfoPopup] _btnClose가 연결되지 않았습니다.");
    }

    public void Show(HeadCoachNode node, Action<int> onUnlockRequested)
    {
        _selectedNode = node;
        _onUnlockRequested = onUnlockRequested;
        Refresh();

        gameObject.SetActive(true);
        transform.SetAsLastSibling();

        // 슬라이드 인은 다음 프레임에 실행
        // SetActive(true) 직후에는 레이아웃이 미반영된 anchoredPosition을 읽을 수 있으므로
        // 한 프레임 뒤에 실제 위치를 읽고 슬라이드 시작
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

    public void Hide()
    {
        if (!gameObject.activeSelf) return;

        // 슬라이드 아웃 → 끝나면 비활성
        PlaySlide(_hiddenPos, _slideOutDuration, _slideOutEase, () =>
        {
            // 다음 Open을 위해 Panel 위치 초기화
            _panelRoot.anchoredPosition = _hiddenPos;
            gameObject.SetActive(false);
            _selectedNode = null;
        });
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

    private void Refresh()
    {
        if (_selectedNode == null) return;

        SetText(_txtName, _selectedNode.Name);
        SetText(_txtDescription, _selectedNode.nodeData.description);

        bool isUnlocked = _selectedNode.IsUnlocked;
        bool canUnlock = !isUnlocked && _selectedNode.ArePrerequisitesMet();
        bool isBlocked = !isUnlocked && !_selectedNode.ArePrerequisitesMet();

        SetText(_txtCost, isUnlocked ? "해금 완료" : $"명성치 {_selectedNode.UnlockCost} 소모");

        // 버튼은 항상 표시, 해금 불가 상태일 때만 interactable = false
        if (_btnUnlock != null)
            _btnUnlock.interactable = canUnlock;

        // 해금 불가 사유는 버튼 위에 표시 (isBlocked일 때만 텍스트 노출)
        SetText(_txtUnlockBlockReason, isBlocked ? "선행 노드를 먼저 해금해야 합니다." : string.Empty);
    }

    private void OnUnlockClicked()
    {
        if (_selectedNode == null) return;
        _onUnlockRequested?.Invoke(_selectedNode.NodeId);
    }

    private static void SetText(TMP_Text t, string v) { if (t != null) t.text = v; }
}