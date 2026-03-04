using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class MessengerRoomPopup : UIBase
{
    [SerializeField] private TMP_Text _txtRoomName;
    [SerializeField] private Transform _chatContentRoot;
    [SerializeField] private Button _btnRoomClose;

    [SerializeField] private GameObject _dateDividerPrefab;
    [SerializeField] private ChatBubble _bubbleLeftPrefab;
    [SerializeField] private ChatBubble _bubbleRightPrefab;
    [SerializeField] private ChatChoiceBox _choiceBoxPrefab;

    private List<GameObject> _spawnedItems = new List<GameObject>();

    public string CurrentRoomId { get; private set; }
    public string CurrentRoomName { get; private set; }

    private bool _isInited;

    public override void Init()
    {
        if (_isInited) return;
        _isInited = true;
        base.Init();

        if (_btnRoomClose != null)
        {
            _btnRoomClose.onClick.RemoveAllListeners();
            _btnRoomClose.onClick.AddListener(Close);
        }

        if (MessengerManager.Instance != null)
        {
            MessengerManager.Instance.OnMessageAdded -= HandleNewMessage;
            MessengerManager.Instance.OnMessageAdded += HandleNewMessage;
        }
    }

    public void OpenRoom(ChatRoom room)
    {
        Init();
        CurrentRoomId = room.RoomId;
        CurrentRoomName = room.RoomName;

        if (_txtRoomName != null) _txtRoomName.text = room.RoomName;

        RefreshChat(room);
        MessengerManager.Instance.MarkAsRead(CurrentRoomId);
        base.Open();

        if (UIManager.Instance != null) UIManager.Instance.PushMessenger(this);
    }
    public override void Close()
    {
        base.Close();

        if (UIManager.Instance != null) UIManager.Instance.PopMessenger(this);
    }

    private void HandleNewMessage(ChatRoom room)
    {
        if (room.RoomId == CurrentRoomId)
        {
            RefreshChat(room);
            MessengerManager.Instance.MarkAsRead(CurrentRoomId);
        }
    }

    private void RefreshChat(ChatRoom room)
    {
        foreach (var item in _spawnedItems)
            if (item != null) Destroy(item);
        _spawnedItems.Clear();

        DateTime? currentDateGroup = null;

        foreach (var msg in room.Messages)
        {
            if (currentDateGroup == null || currentDateGroup.Value.Date != msg.Timestamp.Date)
            {
                currentDateGroup = msg.Timestamp.Date;
                SpawnDateDivider(currentDateGroup.Value);
            }

            if (msg.EventType == MessageEventType.Choice)
            {
                ChatChoiceBox choiceBox = Instantiate(_choiceBoxPrefab, _chatContentRoot);
                choiceBox.Setup(msg, this);
                choiceBox.gameObject.SetActive(true);
                _spawnedItems.Add(choiceBox.gameObject);
            }
            else
            {
                ChatBubble prefabToUse = msg.SenderType == MessageSenderType.Them ? _bubbleLeftPrefab : _bubbleRightPrefab;

                if (prefabToUse != null)
                {
                    ChatBubble bubble = Instantiate(prefabToUse, _chatContentRoot);
                    bubble.Setup(msg.Content);
                    bubble.gameObject.SetActive(true);
                    _spawnedItems.Add(bubble.gameObject);
                }
            }
        }

        Canvas.ForceUpdateCanvases();
    }

    private void SpawnDateDivider(DateTime date)
    {
        if (_dateDividerPrefab == null) return;
        GameObject divider = Instantiate(_dateDividerPrefab, _chatContentRoot);
        divider.SetActive(true);
        TMP_Text txtDate = divider.GetComponentInChildren<TMP_Text>();
        if (txtDate != null) txtDate.text = date.ToString("yyyy. M. d");
        _spawnedItems.Add(divider);
    }
}