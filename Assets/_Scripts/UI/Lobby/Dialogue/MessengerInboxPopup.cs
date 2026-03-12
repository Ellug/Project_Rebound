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


    private MessengerRoomPopup _currentRoomPopup;
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

        if (UIManager.Instance != null)
        {
            UIManager.Instance.PushMessenger(this);
        }
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

    // 1. 리스트를 새로 그릴 때 기존 항목들을 깔끔하게 지워주는 로직 추가
    private void RefreshList()
    {
        // [핵심 추가] 기존에 생성된 방 슬롯과 날짜 구분선 싹 다 지우기
        foreach (var item in _spawnedItems)
        {
            if (item != null) Destroy(item);
        }
        _spawnedItems.Clear();

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

    // 2. 채팅방 팝업을 열 때 무조건 화면 맨 앞으로 끌어오는 로직 추가
    public void OpenRoom(string roomId)
    {
        if (_roomPopupPrefab == null) return;

        ChatRoom room = MessengerManager.Instance.GetRoom(roomId);
        if (room == null) return;

        if (_currentRoomPopup == null)
        {
            _currentRoomPopup = Instantiate(_roomPopupPrefab, transform.parent);
        }

        // [핵심 추가] 채팅방 UI를 하이어라키 최하단으로 내려서 가장 앞에 보이게 만듦
        _currentRoomPopup.transform.SetAsLastSibling();

        _currentRoomPopup.OpenRoom(room);
        _currentRoomPopup.gameObject.SetActive(true);
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

}