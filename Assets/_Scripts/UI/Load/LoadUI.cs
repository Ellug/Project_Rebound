using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class LoadUI : MonoBehaviour
{
    [SerializeField] private GameObject _loadPrefab;
    [SerializeField] private Transform _loadListpanel;

    // 나중에 세이브 생기면 그때 교체 예정
    private List<int> _dummy = new() { 0, 1, 2 };

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
    }

    public void TitleSceneLoad()
    {
        SceneManager.LoadScene("Title");
    }

    public void OnClickLoad(int slotIndex)
    {
        Debug.Log($"로드 요청: {slotIndex}");
    }

    public void OnClickDelete(int slotIndex)
    {
        Debug.Log($"삭제 요청: {slotIndex}");
    }

    public void OnSelect(int slotIndex)
    {
        Debug.Log($"선택: {slotIndex}");
    }
}