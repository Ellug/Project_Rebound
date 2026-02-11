using System;
using System.Collections.Generic;
using UnityEngine;

[Flags]
public enum SuddenEventContextFlags
{
    None = 0,
    PreProcess = 1,
    PostProcess = 2,
    Both = PreProcess | PostProcess
}

[Flags]
public enum SuddenEventConditionFlags
{
    None = 0,
    Daily = 1,
    School = 2,
    Exercise = 4,
    Match = 8
}

public enum SuddenEventScope
{
    Non_Member = 0,
    Member = 1,
    Team_Member = 2,
    Team_Key_Member = 3,
    Team_Bench_Member = 4,
    Graduated_Member = 5,
    Facility = 6,
    Player = 7,
    Non_School = 8,
    Message = 9
}

public enum SuddenEventTermScale
{
    Day = 1,
    Quarter = 2
}

[Serializable]
public sealed class SuddenEventRow
{
    public string id;
    public string eventName;

    public SuddenEventContextFlags context;
    public SuddenEventConditionFlags condition;

    public SuddenEventScope scope;

    public int rangeMin;
    public int rangeMax;

    public int termMin;
    public int termMax;
    public SuddenEventTermScale termScale;

    public string effect1;
    public int amount1;

    public string effect2;
    public int amount2;

    public string effect3;
    public int amount3;

    public float probability; // 0~1
    public string messageId;  // SuddenEventTextTable 참조
}

[CreateAssetMenu(menuName = "Game/Data/SuddenEventTable", fileName = "SO_SuddenEventTable")]
public sealed class SuddenEventTableSO : ScriptableObject
{
    [SerializeField] private List<SuddenEventRow> _rows = new();

    private Dictionary<string, SuddenEventRow> _byId;

    public IReadOnlyList<SuddenEventRow> Rows => _rows;

    void OnEnable()
    {        
        BuildCache();
    }

    public void BuildCache()
    {
        _byId = new Dictionary<string, SuddenEventRow>(_rows.Count, StringComparer.Ordinal);
        foreach (var r in _rows)
        {
            if (r == null || string.IsNullOrEmpty(r.id)) continue;
            _byId[r.id] = r;
        }
    }

    public bool TryGet(string id, out SuddenEventRow row)
        => _byId.TryGetValue(id, out row);

#if UNITY_EDITOR
    public void ReplaceAll(List<SuddenEventRow> newRows)
    {
        _rows = newRows ?? new List<SuddenEventRow>();
        BuildCache();
    }
#endif
}
