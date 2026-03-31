using System.Collections.Generic;
using UnityEngine;

public class LoadUI : MonoBehaviour
{
    [SerializeField] private GameObject _loadPrefab;
    [SerializeField] private GameObject _viewLoadPanel;
    [SerializeField] private Transform _loadListpanel;
    [SerializeField] private CheckPanel _openPanel;
    [SerializeField] private DeletePanel _openDeletePanel;

    // _viewLoadPanel 오브젝트에 붙어있는 PopupAnimator
    [Header("애니메이션")]
    [SerializeField] private PopupAnimator _animator;

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
                continue;

            PlayData data = SaveSystem.Instance.Load(i);
            if (data == null)
                continue;

            PlayData viewData = new PlayData
            {
                slotIndex = data.slotIndex,
                school = data.school,
                playTime = data.playTime,
                saveTime = data.saveTime,
                isEndingReached = data.isEndingReached
            };

            var go = Instantiate(_loadPrefab, _loadListpanel);
            var slot = go.GetComponent<LoadPrefab>();
            slot.Initialize(viewData, this);
        }
    }

    // TitleManager 참조 — 닫기 시 TitleManager.CloseViewLoadPanel()으로 위임
    [SerializeField] private TitleManager _titleManager;

    public void TitleSceneLoad()
    {
        if (_titleManager != null)
        {
            _titleManager.CloseViewLoadPanel();
            return;
        }

        // TitleManager 미연결 시 자체 처리
        if (_animator == null)
        {
            _viewLoadPanel.SetActive(false);
            return;
        }

        _animator.PlayOut(() => _viewLoadPanel.SetActive(false));
    }

    // 이어하기 버튼 클릭 시 View_Load 팝업 열기
    public void OpenViewLoadPanel()
    {
        if (_animator == null)
        {
            Debug.LogWarning("[LoadUI] _animator가 연결되지 않았습니다. 인스펙터에서 PopupAnimator를 연결해주세요.");
            _viewLoadPanel.SetActive(true);
            return;
        }

        _animator.Initialize();
        _viewLoadPanel.SetActive(true);
        _animator.PlayIn();
    }

    public void OpenConfirmPanel(int slotIndex)
    {
        PlayData data = SaveSystem.Instance.Load(slotIndex);
        if (data == null) return;
        if (_openPanel == null) return;

        _openPanel.gameObject.SetActive(true);
        _openPanel.Open(slotIndex, data.playTime, this);
    }

    public void OpenDeletePanel(int slotIndex)
    {
        PlayData data = SaveSystem.Instance.Load(slotIndex);
        if (data == null) return;
        if (_openDeletePanel == null) return;

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
            SaveManager.Instance.Clear();
    }
}