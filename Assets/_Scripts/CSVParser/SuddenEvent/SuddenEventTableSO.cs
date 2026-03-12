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

[Flags]
public enum SuddenEventCategoryFlags
{
    None = 0,
    Positive = 1,
    Negative = 2,
    Neutral = 3,
    Body = 4,
    Mental = 8,
    Mind = 8,
}

public enum SuddenEventScope
{
    None = 0,
    NonMember = 1,
    Member = 2,
    TeamMember = 3,
    TeamKeyMember = 3,
    BenchMember = 4,
    TeamBenchMember = 4,
    Graduated_Member = 5,
    Facility = 6,
    Player = 7,
    NonSchool = 8,
    Message = 9
}

public enum SuddenEventTermScale
{
    None = 0,
    Day = 1,
    Quarter = 2
}

public enum SuddenEventTriggerStatus
{
    None = 0,
    Mental = 1,
    Shoot = 2,
    Speed = 3,
    Jump = 4,
    Condition = 5,
    Stamina = 6,
    Money = 10,
    Fame = 11,
    Trust = 12,
    Experience = 13,
    Unable = 21,
    UnableTraining = 22,
    UnableMatch = 23
}

// 스펙 값: none=0, not=1, equal=2, not_equal=3, more=4, or_less=5, or_more=6, less=7
// Flags 비트 조합 결과가 스펙 값과 일치함
[Flags]
public enum SuddenEventTriggerCondition
{
    None = 0,
    Not = 1,
    Equal = 2,
    NotEqual = Not | Equal,     // 3
    More = 4,
    OrLess = Not | More,        // 5
    OrMore = More | Equal,      // 6
    Less = Not | More | Equal   // 7
}

[Serializable]
public sealed class SuddenEventRow
{
    public string id;
    public string name;

    public SuddenEventContextFlags context;
    public SuddenEventConditionFlags condition;
    public SuddenEventCategoryFlags category;

    public SuddenEventScope scope = SuddenEventScope.Member;

    public int targetMin;
    public int targetMax;

    public int termMin = 1;
    public int termMax = 1;
    public SuddenEventTermScale termScale = SuddenEventTermScale.Day;

    public bool isTrigger;
    public SuddenEventTriggerStatus triggerStatus1;
    public SuddenEventTriggerCondition triggerCondition1;
    public int triggerThreshold1 = -1;
    public SuddenEventTriggerStatus triggerStatus2;
    public SuddenEventTriggerCondition triggerCondition2;
    public int triggerThreshold2 = -1;

    public string effect1;
    public string effect2;
    public string effect3;

    public bool isProbable;
    public float probability; // 0~1
    public string description; // SuddenEventTextTable 참조 ID
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
