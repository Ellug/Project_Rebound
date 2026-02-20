using System;
using System.Collections.Generic;
using UnityEngine;

public enum SuddenEventEffectType
{
    None = 0,
    Fixed = 1,
    Percent = 2
}

public enum PlayerStat
{
    None = 0,
    Mental = 1,
    Shoot = 2,
    Speed = 3,
    Jump = 4,
    Vital = 5,
    Condition = 6,
    Money = 10,
    Fame = 11
}

[Serializable]
public sealed class SuddenEventEffectRow
{
    public string id;

    // CSV: 0/1 또는 Fixed/PerCent 같은 문자열 혼재 가능
    public SuddenEventEffectType type;

    // CSV: target_name
    public string targetName;

    public PlayerStat targetMin;
    public PlayerStat targetMax;

    public int amountMin;
    public int amountMax;
}

[CreateAssetMenu(menuName = "Game/Data/SuddenEventEffectTable", fileName = "SO_SuddenEventEffectTable")]
public sealed class SuddenEventEffectTableSO : ScriptableObject
{
    [SerializeField] private List<SuddenEventEffectRow> _rows = new();

    private Dictionary<string, SuddenEventEffectRow> _byId;

    public IReadOnlyList<SuddenEventEffectRow> Rows => _rows;

    void OnEnable()
    {
        BuildCache();
    }

    public void BuildCache()
    {
        _byId = new Dictionary<string, SuddenEventEffectRow>(_rows.Count, StringComparer.Ordinal);

        for (int i = 0; i < _rows.Count; i++)
        {
            var r = _rows[i];
            if (r == null) continue;
            if (string.IsNullOrEmpty(r.id)) continue;

            _byId[r.id] = r;
        }
    }

    public bool TryGet(string id, out SuddenEventEffectRow row)
        => _byId.TryGetValue(id, out row);

#if UNITY_EDITOR
    public void ReplaceAll(List<SuddenEventEffectRow> newRows)
    {
        _rows = newRows ?? new List<SuddenEventEffectRow>();
        BuildCache();
    }
#endif
}
