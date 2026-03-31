using System;
using UnityEngine;

// 애니메이션 컴포넌트 공통 베이스
// 파생: PopupAnimator (팝업 슬라이드/팝), SceneTransitionAnimator (씬 전환, 미확정)
public abstract class AnimatorBase : MonoBehaviour
{
    public bool IsAnimating { get; protected set; }

    public abstract void PlayIn(Action onComplete = null);
    public abstract void PlayOut(Action onComplete = null);

    protected abstract void KillTween();
    protected virtual void OnDestroy() => KillTween();

    // 외부에서 즉시 중단이 필요할 때 호출 (씬 전환 직전 등)
    public void StopImmediate() => KillTween();
}