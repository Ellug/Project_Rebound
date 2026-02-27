using TMPro;
using UnityEngine;
using System.Collections;

public class TextTyperTMP : MonoBehaviour
{
    [SerializeField] private TMP_Text tmpText;
    [SerializeField] private float charactersPerSecond = 30f;

    [Header("Auto type when tmpText.text changes")]
    [SerializeField] private bool autoTypeOnTextChange = true;

    private Coroutine typingCoroutine;
    private bool isTyping;

    private string _lastText = "";

    private void Awake()
    {
        if (tmpText == null)
            tmpText = GetComponent<TMP_Text>();

        if (tmpText != null)
            _lastText = tmpText.text;
    }

    private void OnEnable()
    {
        // 켜질 때도 한번 동기화
        if (tmpText != null)
            _lastText = tmpText.text;
    }

    private void Update()
    {
        if (!autoTypeOnTextChange || tmpText == null)
            return;

        // LobbyUI가 text를 바꾸면 여기서 감지
        if (tmpText.text != _lastText)
        {
            _lastText = tmpText.text;
            Play(_lastText);
        }
    }

    public void Play(string message)
    {
        if (typingCoroutine != null)
            StopCoroutine(typingCoroutine);

        // message로 고정 (중간에 덮어써져도 최소 한번은 타이핑 시작)
        tmpText.text = message;
        tmpText.maxVisibleCharacters = 0;

        tmpText.ForceMeshUpdate(true, true);

        typingCoroutine = StartCoroutine(TypeRoutine());
    }

    private IEnumerator TypeRoutine()
    {
        isTyping = true;

        tmpText.ForceMeshUpdate(true, true);
        int totalChars = tmpText.textInfo.characterCount;

        float delay = 1f / Mathf.Max(1f, charactersPerSecond);

        for (int i = 0; i <= totalChars; i++)
        {
            tmpText.maxVisibleCharacters = i;
            yield return new WaitForSeconds(delay);
        }

        isTyping = false;
        typingCoroutine = null;
    }

    public void Skip()
    {
        if (!isTyping || tmpText == null) return;

        if (typingCoroutine != null)
            StopCoroutine(typingCoroutine);

        tmpText.ForceMeshUpdate(true, true);
        tmpText.maxVisibleCharacters = tmpText.textInfo.characterCount;

        isTyping = false;
        typingCoroutine = null;
    }
}