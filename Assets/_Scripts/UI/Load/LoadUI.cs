using System.Collections.Generic;
using UnityEngine;

public class LoadUI : MonoBehaviour
{
    [SerializeField] private GameObject _loadPrefab;
    [SerializeField] private GameObject _viewLoadPanel;
    [SerializeField] private Transform _loadListpanel;
    [SerializeField] private CheckPanel _openPanel;
    [SerializeField] private DeletePanel _openDeletePanel;

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
        {
            if (child.GetComponent<LoadPrefab>() != null)
                Destroy(child.gameObject);
        }

        for (int i = 1; i <= 4; i++)
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
        _openPanel.gameObject.SetActive(true);
        _openPanel.Open(slotIndex, data.playTime, this);
    }

    public void OpenDeletePanel(int slotIndex)
    {
        PlayData data = SaveSystem.Instance.Load(slotIndex);
        if (data == null)
        {
            return;
        }
        if (_openDeletePanel == null)
        {
            return;
        }
        _openDeletePanel.Open(slotIndex, data.playTime, this);
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

        if (SaveManager.Instance != null && SaveManager.Instance.CurrentSlotIndex == slotIndex)
        {
            SaveManager.Instance.Clear();
        }
    }
}