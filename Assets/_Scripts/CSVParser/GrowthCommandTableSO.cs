using System;
using System.Collections.Generic;
using UnityEngine;

public enum GrowthCommandBtnType
{
    Category,
    Action
}

public enum GrowthFacilityReq
{
    None = 0,
    School,
    Gym,
    Cafeteria,
    CounselingCenter
}

public enum GrowthCommandTarget
{
    Etc = 0,
    Team,
    Individual
}

// CSV 한 행(커맨드 1개)에 대응
[Serializable]
public sealed class GrowthCommandRow
{
    public int index;

    public string name;
    public string icon;

    public int parentIndex;
    public GrowthCommandBtnType btnType;

    public GrowthFacilityReq facilityReq;
    public int facilityLv = 1;

    public GrowthCommandTarget target;

    public int hpCost;
    public int mental;

    public float shoot;
    public float speed;
    public float defense;
    public float stamina;

    // 문서상 enum 이지만 전부 -로 돼있어서... 일단 "-" 또는 빈값이면 0.
    public int linkedEventId;
}

// 육성 커맨드 데이터테이블 SO
[CreateAssetMenu(menuName = "Game/Data/Growth Command Table", fileName = "SO_GrowthCommandTable")]
public sealed class GrowthCommandTableSO : ScriptableObject
{
    [SerializeField] private List<GrowthCommandRow> _rows = new();

    private Dictionary<int, GrowthCommandRow> _byIndex;
    private Dictionary<int, List<GrowthCommandRow>> _children;

    public IReadOnlyList<GrowthCommandRow> Rows => _rows;

    private void OnEnable()
    {
        BuildCache();
    }

    public void BuildCache()
    {
        _byIndex = new Dictionary<int, GrowthCommandRow>(_rows.Count);
        _children = new Dictionary<int, List<GrowthCommandRow>>(64);

        foreach (var r in _rows)
        {
            if (r == null) continue;

            // 중복 index는 마지막 값으로 덮어씀
            _byIndex[r.index] = r;

            if (!_children.TryGetValue(r.parentIndex, out var list))
            {
                list = new List<GrowthCommandRow>(8);
                _children.Add(r.parentIndex, list);
            }
            list.Add(r);
        }
    }

    public bool TryGet(int index, out GrowthCommandRow row)
    {
        if (_byIndex != null && _byIndex.TryGetValue(index, out row))
            return true;

        row = null;
        return false;
    }

    public GrowthCommandRow GetOrNull(int index)
        => (_byIndex != null && _byIndex.TryGetValue(index, out var r)) ? r : null;

    public IReadOnlyList<GrowthCommandRow> GetChildren(int parentIndex)
        => (_children != null && _children.TryGetValue(parentIndex, out var list)) ? list : Array.Empty<GrowthCommandRow>();

#if UNITY_EDITOR
    // 에디터 Importer가 갱신할 때 사용
    public void ReplaceAll(List<GrowthCommandRow> newRows)
    {
        _rows = newRows ?? new List<GrowthCommandRow>();
        BuildCache();
    }
#endif
}
