using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;
using System.Linq;
using System;

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

        TurnManager tm = FindFirstObjectByType<TurnManager>();
        int currentYear = -1;
        int currentMonth = -1;
        if (tm != null && tm.DateManager != null)
        {
            currentYear = tm.DateManager.CurrentDate.Year;
            currentMonth = tm.DateManager.CurrentDate.Month;
        }

        // 중복 출력을 막기 위한 HashSet 및 이름 매핑용 Dictionary
        HashSet<string> allAvailableSchoolIds = new HashSet<string>();
        Dictionary<string, string> idToNameMap = new Dictionary<string, string>();

        // 1. 기존 테이블에 세팅된 기본 학교들 병합
        if (listTable != null)
        {
            foreach (var row in listTable.Rows)
            {
                string sId = row.schoolName;
                string sName = sId;

                var nameRow = nameTable?.Rows.FirstOrDefault(r => r.id == sId);
                if (nameRow != null) sName = nameRow.name;

                allAvailableSchoolIds.Add(sId);
                idToNameMap[sId] = sName;
            }
        }

        // 2. 토너먼트에서 만나 해금된 학교들 병합
        if (FriendlyMatchManager.Instance != null)
        {
            foreach (string unlockedName in FriendlyMatchManager.Instance.GetUnlockedSchools())
            {
                // 이름 기반으로 SchoolNameTable에서 ID를 역추적
                var nameRow = nameTable?.Rows.FirstOrDefault(r => r.name == unlockedName);

                // 만약 테이블에 없는 예외적인 더미 이름이라면, 이름 자체를 임시 ID로 사용
                string sId = nameRow != null ? nameRow.id : unlockedName;
                string sName = unlockedName;

                if (!allAvailableSchoolIds.Contains(sId))
                {
                    allAvailableSchoolIds.Add(sId);
                    idToNameMap[sId] = sName;
                }
            }
        }

        // 3. 통합된 리스트를 바탕으로 UI 버튼 생성
        foreach (string schoolId in allAvailableSchoolIds)
        {
            string realSchoolName = idToNameMap[schoolId];

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
                bool isCompletedThisMonth = false;

                if (MessengerManager.Instance != null)
                {
                    var room = MessengerManager.Instance.ActiveRooms.FirstOrDefault(r => r.RoomId == roomId);
                    if (room != null && room.Messages.Count > 0)
                    {
                        hasHistory = true;

                        if (room.Messages.Any(m => m.EventType == MessageEventType.System
                                                && m.Content.Contains("친선전 신청 횟수")
                                                && m.Timestamp.Year == currentYear
                                                && m.Timestamp.Month == currentMonth))
                        {
                            isCompletedThisMonth = true;
                        }
                    }
                }

                if (isCompletedThisMonth)
                {
                    inbox.OpenRoom(roomId);
                    return;
                }

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