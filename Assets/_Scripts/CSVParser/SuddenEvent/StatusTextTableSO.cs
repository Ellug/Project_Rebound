using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public sealed class StatusTextRow
{
    public string id;
    public int index;
    public string text;        // 표시명
    public string description;
}

[CreateAssetMenu(menuName = "Game/Data/StatusTextTable", fileName = "SO_StatusTextTable")]
public sealed class StatusTextTableSO : ScriptableObject
{
    [SerializeField] private List<StatusTextRow> _rows = new();

    // key = $"{id}:{index}"
    private Dictionary<string, StatusTextRow> _byKey;

    public IReadOnlyList<StatusTextRow> Rows => _rows;

    private void OnEnable()
    {
        BuildCache();
    }

    public void BuildCache()
    {
        _byKey = new Dictionary<string, StatusTextRow>(_rows.Count, StringComparer.Ordinal);

        for (int i = 0; i < _rows.Count; i++)
        {
            var r = _rows[i];
            if (r == null) continue;
            if (string.IsNullOrEmpty(r.id)) continue;

            _byKey[MakeKey(r.id, r.index)] = r;
        }
    }

    public bool TryGet(string id, int index, out StatusTextRow row)
        => _byKey.TryGetValue(MakeKey(id, index), out row);

    private static string MakeKey(string id, int index)
        => $"{id}:{index}";

#if UNITY_EDITOR
    public void ReplaceAll(List<StatusTextRow> newRows)
    {
        _rows = newRows ?? new List<StatusTextRow>();
        BuildCache();
    }
#endif
}
