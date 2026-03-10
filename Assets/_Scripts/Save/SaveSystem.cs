using System.IO;
using System;
using UnityEngine;

public class SaveSystem : Singleton<SaveSystem>
{
    public event Action OnSaveListChanged;

    private string GetPath(int slotIndex)
    {
        return Path.Combine(Application.persistentDataPath, $"save_{slotIndex}.json");
    }

    public void Save(PlayData data)
    {
        string json = JsonUtility.ToJson(data, true);
        File.WriteAllText(GetPath(data.slotIndex), json);

        OnSaveListChanged?.Invoke();
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
}