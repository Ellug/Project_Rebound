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
    [SerializeField] private ChatBubble _bubbleSystemPrefab;
    [SerializeField] private ScrollRect _scrollRect;
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
    void OnDestroy()
    {
        if (MessengerManager.Instance != null)
        {
            MessengerManager.Instance.OnMessageAdded -= HandleNewMessage;
        }
    }
    public void OpenRoom(ChatRoom room)
    {
        Init();
        CurrentRoomId = room.RoomId;
        CurrentRoomName = room.RoomName;

        if (MessengerManager.Instance != null)
        {
            MessengerManager.Instance.CurrentViewingRoomId = CurrentRoomId;
            MessengerManager.Instance.MarkAsRead(CurrentRoomId);
        }

        if (_txtRoomName != null) _txtRoomName.text = room.RoomName;

        RefreshChat(room);
        MessengerManager.Instance.MarkAsRead(CurrentRoomId);
        base.Open();

        if (UIManager.Instance != null)
        {
            UIManager.Instance.PushMessenger(this);
        }
    }
    public override void Close()
    {
        if (MessengerManager.Instance != null)
        {
            MessengerManager.Instance.CurrentViewingRoomId = "";
        }

        if (DialogueRunner.Instance != null && !string.IsNullOrEmpty(CurrentRoomId))
        {
            DialogueRunner.Instance.SkipRoom(CurrentRoomId);
        }

        base.Close();

        if (UIManager.Instance != null)
        {
            UIManager.Instance.PopMessenger(this);
        }
    }

    private void HandleNewMessage(ChatRoom room)
    {
        if (room.RoomId != CurrentRoomId) return;

        foreach (var item in _spawnedItems)
        {
            Destroy(item);
        }
        _spawnedItems.Clear();

        DateTime? currentDateGroup = null;

        foreach (var msg in room.Messages)
        {
            if (currentDateGroup == null || currentDateGroup.Value.Date != msg.Timestamp.Date)
            {
                currentDateGroup = msg.Timestamp.Date;
                SpawnDateDivider(currentDateGroup.Value);
            }

            // 1. 선택지 분기
            if (msg.EventType == MessageEventType.Choice)
            {
                ChatChoiceBox choiceBox = Instantiate(_choiceBoxPrefab, _chatContentRoot);
                choiceBox.Setup(msg, this);
                choiceBox.gameObject.SetActive(true);
                _spawnedItems.Add(choiceBox.gameObject);
            }
            // 2.  시스템 메시지 분기
            else if (msg.EventType == MessageEventType.System)
            {
                if (_bubbleSystemPrefab != null)
                {
                    ChatBubble bubble = Instantiate(_bubbleSystemPrefab, _chatContentRoot);
                    bubble.Setup(msg.Content);
                    bubble.gameObject.SetActive(true);
                    _spawnedItems.Add(bubble.gameObject);
                }
            }
            // 3. 기존 일반 텍스트 분기
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

        if (gameObject.activeInHierarchy)
        {
            StartCoroutine(ScrollToBottomRoutine());
        }
    }
    private System.Collections.IEnumerator ScrollToBottomRoutine()
    {
        yield return null; // UI 레이아웃이 계산될 때까지 1프레임 대기
        Canvas.ForceUpdateCanvases();

        if (_scrollRect != null)
        {
            _scrollRect.verticalNormalizedPosition = 0f; // 0이 맨 아래, 1이 맨 위
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
            else if (msg.EventType == MessageEventType.System) // 시스템 메시지 분기 처리
            {
                if (_bubbleSystemPrefab != null)
                {
                    ChatBubble bubble = Instantiate(_bubbleSystemPrefab, _chatContentRoot);
                    bubble.Setup(msg.Content);
                    bubble.gameObject.SetActive(true);
                    _spawnedItems.Add(bubble.gameObject);
                }
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