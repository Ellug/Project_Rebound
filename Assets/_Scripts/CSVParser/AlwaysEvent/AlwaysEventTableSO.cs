using System;
using System.Collections.Generic;
using UnityEngine;

// CSV 한 행(상시 이벤트 1개)에 대응
[Serializable]
public sealed class AlwaysEventRow
{
    public string id;
    public string name;
    public string type;

    public string termStart;
    public string termEnd;
    public int term;

    public int priority;
    public int range;

    public string effectId;
    public string description;
}

// 상시 이벤트 데이터테이블 SO
[CreateAssetMenu(menuName = "Game/Data/Always Event Table", fileName = "SO_AlwaysEventTable")]
public sealed class AlwaysEventTableSO : ScriptableObject
{
    [SerializeField] private List<AlwaysEventRow> _rows = new();

    private Dictionary<string, AlwaysEventRow> _byId;

    public IReadOnlyList<AlwaysEventRow> Rows => _rows;

    private void OnEnable()
    {
        BuildCache();
    }

    public void BuildCache()
    {
        _byId = new Dictionary<string, AlwaysEventRow>(_rows.Count, StringComparer.Ordinal);

        for (int i = 0; i < _rows.Count; i++)
        {
            var r = _rows[i];
            if (r == null) continue;
            if (string.IsNullOrEmpty(r.id)) continue;

            _byId[r.id] = r;
        }
    }

    public bool TryGet(string id, out AlwaysEventRow row)
        => _byId.TryGetValue(id, out row);

#if UNITY_EDITOR
    public void ReplaceAll(List<AlwaysEventRow> newRows)
    {
        _rows = newRows ?? new List<AlwaysEventRow>();
        BuildCache();
    }
#endif
}
