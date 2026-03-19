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

    private int _spawnedCount = 0;
    private DateTime? _lastDate = null;

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
    protected override void OnDestroy()
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

        foreach (var item in _spawnedItems)
            if (item != null) Destroy(item);
        _spawnedItems.Clear();
        _spawnedCount = 0;
        _lastDate = null;

        SpawnMissingMessages(room);

        if (MessengerManager.Instance != null)
            MessengerManager.Instance.MarkAsRead(CurrentRoomId);

        base.Open();

        if (UIManager.Instance != null)
        {
            UIManager.Instance.PushMessenger(this);
        }

        StartCoroutine(ScrollToBottomRoutine());
    }
    public override void Close()
    {
        if (MessengerManager.Instance != null)
        {
            MessengerManager.Instance.MarkAsRead(CurrentRoomId);
            MessengerManager.Instance.CurrentViewingRoomId = "";
        }

        if (!string.IsNullOrEmpty(CurrentRoomId))
        {
            if (CurrentRoomId.StartsWith("friendly_"))
            {
                if (FriendlyMatchRunner.Instance != null)
                {
                    FriendlyMatchRunner.Instance.SkipRoom(CurrentRoomId);
                }
            }
            else
            {
                if (DialogueRunner.Instance != null)
                {
                    DialogueRunner.Instance.SkipRoom(CurrentRoomId);
                }
            }
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

        bool isNearBottom = true;
        if (_scrollRect != null)
        {
            isNearBottom = _scrollRect.verticalNormalizedPosition <= 0.05f;
        }

        SpawnMissingMessages(room);

        // 유저가 밑을 보고 있었을 때만 스크롤을 자동으로 내려줌
        if (isNearBottom && gameObject.activeInHierarchy)
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

    private void SpawnDateDivider(DateTime date)
    {
        if (_dateDividerPrefab == null) return;
        GameObject divider = Instantiate(_dateDividerPrefab, _chatContentRoot);
        divider.SetActive(true);
        TMP_Text txtDate = divider.GetComponentInChildren<TMP_Text>();
        if (txtDate != null) txtDate.text = date.ToString("yyyy. M. d");
        _spawnedItems.Add(divider);
    }

    private void SpawnMissingMessages(ChatRoom room)
    {
        if (room == null || room.Messages == null) return;

        bool addedNew = false;

        // 이미 그려진 개수부터 시작해서 새로 온 메시지만 추가
        for (int i = _spawnedCount; i < room.Messages.Count; i++)
        {
            var msg = room.Messages[i];

            if (_lastDate == null || _lastDate.Value.Date != msg.Timestamp.Date)
            {
                _lastDate = msg.Timestamp.Date;
                SpawnDateDivider(_lastDate.Value);
            }

            if (msg.EventType == MessageEventType.Choice)
            {
                ChatChoiceBox choiceBox = Instantiate(_choiceBoxPrefab, _chatContentRoot);
                choiceBox.Setup(msg, this);
                choiceBox.gameObject.SetActive(true);
                _spawnedItems.Add(choiceBox.gameObject);
            }
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

            _spawnedCount++;
            addedNew = true;
        }

        if (addedNew)
        {
            Canvas.ForceUpdateCanvases();
        }
    }
}