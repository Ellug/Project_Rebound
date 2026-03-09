using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public sealed class SuddenEventTextRow
{
    public string id;     // 예: ID_Text_SuddenEvent_000001
    public SuddenEventConditionFlags condition;
    public int target;
    public string speaker;
    public string description;
}

[CreateAssetMenu(menuName = "Game/Data/SuddenEventTextTable", fileName = "SO_SuddenEventTextTable")]
public sealed class SuddenEventTextTableSO : ScriptableObject
{
    [SerializeField] private List<SuddenEventTextRow> _rows = new();

    // key = $"{id}:{target}"
    private Dictionary<string, SuddenEventTextRow> _byKey;

    public IReadOnlyList<SuddenEventTextRow> Rows => _rows;

    private void OnEnable()
    {
        BuildCache();
    }

    public void BuildCache()
    {
        _byKey = new Dictionary<string, SuddenEventTextRow>(_rows.Count, StringComparer.Ordinal);

        for (int i = 0; i < _rows.Count; i++)
        {
            var r = _rows[i];
            if (r == null) continue;
            if (string.IsNullOrEmpty(r.id)) continue;

            var key = MakeKey(r.id, r.target);
            _byKey[key] = r; // 중복이면 마지막 값으로 덮어씀
        }
    }

    public bool TryGet(string id, int target, out SuddenEventTextRow row)
        => _byKey.TryGetValue(MakeKey(id, target), out row);

    private static string MakeKey(string id, int target)
        => $"{id}:{target}";

#if UNITY_EDITOR
    public void ReplaceAll(List<SuddenEventTextRow> newRows)
    {
        _rows = newRows ?? new List<SuddenEventTextRow>();
        BuildCache();
    }
#endif
}
