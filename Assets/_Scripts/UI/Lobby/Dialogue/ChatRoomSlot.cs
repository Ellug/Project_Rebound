using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class ChatRoomSlot : MonoBehaviour
{
    [SerializeField] private TMP_Text _txtPreview;      // 버튼 안의 내용
    [SerializeField] private Image _btnBackgroundImage;   // 버튼 배경 
    [SerializeField] private Button _btnOpenRoom;

    [SerializeField] private Color _unreadBgColor = new Color(0.4f, 0.4f, 0.4f, 1f); // 밝은 회색
    [SerializeField] private Color _readBgColor = new Color(0.15f, 0.15f, 0.15f, 1f); // 어두운 회색

    private ChatRoom _currentRoom;
    private MessengerInboxPopup _parentPopup;

    public void Setup(ChatRoom room, MessengerInboxPopup parentPopup)
    {
        _currentRoom = room;
        _parentPopup = parentPopup;

        if (_btnBackgroundImage != null)
            _btnBackgroundImage.color = room.HasUnread ? _unreadBgColor : _readBgColor;

        if (_txtPreview != null && room.Messages.Count > 0)
        {
            string lastMsg = room.Messages[room.Messages.Count - 1].Content;
            if (room.RoomName.StartsWith("["))
                _txtPreview.text = $"{room.RoomName} {lastMsg}";
            else
                _txtPreview.text = $"{room.RoomName}: {lastMsg}";
        }

        if (_btnOpenRoom != null)
        {
            _btnOpenRoom.onClick.RemoveAllListeners();
            _btnOpenRoom.onClick.AddListener(OnSlotClicked);
        }
    }

    private void OnSlotClicked()
    {
        if (_parentPopup != null && _currentRoom != null)
            _parentPopup.OpenRoom(_currentRoom.RoomId);
    }
}