using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;
using System.Linq;

public class FriendlyMatchSelectPopup : UIBase
{
    [SerializeField] private TMP_InputField _inputSearch;
    [SerializeField] private Transform _contentRoot;
    [SerializeField] private Button _btnSchoolPrefab;
    [SerializeField] private Button _btnClose;

    [SerializeField] private TMP_Text _txtFriendlyMatchCount;

    private List<GameObject> _spawnedItems = new List<GameObject>();

    public override void Init()
    {
        base.Init();
        if (_btnClose != null) _btnClose.onClick.AddListener(Close);
        if (_inputSearch != null) _inputSearch.onValueChanged.AddListener(OnSearchValueChanged);
    }

    public override void Open()
    {
        base.Open();
        if (UIManager.Instance != null) UIManager.Instance.PushMessenger(this);
        transform.SetAsLastSibling();

        if (_inputSearch != null) _inputSearch.text = "";
        UpdateMatchCountUI();
        PopulateSchools("");
    }
    public override void Close()
    {
        if (UIManager.Instance != null) UIManager.Instance.PopMessenger(this);
        base.Close();
    }

    public void UpdateMatchCountUI()
    {
        if (_txtFriendlyMatchCount != null)
        {
            int current = FriendlyMatchManager.Instance.CurrentApplyCount;
            int max = FriendlyMatchManager.Instance.MaxMonthlyCount;
            _txtFriendlyMatchCount.text = $"{current}";
        }
    }

    private void OnSearchValueChanged(string keyword)
    {
        PopulateSchools(keyword);
    }

    private void PopulateSchools(string filter)
    {
        foreach (var item in _spawnedItems) Destroy(item);
        _spawnedItems.Clear();

        var listTable = CachedSOData.Get<FriendlyMatchScheduleMsgListTableSO>();
        var nameTable = CachedSOData.Get<SchoolNameTableSO>();

        if (listTable == null || nameTable == null) return;

        foreach (var row in listTable.Rows)
        {
            string schoolId = row.schoolName;
            string realSchoolName = schoolId;

            var nameRow = nameTable.Rows.FirstOrDefault(r => r.id == schoolId);
            if (nameRow != null)
            {
                realSchoolName = nameRow.name;
            }

            if (!string.IsNullOrEmpty(filter) && !realSchoolName.Contains(filter))
                continue;

            Button btn = Instantiate(_btnSchoolPrefab, _contentRoot);
            btn.GetComponentInChildren<TMP_Text>().text = realSchoolName;
            btn.gameObject.SetActive(true);

            string sName = realSchoolName;
            string sId = schoolId;

            btn.onClick.AddListener(() => {
                string roomId = $"friendly_{sId}";
                var inbox = FindFirstObjectByType<MessengerInboxPopup>();
                if (inbox == null) return;

                bool hasHistory = false;
                bool isCompleted = false;
                if (MessengerManager.Instance != null)
                {
                    var room = MessengerManager.Instance.ActiveRooms.FirstOrDefault(r => r.RoomId == roomId);
                    if (room != null && room.Messages.Count > 0)
                    {
                        hasHistory = true;

                        if (room.Messages.Any(m => m.EventType == MessageEventType.System && m.Content.Contains("친선전 신청 횟수")))
                        {
                            isCompleted = true;
                        }
                    }
                }

                // 1. 이미 수락/거절이 끝난 방이면 횟수 차감이나 롤백 없이 그냥 대화 내역만 보여줌
                if (isCompleted)
                {
                    inbox.OpenRoom(roomId);
                    return;
                }

                // 2. 아직 안 끝났거나 새로운 매치 신청
                bool isSuccess = FriendlyMatchManager.Instance.StartFriendlyMatch(sId, sName);

                if (isSuccess)
                {
                    inbox.OpenRoom(roomId);
                    UpdateMatchCountUI(); 
                }
                else if (hasHistory)
                {
                    inbox.OpenRoom(roomId);
                }
                else
                {
                    Debug.Log("신청 횟수 소진으로 새로운 채팅방을 열 수 없습니다.");
                }
            });

            _spawnedItems.Add(btn.gameObject);
        }
    }
}