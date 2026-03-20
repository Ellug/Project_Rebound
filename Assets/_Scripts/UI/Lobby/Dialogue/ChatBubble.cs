using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ChatBubble : MonoBehaviour
{
    [SerializeField] private TMP_Text _txtContent;
    [SerializeField] private LayoutElement _textLayoutElement; // 글자 크기 제한용 벽
    [SerializeField] private float _maxWidth = 441f;           // 최대 가로 길이
    public void Setup(string content)
    {
        Debug.Log($"<color=green>[메세지 텍스트 확인]</color> 전달받은 내용: {content}");
        if (_txtContent != null)
        {
            _txtContent.text = content;

            _txtContent.ForceMeshUpdate();

            if (_textLayoutElement != null)
            {
                if (_txtContent.preferredWidth > _maxWidth)
                {
                    _textLayoutElement.preferredWidth = _maxWidth;
                    _textLayoutElement.enabled = true;
                }
                else
                {
                    _textLayoutElement.enabled = false;
                }
            }
        }
    }
}