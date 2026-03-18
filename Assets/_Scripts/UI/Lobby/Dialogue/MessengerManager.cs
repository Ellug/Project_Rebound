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

    public void ReceiveMessage(string roomId, string roomName, ChatMessage newMessage)
    {
        DateTime currentDate = DateTime.Now;
        TurnManager turnManager = FindFirstObjectByType<TurnManager>();
        if (turnManager != null && turnManager.DateManager != null)
            currentDate = turnManager.DateManager.CurrentDate;

        newMessage.Timestamp = currentDate;

        ChatRoom room = _activeRooms.Find(r => r.RoomId == roomId);

        bool isViewing = (CurrentViewingRoomId == roomId);

        if (room == null)
        {
            room = new ChatRoom { RoomId = roomId, RoomName = roomName, HasUnread = true };
            _activeRooms.Add(room);
        }
        else
        {
            room.HasUnread = true;
        }

        room.Messages.Add(newMessage);
        room.LastUpdatedDate = currentDate;

        OnRoomListUpdated?.Invoke();
        OnMessageAdded?.Invoke(room);
        OnLatestMessageReceived?.Invoke(newMessage);
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
        }
    }
}