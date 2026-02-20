using UnityEngine;
using UnityEngine.SceneManagement;

public class SaveManager : Singleton<SaveManager>
{
    public PlayData CurrentData { get; private set; }

    public void LoadSlot(int slotIndex, string sceneName)
    {
        PlayData data = SaveSystem.Instance.Load(slotIndex);

        if (data == null)
        {
            Debug.LogWarning("로드 실패");
            return;
        }

        CurrentData = data;
        SceneManager.LoadScene(sceneName);
    }

    public void SetLoadedData(PlayData data)
    {
        CurrentData = data;
    }

    public void SaveCurrent()
    {
        if (CurrentData == null)
        {
            Debug.LogWarning("저장할 데이터 없음");
            return;
        }

        SaveSystem.Instance.Save(CurrentData);
    }

    public void DeleteSlot(int slotIndex)
    {
        SaveSystem.Instance.Delete(slotIndex);
    }

    public void Clear()
    {
        CurrentData = null;
    }
}