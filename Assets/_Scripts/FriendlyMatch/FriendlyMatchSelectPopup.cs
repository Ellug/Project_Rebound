using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;
using System.Linq; // FirstOrDefault를 위해 추가

public class FriendlyMatchSelectPopup : UIBase
{
    [SerializeField] private TMP_InputField _inputSearch;
    [SerializeField] private Transform _contentRoot;
    [SerializeField] private Button _btnSchoolPrefab;
    [SerializeField] private Button _btnClose;

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
        if (_inputSearch != null) _inputSearch.text = "";
        PopulateSchools("");
    }

    private void OnSearchValueChanged(string keyword)
    {
        PopulateSchools(keyword);
    }

    private void PopulateSchools(string filter)
    {
        foreach (var item in _spawnedItems) Destroy(item);
        _spawnedItems.Clear();

        // 1. 친선전 목록 테이블과 학교 이름 테이블을 둘 다 불러옴
        var listTable = CachedSOData.Get<FriendlyMatchScheduleMsgListTableSO>();
        var nameTable = CachedSOData.Get<SchoolNameTableSO>();

        if (listTable == null || nameTable == null) return;

        foreach (var row in listTable.Rows)
        {
            // row.schoolName 에는 "school_001" 같은 ID가 들어음
            string schoolId = row.schoolName;
            string realSchoolName = schoolId; // 기본값

            // 2. SchoolNameTable에서 school_001에 해당하는 진짜 이름을 찾고
            var nameRow = nameTable.Rows.FirstOrDefault(r => r.id == schoolId);
            if (nameRow != null)
            {
                realSchoolName = nameRow.name;
            }

            // 3. ID가 아닌 "진짜 이름"을 기준으로 검색어를 필터링
            if (!string.IsNullOrEmpty(filter) && !realSchoolName.Contains(filter))
                continue;

            Button btn = Instantiate(_btnSchoolPrefab, _contentRoot);

            // 화면 버튼에는 진짜 한국어 이름 입력
            btn.GetComponentInChildren<TMP_Text>().text = realSchoolName;
            btn.gameObject.SetActive(true);

            string sName = realSchoolName;
            string sId = schoolId;

            btn.onClick.AddListener(() => {
                // 매니저에게 넘겨줄 때도 변환된 진짜 이름을 넘겨서 다이얼로그에 적용
                FriendlyMatchManager.Instance.StartFriendlyMatch(sId, sName);

                Close();

                MessengerManager.Instance.ReceiveMessage("temp", "temp", new ChatMessage(MessageSenderType.System, ""));

                var inbox = FindFirstObjectByType<MessengerInboxPopup>();
                if (inbox != null) inbox.RefreshFriendlyMatchUI();
            });

            _spawnedItems.Add(btn.gameObject);
        }
    }
}