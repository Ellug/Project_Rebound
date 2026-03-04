using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class MessengerInboxPopup : UIBase
{
    [SerializeField] private Transform _slotContentRoot;
    [SerializeField] private ChatRoomSlot _slotPrefab;
    [SerializeField] private GameObject _dateDividerPrefab;
    [SerializeField] private Button _btnInboxClose;
    [SerializeField] private MessengerRoomPopup _roomPopupPrefab;

    private List<GameObject> _spawnedItems = new List<GameObject>();
    private bool _isInited;

    public override void Init()
    {
        if (_isInited) return;
        _isInited = true;
        base.Init();

        if (_btnInboxClose != null)
        {
            _btnInboxClose.onClick.RemoveAllListeners();
            _btnInboxClose.onClick.AddListener(Close);
        }

        if (MessengerManager.Instance != null)
        {
            MessengerManager.Instance.OnRoomListUpdated -= RefreshList;
            MessengerManager.Instance.OnRoomListUpdated += RefreshList;
        }
    }

    public override void Open()
    {
        Init();
        RefreshList();
        base.Open();

        if (UIManager.Instance != null) UIManager.Instance.PushMessenger(this);
    }

    public override void Close()
    {
        gameObject.SetActive(false);
        
        if (UIManager.Instance != null)
        {
            UIManager.Instance.PopMessenger(this);
        }

        base.Close();

        if (UIManager.Instance != null) UIManager.Instance.PopMessenger(this);
    }

    private void RefreshList()
    {
        foreach (var item in _spawnedItems)
            if (item != null) Destroy(item);
        _spawnedItems.Clear();

        if (MessengerManager.Instance == null) return;

        var rooms = MessengerManager.Instance.ActiveRooms;
        DateTime? currentDateGroup = null;

        foreach (var room in rooms)
        {
            if (currentDateGroup == null || currentDateGroup.Value.Date != room.LastUpdatedDate.Date)
            {
                currentDateGroup = room.LastUpdatedDate.Date;
                SpawnDateDivider(currentDateGroup.Value);
            }

            ChatRoomSlot slot = Instantiate(_slotPrefab, _slotContentRoot);
            slot.Setup(room, this);
            slot.gameObject.SetActive(true);
            _spawnedItems.Add(slot.gameObject);
        }
    }

    private void SpawnDateDivider(DateTime date)
    {
        if (_dateDividerPrefab == null) return;

        GameObject divider = Instantiate(_dateDividerPrefab, _slotContentRoot);
        divider.SetActive(true);

        TMP_Text txtDate = divider.GetComponentInChildren<TMP_Text>();
        if (txtDate != null)
            txtDate.text = date.ToString("yyyy. M. d");

        _spawnedItems.Add(divider);
    }

    public void OpenRoom(string roomId)
    {
        if (_roomPopupPrefab == null) return;

        ChatRoom room = MessengerManager.Instance.GetRoom(roomId);
        if (room == null) return;

        MessengerRoomPopup popup = Instantiate(_roomPopupPrefab, transform.parent);
        popup.Init();
        popup.OpenRoom(room);
    }
}