using System;
using System.Collections.Generic;
using UnityEngine;

// CSV 한 행(학생 이름 1개)에 대응
[Serializable]
public sealed class StudentNameRow
{
    public int id;
    public string name;
}

// 학생 이름 데이터테이블 SO
[CreateAssetMenu(menuName = "Game/Data/Student Name Table", fileName = "SO_StudentNameTable")]
public sealed class StudentNameTableSO : ScriptableObject
{
    [SerializeField] private List<StudentNameRow> _rows = new();

    private Dictionary<int, StudentNameRow> _byId;

    public IReadOnlyList<StudentNameRow> Rows => _rows;

    void OnEnable()
    {
        BuildCache();
    }

    public void BuildCache()
    {
        _byId = new Dictionary<int, StudentNameRow>(_rows.Count);

        foreach (var r in _rows)
        {
            if (r == null) continue;
            _byId[r.id] = r;
        }
    }

    public bool TryGet(int id, out StudentNameRow row)
        => _byId.TryGetValue(id, out row);

    public StudentNameRow GetOrNull(int id)
        => _byId.TryGetValue(id, out var r) ? r : null;

#if UNITY_EDITOR
    public void ReplaceAll(List<StudentNameRow> newRows)
    {
        _rows = newRows ?? new List<StudentNameRow>();
        BuildCache();
    }
#endif
}
