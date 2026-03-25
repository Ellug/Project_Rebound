using System;
using System.Collections;
using UnityEngine;

// TMP maxVisibleCharacters 기반의 공용 타이핑 연출 실행기
public sealed class TextTypewriter
{
    private readonly MonoBehaviour _coroutineHost;
    private Coroutine _typingCoroutine;

    // 타이핑 진행 상태
    public bool IsTyping { get; private set; }

    public TextTypewriter(MonoBehaviour coroutineHost)
    {
        _coroutineHost = coroutineHost;
    }

    // 타이핑 시작
    public void StartTyping(
        int startVisibleCharacters,
        int targetVisibleCharacters,
        float charactersPerSecond,
        Action<int> setVisibleCharacters,
        Action revealInstantly,
        Action onCompleted = null)
    {
        StopTyping();

        if (_coroutineHost == null || setVisibleCharacters == null || revealInstantly == null)
        {
            revealInstantly?.Invoke();
            IsTyping = false;
            onCompleted?.Invoke();
            return;
        }

        if (targetVisibleCharacters <= startVisibleCharacters || charactersPerSecond <= 0f)
        {
            revealInstantly();
            IsTyping = false;
            onCompleted?.Invoke();
            return;
        }

        IsTyping = true;
        _typingCoroutine = _coroutineHost.StartCoroutine(TypeRoutine(
            startVisibleCharacters,
            targetVisibleCharacters,
            Mathf.Max(1f, charactersPerSecond),
            setVisibleCharacters,
            revealInstantly,
            onCompleted));
    }

    // 타이핑 강제 완료
    public void CompleteTyping(Action revealInstantly, Action onCompleted = null)
    {
        if (!IsTyping && _typingCoroutine == null)
            return;

        StopTyping();
        revealInstantly?.Invoke();
        onCompleted?.Invoke();
    }

    // 타이핑 코루틴 중단
    public void StopTyping()
    {
        if (_typingCoroutine != null && _coroutineHost != null)
            _coroutineHost.StopCoroutine(_typingCoroutine);

        _typingCoroutine = null;
        IsTyping = false;
    }

    // 프레임 기반 문자 증가 루프
    private IEnumerator TypeRoutine(
        int startVisibleCharacters,
        int targetVisibleCharacters,
        float charactersPerSecond,
        Action<int> setVisibleCharacters,
        Action revealInstantly,
        Action onCompleted)
    {
        float visibleCharacters = Mathf.Max(0, startVisibleCharacters);
        setVisibleCharacters(Mathf.FloorToInt(visibleCharacters));

        while (visibleCharacters < targetVisibleCharacters)
        {
            visibleCharacters += charactersPerSecond * Time.unscaledDeltaTime;
            setVisibleCharacters(Mathf.FloorToInt(visibleCharacters));
            yield return null;
        }

        revealInstantly();
        _typingCoroutine = null;
        IsTyping = false;
        onCompleted?.Invoke();
    }
}
