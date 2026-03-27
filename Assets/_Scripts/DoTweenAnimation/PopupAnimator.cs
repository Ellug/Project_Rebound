using System;
using System.Collections;
using UnityEngine;
using DG.Tweening;

// 팝업 등장/퇴장 애니메이션 처리
// 인스펙터에서 AnimationType 선택으로 연출 방식 지정
// Slide: 슬라이드 인/아웃 (X/Y 오프셋으로 방향 지정) / Pop: Scale 오버슈트 등장, Scale 0 퇴장
public class PopupAnimator : AnimatorBase
{
    public enum AnimationType
    {
        Slide,
        Pop,
        Swipe
    }

    [Header("연출 방식")]
    [SerializeField] private AnimationType _type = AnimationType.Slide;

    [Header("Slide 설정")]
    [SerializeField] private RectTransform _panelRoot;  // 미연결 시 자신의 RectTransform 사용
    [SerializeField] private float _hiddenOffsetX = 0f;
    [SerializeField] private float _hiddenOffsetY = -400f;
    [SerializeField] private float _slideInDuration = 0.2f;
    [SerializeField] private float _slideOutDuration = 0.28f;
    [SerializeField] private Ease _slideInEase = Ease.OutCubic;
    [SerializeField] private Ease _slideOutEase = Ease.InCubic;

    [Header("Pop 설정")]
    [SerializeField] private float _popInDuration = 0.18f;
    [SerializeField] private float _popOutDuration = 0.14f;
    [SerializeField] private Ease _popInEase = Ease.OutBack;
    [SerializeField] private Ease _popOutEase = Ease.InBack;
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

    private void Awake() => Initialize();

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
                _shownPos = _panelRoot.anchoredPosition;
                _hiddenPos = _shownPos + new Vector2(_hiddenOffsetX, _hiddenOffsetY);
                _panelRoot.anchoredPosition = _hiddenPos;
                break;

            case AnimationType.Pop:
                break;

            case AnimationType.Swipe:
                _shownPos = _panelRoot.anchoredPosition;
                _hiddenPos = _shownPos;
                // Swipe는 시작 위치를 건드리지 않음
                break;
        }
    }

    public override void PlayIn(Action onComplete = null)
    {
        if (!_isInited) Initialize();
        if (IsAnimating && _playingIn) return;

        _playingIn = true;

        switch (_type)
        {
            case AnimationType.Slide:
                StartCoroutine(SlideInRoutine(onComplete));
                break;

            case AnimationType.Pop:
                PlayInPop(onComplete);
                break;

            case AnimationType.Swipe:
                onComplete?.Invoke();
                break;
        }
    }

    public override void PlayOut(Action onComplete = null)
    {
        if (!_isInited) Initialize();
        if (IsAnimating && !_playingIn) return;

        _playingIn = false;

        switch (_type)
        {
            case AnimationType.Slide:
                PlayOutSlide(onComplete);
                break;

            case AnimationType.Pop:
                PlayOutPop(onComplete);
                break;

            case AnimationType.Swipe:
                onComplete?.Invoke();
                break;
        }
    }

    private IEnumerator SlideInRoutine(Action onComplete)
    {
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

    private void PlayInPop(Action onComplete)
    {
        _popTween?.Kill();
        IsAnimating = true;
        SetRaycastEnabled(false);

        _panelRoot.localScale = Vector3.zero;
        _popTween = _panelRoot.DOScale(Vector3.one, _popInDuration)
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

    private void PlayOutPop(Action onComplete)
    {
        _popTween?.Kill();
        IsAnimating = true;
        SetRaycastEnabled(false);

        _popTween = _panelRoot.DOScale(Vector3.zero, _popOutDuration)
            .SetEase(_popOutEase)
            .SetUpdate(true)
            .OnComplete(() =>
            {
                IsAnimating = false;
                SetRaycastEnabled(true);
                _panelRoot.localScale = Vector3.one;
                _popTween = null;
                onComplete?.Invoke();
            });
    }

    private void StartSlideTween(Vector2 target, float duration, Ease ease, Action onComplete)
    {
        _slideTween?.Kill();
        IsAnimating = true;
        SetRaycastEnabled(false);

        _slideTween = _panelRoot.DOAnchorPos(target, duration)
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

    // 스와이프 전용: X축 이동
    public void SlideToX(float targetX, float duration, Action onComplete = null, Ease ease = Ease.OutCubic)
    {
        if (_panelRoot == null)
            _panelRoot = GetComponent<RectTransform>();

        _slideTween?.Kill();
        IsAnimating = true;

        Vector2 target = _panelRoot.anchoredPosition;
        target.x = targetX;

        _slideTween = _panelRoot.DOAnchorPos(target, duration)
            .SetEase(ease)
            .SetUpdate(true)
            .OnComplete(() =>
            {
                IsAnimating = false;
                _slideTween = null;
                onComplete?.Invoke();
            });
    }

    public void SetPositionX(float x)
    {
        if (_panelRoot == null)
            _panelRoot = GetComponent<RectTransform>();

        Vector2 pos = _panelRoot.anchoredPosition;
        pos.x = x;
        _panelRoot.anchoredPosition = pos;
    }

    public float GetPositionX()
    {
        if (_panelRoot == null)
            _panelRoot = GetComponent<RectTransform>();

        return _panelRoot.anchoredPosition.x;
    }

    public void StopSlide()
    {
        _slideTween?.Kill();
        _slideTween = null;
        IsAnimating = false;
    }
}