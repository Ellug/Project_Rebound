using TMPro;
using UnityEngine;
using System.Collections;



public class TextTyperTMP : MonoBehaviour
{
    [Header("Reference")]
    [SerializeField] private TMP_Text tmpText;

    [Header("Typing Settings")]
    [SerializeField] private float charactersPerSecond = 30f;
    [SerializeField] private bool useUnscaledTime = true; // 타임스케일 영향 여부

    private Coroutine typingCoroutine;
    private bool isTyping;
    private bool isCompleted;

    public bool IsTyping => isTyping;
    public bool IsCompleted => isCompleted;

    private void Awake()
    {
        if (tmpText == null)
            tmpText = GetComponent<TMP_Text>();
    }

    /// <summary>
    /// 텍스트 타이핑 시작
    /// </summary>
    public void Play(string message)
    {
        if (tmpText == null || string.IsNullOrEmpty(message))
            return;

        // 이전 코루틴 정리
        if (typingCoroutine != null)
            StopCoroutine(typingCoroutine);

        tmpText.text = message;
        tmpText.maxVisibleCharacters = 0;

        tmpText.ForceMeshUpdate(true, true);

        isTyping = true;
        isCompleted = false;

        typingCoroutine = StartCoroutine(TypeRoutine());
    }

    private IEnumerator TypeRoutine()
    {
        tmpText.ForceMeshUpdate(true, true);
        int totalChars = tmpText.textInfo.characterCount;

        float visibleCount = 0f;
        float cps = Mathf.Max(1f, charactersPerSecond);

        while (visibleCount < totalChars)
        {
            float delta = useUnscaledTime ? Time.unscaledDeltaTime : Time.deltaTime;
            visibleCount += cps * delta;

            tmpText.maxVisibleCharacters = Mathf.FloorToInt(visibleCount);

            yield return null;
        }

        // 완전 표시
        tmpText.maxVisibleCharacters = totalChars;

        isTyping = false;
        isCompleted = true;
        typingCoroutine = null;
    }

    /// <summary>
    /// 타이핑 중이면 즉시 완료
    /// </summary>
    public void Skip()
    {
        if (!isTyping || tmpText == null)
            return;

        if (typingCoroutine != null)
            StopCoroutine(typingCoroutine);

        tmpText.ForceMeshUpdate(true, true);
        tmpText.maxVisibleCharacters = tmpText.textInfo.characterCount;

        isTyping = false;
        isCompleted = true;
        typingCoroutine = null;
    }

    /// <summary>
    /// 현재 텍스트 즉시 교체 (애니메이션 없음)
    /// </summary>
    public void SetImmediate(string message)
    {
        if (tmpText == null)
            return;

        if (typingCoroutine != null)
            StopCoroutine(typingCoroutine);

        tmpText.text = message;
        tmpText.ForceMeshUpdate(true, true);
        tmpText.maxVisibleCharacters = tmpText.textInfo.characterCount;

        isTyping = false;
        isCompleted = true;
    }
}