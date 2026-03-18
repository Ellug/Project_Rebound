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
        transform.SetAsLastSibling(); // 다른 UI에 가려지지 않게 맨 앞으로 땡겨오기

        if (_inputSearch != null) _inputSearch.text = "";
        UpdateMatchCountUI();
        PopulateSchools("");
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
                FriendlyMatchManager.Instance.StartFriendlyMatch(sId, sName);

                // 2. 현재 열려있는 목록 창 닫기
                Close();

                // 3. 인박스 목록을 갱신할 필요 없이, 즉시 해당 채팅방으로 진입
                var inbox = FindFirstObjectByType<MessengerInboxPopup>();
                if (inbox != null)
                {
                    inbox.OpenRoom($"friendly_{sId}");
                }
            });

            _spawnedItems.Add(btn.gameObject);
        }
    }
}