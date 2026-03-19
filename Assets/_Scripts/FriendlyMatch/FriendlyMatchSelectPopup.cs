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

    // 상단에 표시될 남은 횟수 텍스트
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
        base.Open(); // 뒤로가기 지원
        if (UIManager.Instance != null) UIManager.Instance.PushMessenger(this);
        transform.SetAsLastSibling(); // 다른 UI에 가려지지 않게 맨 앞으로 땡겨오기

        if (_inputSearch != null) _inputSearch.text = "";
        UpdateMatchCountUI();
        PopulateSchools("");
    }
    public override void Close()
    {
        if (UIManager.Instance != null) UIManager.Instance.PopMessenger(this);

        base.Close();
    }
    private void UpdateMatchCountUI()
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
                // 1. 매니저에게 방 생성 명령
                bool isSuccess = FriendlyMatchManager.Instance.StartFriendlyMatch(sId, sName);

                if (isSuccess)
                {
                    var inbox = FindFirstObjectByType<MessengerInboxPopup>();
                    if (inbox != null)
                    {
                        inbox.OpenRoom($"friendly_{sId}");
                    }
                }
                else
                {
                    // 필요하다면 여기에 "이번 달 신청 횟수를 모두 소진했습니다." 안내 팝업을 띄워도 좋습니다.
                    Debug.Log("신청 횟수 소진 등의 이유로 채팅방을 열지 않습니다.");
                }
            });

            _spawnedItems.Add(btn.gameObject);
        }
    }
}