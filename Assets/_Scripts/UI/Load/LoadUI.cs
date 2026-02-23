using System.Collections.Generic;
using UnityEngine;

public class LoadUI : MonoBehaviour
{
    [SerializeField] private GameObject _loadPrefab;
    [SerializeField] private GameObject _viewLoadPanel;
    [SerializeField] private Transform _loadListpanel;
    [SerializeField] private CheckPanel _openPanel;

    // 테스트용 데이터 나중에 세이브 생기면 삭제 예정
    // 나중에 바꿀 때 TestSaveSlotViewData 검색 후 변경 필요 지금은 LoadPrefab에서 사용
    private List<PlayData> _dummy = new()
    {
        new PlayData{ slotIndex=0, school="A", playTime="1:20", saveTime="2026-02-13" },
        new PlayData{ slotIndex=1, school="B", playTime="2:10", saveTime="2026-02-12" },
        new PlayData{ slotIndex=2, school="C", playTime="0:40", saveTime="2026-02-11" }
    };

    void OnEnable()
    {
        Debug.Log("LoadUI OnEnable 실행");
        LoadList();
        if (SaveSystem.Instance != null)
        {
            SaveSystem.Instance.OnSaveListChanged += LoadList;
        }
    }
    void OnDisable()
    {
        if (SaveSystem.Instance != null)
        {
            SaveSystem.Instance.OnSaveListChanged -= LoadList;
        }
    }

    public void LoadList()
    {
        foreach (Transform child in _loadListpanel)
            Destroy(child.gameObject);

        for (int i = 0; i < 3; i++)
        {
            if (!SaveSystem.Instance.Exists(i))
            {
                continue;
            }


            PlayData data = SaveSystem.Instance.Load(i);

            if (data == null)
            {
                continue;
            }

            PlayData viewData = new PlayData
            {
                slotIndex = data.slotIndex,
                school = data.school,
                playTime = data.playTime,
                saveTime = data.saveTime
            };

            var go = Instantiate(_loadPrefab, _loadListpanel);
            var slot = go.GetComponent<LoadPrefab>();
            slot.Initialize(viewData, this);
        }
    }

    public void TitleSceneLoad()
    {
        _viewLoadPanel.SetActive(false);
    }

    public void OpenConfirmPanel(int slotIndex)
    {
        PlayData data = SaveSystem.Instance.Load(slotIndex);
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
        SaveManager.Instance.LoadSlot(slotIndex, "Lobby");
    }

    public void OnClickDelete(int slotIndex)
    {
        Debug.Log($"삭제 요청: {slotIndex}");
        SaveSystem.Instance.Delete(slotIndex);
        LoadList();
    }
}