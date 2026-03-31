using System;
using DG.Tweening;
using UnityEngine;

// AnimatorBase를 상속해 PopupAnimator와 동일한 패턴으로 동작한다
public class EndingCreditAnimator : AnimatorBase
{
    [Header("대상")]
    [Tooltip("미연결 시 자신의 CanvasGroup을 자동 사용 (없으면 AddComponent)")]
    [SerializeField] private CanvasGroup _canvasGroup;

    [Header("페이드 설정")]
    [SerializeField] private float _fadeInDuration = 1.0f;
    [SerializeField] private float _fadeOutDuration = 1.2f;
    [SerializeField] private Ease _fadeInEase = Ease.InOutSine;
    [SerializeField] private Ease _fadeOutEase = Ease.InOutSine;

    private Tweener _fadeTween;

    private void Awake()
    {
        if (_canvasGroup == null)
            _canvasGroup = GetComponent<CanvasGroup>() ?? gameObject.AddComponent<CanvasGroup>();

        _canvasGroup.alpha = 0f;
    }

    public override void PlayIn(Action onComplete = null)
    {
        if (IsAnimating && _fadeTween != null) return;
        FadeTo(0f, 1f, _fadeInDuration, _fadeInEase, onComplete);
    }

    public override void PlayOut(Action onComplete = null)
    {
        float current = _canvasGroup != null ? _canvasGroup.alpha : 1f;
        float duration = _fadeOutDuration * current;
        FadeTo(current, 0f, duration, _fadeOutEase, onComplete);
    }

    private void FadeTo(float from, float to, float duration, Ease ease, Action onComplete)
    {
        if (_canvasGroup == null)
        {
            onComplete?.Invoke();
            return;
        }

        _fadeTween?.Kill();
        IsAnimating = true;
        _canvasGroup.alpha = from;

        _fadeTween = _canvasGroup
            .DOFade(to, Mathf.Max(0f, duration))
            .SetEase(ease)
            .SetUpdate(true)
            .OnComplete(() =>
            {
                IsAnimating = false;
                _fadeTween = null;
                onComplete?.Invoke();
            });
    }

    protected override void KillTween()
    {
        _fadeTween?.Kill();
        _fadeTween = null;
    }

    // EndingCreditUI에서 Skip 조건 판단에 사용
    public float CurrentAlpha => _canvasGroup != null ? _canvasGroup.alpha : 0f;
}