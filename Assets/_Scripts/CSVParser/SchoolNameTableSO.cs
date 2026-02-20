using System;
using System.Collections.Generic;
using UnityEngine;

// CSV 한 행(학교 이름 1개)에 대응
[Serializable]
public sealed class SchoolNameRow
{
    public int id;
    public string name;
}

// 학교 이름 데이터테이블 SO
[CreateAssetMenu(menuName = "Game/Data/School Name Table", fileName = "SO_SchoolNameTable")]
public sealed class SchoolNameTableSO : ScriptableObject
{
    [SerializeField] private List<SchoolNameRow> _rows = new();

    private Dictionary<int, SchoolNameRow> _byId;

    public IReadOnlyList<SchoolNameRow> Rows => _rows;

    private void OnEnable()
    {
        BuildCache();
    }

    public void BuildCache()
    {
        _byId = new Dictionary<int, SchoolNameRow>(_rows.Count);

        for (int i = 0; i < _rows.Count; i++)
        {
            var r = _rows[i];
            if (r == null) continue;

            _byId[r.id] = r;
        }
    }

    public bool TryGet(int id, out SchoolNameRow row)
        => _byId.TryGetValue(id, out row);

    public SchoolNameRow GetOrNull(int id)
        => _byId.TryGetValue(id, out var r) ? r : null;

#if UNITY_EDITOR
    public void ReplaceAll(List<SchoolNameRow> newRows)
    {
        _rows = newRows ?? new List<SchoolNameRow>();
        BuildCache();
    }
#endif
}
