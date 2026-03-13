using UnityEngine;

[CreateAssetMenu(menuName = "Game/Event/Executor/MessengerExecutor")]
public class MessengerEventExecutor : ScriptableObject
{
    [SerializeField] private string _roomId = "event_01";
    [SerializeField] private string _roomName = "시스템 알림";
    [SerializeField] private MessageSenderType _senderType = MessageSenderType.Them;
    [TextArea(3, 5)]
    [SerializeField] private string _messageContent = "메시지 내용";
    [SerializeField] private MessageEventType _eventType = MessageEventType.NormalText;

    public void Execute(GameState gameState)
    {
        ChatMessage newMsg = new ChatMessage(_senderType, _messageContent, _eventType);

        if (MessengerManager.Instance != null)
        {
            MessengerManager.Instance.ReceiveMessage(_roomId, _roomName, newMsg);
        }
    }
}