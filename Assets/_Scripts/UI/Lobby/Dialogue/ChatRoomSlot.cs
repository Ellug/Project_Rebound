using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class ChatRoomSlot : MonoBehaviour
{
    [SerializeField] private TMP_Text _txtPreview;      // 버튼 안의 내용
    [SerializeField] private Image _btnBackgroundImage;   // 버튼 배경 
    [SerializeField] private Button _btnOpenRoom;   // 클릭 버튼


    [Header("안 읽은 상태 (새 메시지)")]
    [SerializeField] private Sprite _unreadSprite;        // 하얀색 배경 이미지
    [SerializeField] private Color _unreadTextColor = Color.black;

    [Header("읽은 상태 (확인함)")]
    [SerializeField] private Sprite _readSprite;          // 검은색 배경 이미지
    [SerializeField] private Color _readTextColor = Color.white;


    private ChatRoom _currentRoom;
    private MessengerInboxPopup _parentPopup;

    public void Setup(ChatRoom room, MessengerInboxPopup parentPopup)
    {
        _currentRoom = room;
        _parentPopup = parentPopup;

        // 1. 읽음/안읽음 상태에 따라 배경과 텍스트 색상 변경
        if (_btnBackgroundImage != null)
        {
            Sprite targetSprite = room.HasUnread ? _unreadSprite : _readSprite;
            if (targetSprite != null) _btnBackgroundImage.sprite = targetSprite;
        }

        if (_txtPreview != null)
        {
            _txtPreview.color = room.HasUnread ? _unreadTextColor : _readTextColor;

            if (room.Messages.Count > 0)
            {
                string lastMsg = room.Messages[room.Messages.Count - 1].Content;
                if (room.RoomName.StartsWith("["))
                    _txtPreview.text = $"{room.RoomName}\n{lastMsg}";
                else
                    _txtPreview.text = $"{room.RoomName}: {lastMsg}";
            }
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
        {
            _parentPopup.OpenRoom(_currentRoom.RoomId);
        }
    }
}