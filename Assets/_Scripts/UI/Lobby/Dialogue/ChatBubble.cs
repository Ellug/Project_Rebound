using UnityEngine;
using TMPro;

public class ChatBubble : MonoBehaviour
{
    [SerializeField] private TMP_Text _txtContent;

    public void Setup(string content)
    {
        if (_txtContent != null)
            _txtContent.text = content;
    }
}