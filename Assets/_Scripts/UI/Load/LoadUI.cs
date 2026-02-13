using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class LoadUI : MonoBehaviour
{
    [SerializeField] private GameObject _loadPrefab;
    [SerializeField] private Transform _loadListpanel;
    [SerializeField] private LoadConfirmPanel _openPanel;

    // 테스트용 데이터 나중에 세이브 생기면 삭제 예정
    // 나중에 바꿀 때 TestSaveSlotViewData 검색 후 변경 필요 지금은 LoadPrefab에서 사용
    private List<TestSaveSlotViewData> _dummy = new()
    {
        new TestSaveSlotViewData{ slotIndex=0, school="A", playTime="1:20", saveTime="2026-02-13" },
        new TestSaveSlotViewData{ slotIndex=1, school="B", playTime="2:10", saveTime="2026-02-12" },
        new TestSaveSlotViewData{ slotIndex=2, school="C", playTime="0:40", saveTime="2026-02-11" }
    };

    void OnEnable()
    {
        LoadList();
    }

    public void LoadList()
    {
        foreach (Transform child in _loadListpanel)
        {
            Destroy(child.gameObject);
        }
        foreach (var data in _dummy)
        {
            var go = Instantiate(_loadPrefab, _loadListpanel);
            var slot = go.GetComponent<LoadPrefab>();
            slot.Initialize(data, this);
        }
    }

    public void TitleSceneLoad()
    {
        SceneManager.LoadScene("Title");
    }

    public void OpenConfirmPanel(int slotIndex)
    {
        var data = _dummy.Find(x => x.slotIndex == slotIndex);
        if (data == null)
        {
            return;
        }
        if (_openPanel == null)
        {
            return;
        }
        _openPanel.Open(slotIndex, data.playTime, this);
    }

    public void OnClickLoad(int slotIndex)
    {
        Debug.Log($"로드 요청: {slotIndex}");
    }

    public void OnClickDelete(int slotIndex)
    {
        Debug.Log($"삭제 요청: {slotIndex}");
    }
}