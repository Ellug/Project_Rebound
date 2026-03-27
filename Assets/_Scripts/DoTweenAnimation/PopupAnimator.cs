using System;
using System.Collections;
using UnityEngine;
using DG.Tweening;

// 팝업 등장/퇴장 애니메이션 처리
// 인스펙터에서 AnimationType 선택으로 연출 방식 지정
// Slide: 슬라이드 인/아웃 (X/Y 오프셋으로 방향 지정) / Pop: Scale 오버슈트 등장, Scale 0 퇴장
public class PopupAnimator : AnimatorBase
{
    public enum AnimationType { Slide, Pop }

    [Header("연출 방식")]
    [SerializeField] private AnimationType _type = AnimationType.Slide;

    [Header("Slide 설정")]
    [SerializeField] private RectTransform _panelRoot;  // 미연결 시 자신의 RectTransform 사용
    // 숨김 위치 오프셋 — X/Y 조합으로 슬라이드 방향 지정
    // 예) 아래에서 올라옴: X=0, Y=-400 / 오른쪽에서 들어옴: X=600, Y=0
    [SerializeField] private float _hiddenOffsetX = 0f;
    [SerializeField] private float _hiddenOffsetY = -400f;
    [SerializeField] private float _slideInDuration = 0.2f;
    [SerializeField] private float _slideOutDuration = 0.28f;
    [SerializeField] private Ease _slideInEase = Ease.OutCubic;
    [SerializeField] private Ease _slideOutEase = Ease.InCubic;

    [Header("Pop 설정")]
    // 등장: Scale 0 → 오버슈트 → 1 / 퇴장: Scale 1 → 0
    [SerializeField] private float _popInDuration = 0.18f;
    [SerializeField] private float _popOutDuration = 0.14f;
    [SerializeField] private Ease _popInEase = Ease.OutBack;
    [SerializeField] private Ease _popOutEase = Ease.InBack;
    // OutBack 오버슈트 강도 (기본값 1.70158 / 높을수록 더 튀어나옴)
    [SerializeField] private float _popOvershoot = 2.0f;

    [Header("공통")]
    [SerializeField] private bool _disableRaycastWhileTween = true;

    private bool _playingIn;
    private Vector2 _shownPos;
    private Vector2 _hiddenPos;
    private Tweener _slideTween;
    private Tweener _popTween;
    private CanvasGroup _canvasGroup;
    private bool _isInited;

    // 씬에서 활성 상태로 저장된 경우 Awake에서 자동 초기화
    // 씬에서 비활성 상태로 저장된 경우 Awake가 호출되지 않으므로
    // 팝업의 Show / Open에서 SetActive(true) 전에 Initialize()를 명시 호출해야 함
    private void Awake() => Initialize();

    // SetActive(true) 전, 오브젝트가 활성 상태일 때 호출해야 anchoredPosition을 올바르게 읽음
    // _isInited 플래그로 중복 실행 방지 (Awake에서 이미 실행된 경우 통과)
    public void Initialize()
    {
        if (_isInited) return;
        _isInited = true;

        if (_panelRoot == null)
            _panelRoot = GetComponent<RectTransform>();

        if (_disableRaycastWhileTween)
            _canvasGroup = GetComponent<CanvasGroup>() ?? gameObject.AddComponent<CanvasGroup>();

        switch (_type)
        {
            case AnimationType.Slide:
                // 에디터에서 잡힌 위치를 표시 위치로, 오프셋만큼 이동한 위치를 숨김 위치로 설정
                _shownPos = _panelRoot.anchoredPosition;
                _hiddenPos = _shownPos + new Vector2(_hiddenOffsetX, _hiddenOffsetY);
                _panelRoot.anchoredPosition = _hiddenPos;
                break;

            case AnimationType.Pop:
                // Scale은 PlayIn() 진입 시점에 0으로 설정
                // 여기서 미리 0으로 두면 LayoutGroup 재계산 시 형제 오브젝트 위치에 영향을 줌
                break;
        }
    }

    public override void PlayIn(Action onComplete = null)
    {
        // 이미 등장 애니메이션 진행 중이면 무시 (광클 대비)
        if (IsAnimating && _playingIn) return;

        _playingIn = true;
        switch (_type)
        {
            case AnimationType.Slide: StartCoroutine(SlideInRoutine(onComplete)); break;
            case AnimationType.Pop: PlayInPop(onComplete); break;
        }
    }

    public override void PlayOut(Action onComplete = null)
    {
        // 이미 퇴장 애니메이션 진행 중이면 무시 (광클 대비)
        if (IsAnimating && !_playingIn) return;

        _playingIn = false;
        switch (_type)
        {
            case AnimationType.Slide: PlayOutSlide(onComplete); break;
            case AnimationType.Pop: PlayOutPop(onComplete); break;
        }
    }

    private IEnumerator SlideInRoutine(Action onComplete)
    {
        // 레이아웃 확정 후 슬라이드 시작
        yield return null;
        Canvas.ForceUpdateCanvases();
        _panelRoot.anchoredPosition = _hiddenPos;
        StartSlideTween(_shownPos, _slideInDuration, _slideInEase, onComplete);
    }

    private void PlayOutSlide(Action onComplete)
    {
        StartSlideTween(_hiddenPos, _slideOutDuration, _slideOutEase, () =>
        {
            _panelRoot.anchoredPosition = _hiddenPos;
            onComplete?.Invoke();
        });
    }

    private void StartSlideTween(Vector2 target, float duration, Ease ease, Action onComplete)
    {
        _slideTween?.Kill();
        IsAnimating = true;
        SetRaycastEnabled(false);

        _slideTween = _panelRoot
            .DOAnchorPos(target, duration)
            .SetEase(ease)
            .SetUpdate(true)
            .OnComplete(() =>
            {
                IsAnimating = false;
                SetRaycastEnabled(true);
                _slideTween = null;
                onComplete?.Invoke();
            });
    }

    // Scale 0 → 오버슈트 → 1
    private void PlayInPop(Action onComplete)
    {
        _popTween?.Kill();
        IsAnimating = true;
        SetRaycastEnabled(false);
        _panelRoot.localScale = Vector3.zero;

        _popTween = _panelRoot
            .DOScale(Vector3.one, _popInDuration)
            .SetEase(_popInEase, _popOvershoot)
            .SetUpdate(true)
            .OnComplete(() =>
            {
                IsAnimating = false;
                SetRaycastEnabled(true);
                _popTween = null;
                onComplete?.Invoke();
            });
    }

    // Scale 1 → 0
    private void PlayOutPop(Action onComplete)
    {
        _popTween?.Kill();
        IsAnimating = true;
        SetRaycastEnabled(false);

        _popTween = _panelRoot
            .DOScale(Vector3.zero, _popOutDuration)
            .SetEase(_popOutEase)
            .SetUpdate(true)
            .OnComplete(() =>
            {
                IsAnimating = false;
                SetRaycastEnabled(true);
                // SetActive(false) 후에는 Scale이 레이아웃에 영향을 주지 않도록 1로 복원
                // zero로 두면 LayoutGroup이 재계산할 때 형제 오브젝트 크기에 영향을 줄 수 있음
                _panelRoot.localScale = Vector3.one;
                _popTween = null;
                onComplete?.Invoke();
            });
    }

    private void SetRaycastEnabled(bool enabled)
    {
        if (_canvasGroup == null) return;
        _canvasGroup.blocksRaycasts = enabled;
        _canvasGroup.interactable = enabled;
    }

    protected override void KillTween()
    {
        _slideTween?.Kill();
        _popTween?.Kill();
    }

    public void SetHiddenOffsetX(float offsetX)
    {
        if (_type != AnimationType.Slide) return;
        _hiddenOffsetX = offsetX;
        _hiddenPos = _shownPos + new Vector2(_hiddenOffsetX, _hiddenOffsetY);
    }
}