using System;
using System.Collections;
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

    [Header("Friendly Match")]
    [SerializeField] private Button _btnFriendlyMatch;
    [SerializeField] private FriendlyMatchSelectPopup _friendlyMatchSelectPopup;

    [Header("애니메이션")]
    [SerializeField] private PopupAnimator _animator;

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
        // 재구독 (씬 복귀 시 중복 방지)
        if (MessengerManager.Instance != null)
        {
            MessengerManager.Instance.OnRoomListUpdated -= RefreshList;
            MessengerManager.Instance.OnRoomListUpdated += RefreshList;
        }

        if (_animator == null)
        {
            Debug.LogWarning("[MessengerInboxPopup] _animator가 연결되지 않았습니다. 인스펙터에서 PopupAnimator를 연결해주세요.");
            base.Open();
        }
        else
        {
            _animator.Initialize();
            base.Open();
            _animator.PlayIn();
        }

        if (UIManager.Instance != null) UIManager.Instance.PushMessenger(this);

        RefreshList();
        RefreshFriendlyMatchUI();
    }

    public override void Close()
    {
        // 구독 해제
        if (MessengerManager.Instance != null)
            MessengerManager.Instance.OnRoomListUpdated -= RefreshList;

        if (UIManager.Instance != null) UIManager.Instance.PopMessenger(this);

        if (_animator == null || !gameObject.activeSelf)
        {
            base.Close();
        }
        else
        {
            PlayPopupCloseSfx();
            _animator.PlayOut(() =>
            {
                gameObject.SetActive(false);

                if (SuddenEventManager.Instance != null)
                {
                    SuddenEventManager.Instance.ProcessNextPopup();
                }
            });
            return;
        }

        if (SuddenEventManager.Instance != null)
        {
            SuddenEventManager.Instance.ProcessNextPopup();
        }
    }
    public void RefreshList()
    {
        foreach (var item in _spawnedItems) Destroy(item);
        _spawnedItems.Clear();

        var rooms = MessengerManager.Instance.ActiveRooms;
        DateTime? currentDateGroup = null;

        foreach (var room in rooms)
        {
            // 친선 경기 채팅방은 목록에 절대 나타나지 않도록 차단
            if (room.RoomId.StartsWith("friendly_")) continue;

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

        Canvas.ForceUpdateCanvases();
        UnityEngine.UI.ScrollRect scrollRect = GetComponentInChildren<UnityEngine.UI.ScrollRect>();
        if (scrollRect != null)
        {
            scrollRect.verticalNormalizedPosition = 1f;
        }
        StartCoroutine(ForceScrollTopRoutine());
    }
    private IEnumerator ForceScrollTopRoutine()
    {
        yield return null; // UI 레이아웃이 크기를 완전히 잡을 때까지 1프레임 대기
        Canvas.ForceUpdateCanvases();

        UnityEngine.UI.ScrollRect scrollRect = GetComponentInChildren<UnityEngine.UI.ScrollRect>();
        if (scrollRect != null)
        {
            scrollRect.StopMovement(); // 진행 중인 관성 스크롤 강제 정지
            scrollRect.verticalNormalizedPosition = 1f; // 최상단으로 이동
            scrollRect.velocity = Vector2.zero;
        }
    }

    public void OpenRoom(string roomId)
    {
        if (_roomPopupPrefab == null)
        {
            return;
        }

        ChatRoom room = MessengerManager.Instance.GetRoom(roomId);
        if (room == null)
        {
            Debug.LogError($"[MessengerInboxPopup] '{roomId}' 채팅방을 찾을 수 없습니다!");
            return;
        }

        if (_currentRoomPopup == null)
        {
            _currentRoomPopup = Instantiate(_roomPopupPrefab, transform.parent);
        }
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
        if (txtDate != null) txtDate.text = date.ToString("yyyy. M. d");
        _spawnedItems.Add(divider);
    }

    public void RefreshFriendlyMatchUI()
    {
        if (_btnFriendlyMatch == null) return;

        _btnFriendlyMatch.onClick.RemoveAllListeners();
        _btnFriendlyMatch.onClick.AddListener(() => {
            if (_friendlyMatchSelectPopup != null)
            {
                _friendlyMatchSelectPopup.Init();
                _friendlyMatchSelectPopup.Open();
            }
        });
    }

    protected override void OnDestroy()
    {
        base.OnDestroy();

        if (MessengerManager.Instance != null)
        {
            MessengerManager.Instance.OnRoomListUpdated -= RefreshList;
        }
    }
}