using System.IO;
using System;
using UnityEngine;

public class SaveSystem : Singleton<SaveSystem>
{
    public event Action OnSaveListChanged;

    private const int MIN_SLOT_INDEX = 1;
    private const int MAX_SLOT_INDEX = 4;

    private string GetPath(int slotIndex)
    {
        return Path.Combine(Application.persistentDataPath, $"save_{slotIndex}.json");
    }

    private string GetUserDataPath()
    {
        return Path.Combine(Application.persistentDataPath, "user_data.json"); // 영구 유저 데이터 경로
    }

    public void Save(PlayData data)
    {
        try
        {
            string path = GetPath(data.slotIndex);
            string json = JsonUtility.ToJson(data, true);
            File.WriteAllText(path, json);

            // 저장 후 파일 존재 여부 검증
            if (!File.Exists(path))
            {
                Debug.LogError($"[SaveSystem] 저장 실패 - 파일이 존재하지 않음: {path}");
                return;
            }
#if UNITY_EDITOR
            Debug.Log($"[SaveSystem] 저장 완료 | slot={data.slotIndex} | path={path} | size={new FileInfo(path).Length}bytes");
#endif
            OnSaveListChanged?.Invoke();
        }
        catch (Exception e)
        {
            Debug.LogError($"[SaveSystem] 저장 중 예외 발생: {e.Message}");
        }
    }

    public PlayData Load(int slotIndex)
    {
        string path = GetPath(slotIndex);

        if (!File.Exists(path))
        {
            Debug.LogWarning("세이브 파일 없음");
            return null;
        }

        string json = File.ReadAllText(path);
        return JsonUtility.FromJson<PlayData>(json);
    }

    public void Delete(int slotIndex)
    {
        string path = GetPath(slotIndex);

        if (File.Exists(path))
        {
            File.Delete(path);
            OnSaveListChanged?.Invoke();
        }
    }

    public bool Exists(int slotIndex)
    {
        return File.Exists(GetPath(slotIndex));
    }

    public void SaveUserData(UserData data)
    {
        if (data == null)
        {
            Debug.LogWarning("[SaveSystem] 저장할 유저 데이터가 없음");
            return;
        }

        string json = JsonUtility.ToJson(data, true);
        File.WriteAllText(GetUserDataPath(), json);
    }

    public UserData LoadUserData()
    {
        string path = GetUserDataPath();

        if (!File.Exists(path))
        {
            return null;
        }

        string json = File.ReadAllText(path);
        return JsonUtility.FromJson<UserData>(json);
    }

    public int FindFirstEmptySlotIndex()
    {
        for (int slotIndex = MIN_SLOT_INDEX; slotIndex <= MAX_SLOT_INDEX; slotIndex++)
        {
            bool exists = Exists(slotIndex);
            Debug.Log($"[SaveSystem] slotIndex={slotIndex}, exists={exists}, path={GetPath(slotIndex)}");

            if (!exists)
                return slotIndex;
        }

        return -1;
    }

    public int GetTotalSlotCount()
    {
        return MAX_SLOT_INDEX;
    }
}