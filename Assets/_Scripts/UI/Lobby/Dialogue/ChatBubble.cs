using UnityEngine;
using TMPro;

public class ChatBubble : MonoBehaviour
{
    [SerializeField] private TMP_Text _txtContent;

    public void Setup(string content)
    {
        Debug.Log($"<color=green>[메세지 텍스트 확인]</color> 전달받은 내용: {content}");
        if (_txtContent != null)
            _txtContent.text = content;
    }
}