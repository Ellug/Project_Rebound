using System;
using System.Collections.Generic;

public enum MessageSenderType
{
    Them,       // 상대방 (좌측 정렬)
    Me,          // 나 (우측 정렬)
    System
}

public enum MessageEventType
{
    NormalText,
    Choice,
    System
}

[Serializable]
public class ChoiceOption
{
    public string Text;
    public Action OnSelected;
}

[Serializable]
public class ChatMessage
{
    public MessageSenderType SenderType;
    public MessageEventType EventType;
    public string Content;
    public DateTime Timestamp;

    public List<ChoiceOption> Choices;
    public int SelectedChoiceIndex = -1;

    public ChatMessage(MessageSenderType senderType, string content, MessageEventType eventType = MessageEventType.NormalText)
    {
        SenderType = senderType;
        Content = content;
        EventType = eventType;
        Choices = new List<ChoiceOption>();
        Timestamp = DateTime.Now;
    }
}

[Serializable]
public class ChatRoom
{
    public string RoomId;
    public string RoomName;
    public List<ChatMessage> Messages = new List<ChatMessage>();
    public bool HasUnread;
    public DateTime LastUpdatedDate;
}