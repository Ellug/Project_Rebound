using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class ChatChoiceBox : MonoBehaviour
{
    [System.Serializable]
    public class ChoiceButtonUI
    {
        public Button button;
        public Image bgImage;
        public TMP_Text text;
    }

    [SerializeField] private List<ChoiceButtonUI> _choiceButtons;

    [SerializeField] private Color _normalBgColor = new Color(0.4f, 0.4f, 0.4f, 1f);
    [SerializeField] private Color _selectedBgColor = new Color(0.1f, 0.1f, 0.1f, 1f);
    [SerializeField] private Color _unselectedBgColor = new Color(0.9f, 0.9f, 0.9f, 0.3f);
    [SerializeField] private Color _unselectedTextColor = new Color(0.7f, 0.7f, 0.7f, 1f);

    private ChatMessage _messageData;
    private MessengerRoomPopup _parentRoom;

    public void Setup(ChatMessage messageData, MessengerRoomPopup parentRoom)
    {
        _messageData = messageData;
        _parentRoom = parentRoom;

        for (int i = 0; i < _choiceButtons.Count; i++)
        {
            if (i < messageData.Choices.Count)
            {
                _choiceButtons[i].button.gameObject.SetActive(true);
                _choiceButtons[i].text.text = $"{i + 1}. {messageData.Choices[i].Text}";

                int choiceIndex = i;
                _choiceButtons[i].button.onClick.RemoveAllListeners();
                _choiceButtons[i].button.onClick.AddListener(() => OnChoiceClicked(choiceIndex));
            }
            else
            {
                _choiceButtons[i].button.gameObject.SetActive(false);
            }
        }

        RefreshVisualState();
    }

    private void OnChoiceClicked(int index)
    {
        if (_messageData.SelectedChoiceIndex != -1) return;

        _messageData.SelectedChoiceIndex = index;
        RefreshVisualState();

        ChatMessage myReply = new ChatMessage(MessageSenderType.Me, _messageData.Choices[index].Text);
        string roomId = _parentRoom.CurrentRoomId;
        string roomName = _parentRoom.CurrentRoomName;

        MessengerManager.Instance.ReceiveMessage(roomId, roomName, myReply);
        _messageData.Choices[index].OnSelected?.Invoke();
    }

    private void RefreshVisualState()
    {
        bool isAnswered = _messageData.SelectedChoiceIndex != -1;

        for (int i = 0; i < _messageData.Choices.Count; i++)
        {
            var btnUI = _choiceButtons[i];
            btnUI.button.interactable = !isAnswered;

            if (!isAnswered)
            {
                btnUI.bgImage.color = _normalBgColor;
                btnUI.text.color = Color.white;
            }
            else
            {
                if (i == _messageData.SelectedChoiceIndex)
                {
                    btnUI.bgImage.color = _selectedBgColor;
                    btnUI.text.color = Color.white;
                }
                else
                {
                    btnUI.bgImage.color = _unselectedBgColor;
                    btnUI.text.color = _unselectedTextColor;
                }
            }
        }
    }
}