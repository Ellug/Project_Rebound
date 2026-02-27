using TMPro;
using UnityEngine;
using System.Collections;

public class TextTyperTMP : MonoBehaviour
{
    [SerializeField] private TMP_Text tmpText;
    [SerializeField] private float charactersPerSecond = 30f;

    private Coroutine typingCoroutine;
    private bool isTyping;



    private void Start()
    {
        Play(tmpText.text);
    }

    public void Play(string message)
    {
        if (typingCoroutine != null)
            StopCoroutine(typingCoroutine);

        tmpText.text = message;
        tmpText.ForceMeshUpdate();
        tmpText.maxVisibleCharacters = 0;

        typingCoroutine = StartCoroutine(TypeRoutine());
    }

    private IEnumerator TypeRoutine()
    {
        isTyping = true;

        int totalChars = tmpText.textInfo.characterCount;
        float delay = 1f / charactersPerSecond;

        for (int i = 0; i <= totalChars; i++)
        {
            tmpText.maxVisibleCharacters = i;
            yield return new WaitForSeconds(delay);
        }

        isTyping = false;
    }

    public void Skip()
    {
        if (!isTyping) return;

        StopCoroutine(typingCoroutine);
        tmpText.maxVisibleCharacters = tmpText.textInfo.characterCount;
        isTyping = false;
    }
}