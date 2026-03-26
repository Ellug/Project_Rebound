using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class MessengerManager : Singleton<MessengerManager>
{
    [SerializeField] private List<ChatRoom> _activeRooms = new List<ChatRoom>();

    public string CurrentViewingRoomId { get; set; } = "";

    public event Action OnRoomListUpdated;
    public event Action<ChatRoom> OnMessageAdded;
    public event Action<ChatMessage> OnLatestMessageReceived;

    public IReadOnlyList<ChatRoom> ActiveRooms => _activeRooms.OrderByDescending(r => r.LastUpdatedDate).ToList();

    protected override void OnSingletonAwake()
    {
        base.OnSingletonAwake();
        ClearAll();
    }

    public void ReceiveMessage(string roomId, string roomName, ChatMessage newMessage)
    {
        if (newMessage.Timestamp == default(DateTime) || newMessage.Timestamp.Date == DateTime.Now.Date)
        {
            DateTime currentDate = DateTime.Now;
            TurnManager turnManager = FindFirstObjectByType<TurnManager>();
            if (turnManager != null && turnManager.DateManager != null)
                currentDate = turnManager.DateManager.CurrentDate;

            newMessage.Timestamp = currentDate;
        }

        ChatRoom room = _activeRooms.Find(r => r.RoomId == roomId);

        bool isViewing = (CurrentViewingRoomId == roomId);

        if (room == null)
        {
            room = new ChatRoom { RoomId = roomId, RoomName = roomName, HasUnread = !isViewing };
            _activeRooms.Add(room);
        }
        else
        {
            if (!isViewing) room.HasUnread = true;
        }

        room.Messages.Add(newMessage);

        room.LastUpdatedDate = newMessage.Timestamp;

        OnRoomListUpdated?.Invoke();
        OnMessageAdded?.Invoke(room);
        OnLatestMessageReceived?.Invoke(newMessage);

        if (SaveManager.Instance != null && SaveManager.Instance.CurrentData != null)
        {
            Debug.Log("[MessengerManager] 메시지 수신 저장");
            SaveManager.Instance.SaveCurrent();
        }
    }

    public ChatRoom GetRoom(string roomId)
    {
        return _activeRooms.Find(r => r.RoomId == roomId);
    }

    public void MarkAsRead(string roomId)
    {
        var room = GetRoom(roomId);
        if (room != null && room.HasUnread)
        {
            room.HasUnread = false;
            OnRoomListUpdated?.Invoke();

            // 읽음 상태 저장
            if (SaveManager.Instance != null && SaveManager.Instance.CurrentData != null)
            {
                Debug.Log("[MessengerManager] 읽음 상태 저장");
                SaveManager.Instance.SaveCurrent();
            }
        }
    }

    // 현재 메신저 방 목록과 열람 상태를 모두 초기화
    public void ClearAll()
    {
        _activeRooms.Clear();
        CurrentViewingRoomId = "";
        OnRoomListUpdated?.Invoke();
    }

    // 현재 메신저 방/메시지/읽음 상태를 세이브용 데이터로 수집
    public SavedMessengerData CollectSaveData()
    {
        SavedMessengerData data = new SavedMessengerData
        {
            currentViewingRoomId = CurrentViewingRoomId,
            rooms = new List<SavedChatRoomData>()
        };

        foreach (ChatRoom room in _activeRooms)
        {
            SavedChatRoomData roomData = new SavedChatRoomData
            {
                roomId = room.RoomId,
                roomName = room.RoomName,
                hasUnread = room.HasUnread,
                lastUpdatedDate = room.LastUpdatedDate != default
                    ? room.LastUpdatedDate.ToString("yyyy-MM-dd HH:mm:ss")
                    : string.Empty,
                messages = new List<SavedChatMessageData>()
            };

            foreach (ChatMessage msg in room.Messages)
            {
                SavedChatMessageData msgData = new SavedChatMessageData
                {
                    senderType = (int)msg.SenderType,
                    eventType = (int)msg.EventType,
                    content = msg.Content,
                    timestamp = msg.Timestamp != default
                        ? msg.Timestamp.ToString("yyyy-MM-dd HH:mm:ss")
                        : string.Empty,
                    selectedChoiceIndex = msg.SelectedChoiceIndex,
                    choices = new List<SavedChoiceOptionData>()
                };

                if (msg.Choices != null)
                {
                    foreach (ChoiceOption choice in msg.Choices)
                    {
                        msgData.choices.Add(new SavedChoiceOptionData
                        {
                            text = choice != null ? choice.Text : string.Empty
                        });
                    }
                }

                roomData.messages.Add(msgData);
            }

            data.rooms.Add(roomData);
        }

        return data;
    }

    // 저장된 메신저 방/메시지/읽음 상태를 런타임 상태로 복원
    public void RestoreSaveData(SavedMessengerData data)
    {
        _activeRooms.Clear();
        CurrentViewingRoomId = "";

        if (data == null)
        {
            OnRoomListUpdated?.Invoke();
            return;
        }

        CurrentViewingRoomId = data.currentViewingRoomId ?? string.Empty;

        if (data.rooms != null)
        {
            foreach (SavedChatRoomData roomData in data.rooms)
            {
                ChatRoom room = new ChatRoom
                {
                    RoomId = roomData.roomId,
                    RoomName = roomData.roomName,
                    HasUnread = roomData.hasUnread,
                    LastUpdatedDate = ParseDateTime(roomData.lastUpdatedDate),
                    Messages = new List<ChatMessage>()
                };

                if (roomData.messages != null)
                {
                    foreach (SavedChatMessageData msgData in roomData.messages)
                    {
                        ChatMessage msg = new ChatMessage(
                            (MessageSenderType)msgData.senderType,
                            msgData.content,
                            (MessageEventType)msgData.eventType);

                        msg.Timestamp = ParseDateTime(msgData.timestamp);
                        msg.SelectedChoiceIndex = msgData.selectedChoiceIndex;
                        msg.Choices.Clear();

                        if (msgData.choices != null)
                        {
                            foreach (SavedChoiceOptionData choiceData in msgData.choices)
                            {
                                msg.Choices.Add(new ChoiceOption
                                {
                                    Text = choiceData.text,
                                    OnSelected = null
                                });
                            }
                        }

                        room.Messages.Add(msg);
                    }
                }

                _activeRooms.Add(room);
            }
        }

        OnRoomListUpdated?.Invoke();
    }

    // 저장된 문자열 날짜를 DateTime으로 변환 (실패 시 기본값 반환)
    private static DateTime ParseDateTime(string raw)
    {
        return DateTime.TryParse(raw, out DateTime result) ? result : default;
    }
}