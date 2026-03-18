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

    [Header("Button Sprites (버튼 이미지)")]
    [SerializeField] private Sprite _normalSprite;     // 1. 기본 상태 (하얀 배경)
    [SerializeField] private Sprite _selectedSprite;   // 2. 선택된 상태 (까만 배경)
    [SerializeField] private Sprite _unselectedSprite; // 3. 선택 안 된 상태 (회색 테두리)

    [Header("Text Colors (글자 색상)")]
    [SerializeField] private Color _normalTextColor = Color.black;
    [SerializeField] private Color _selectedTextColor = Color.white;
    [SerializeField] private Color _unselectedTextColor = Color.gray;

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
                _choiceButtons[i].text.text = messageData.Choices[i].Text;

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

        _messageData.Choices[index].OnSelected?.Invoke();
    }

    private void RefreshVisualState()
    {
        bool isAnswered = _messageData.SelectedChoiceIndex != -1;

        for (int i = 0; i < _messageData.Choices.Count; i++)
        {
            if (i >= _messageData.Choices.Count) continue;

            var btnUI = _choiceButtons[i];
            btnUI.button.interactable = !isAnswered;

            // 스프라이트 본연의 색을 내기 위해 틴트를 흰색으로 고정
            btnUI.bgImage.color = Color.white;

            if (!isAnswered)
            {
                // 1. 선택 전
                btnUI.bgImage.sprite = _normalSprite;
                btnUI.text.color = _normalTextColor;
            }
            else
            {
                if (i == _messageData.SelectedChoiceIndex)
                {
                    // 2. 내가 선택한 버튼 
                    btnUI.bgImage.sprite = _selectedSprite;
                    btnUI.text.color = _selectedTextColor;
                }
                else
                {
                    // 3. 선택받지 못한 버튼
                    btnUI.bgImage.sprite = _unselectedSprite;
                    btnUI.text.color = _unselectedTextColor;
                }
            }
        }
    }
}