using System;
using System.Collections.Generic;
using UnityEngine;

// CSV 한 행(스탯 정보 1개)에 대응
[Serializable]
public sealed class StudentStatRow
{
    public int statId;
    public string statName;
    public string roleDesc;
    public string logicEffect;
}

// 학생 스탯 데이터테이블 SO
[CreateAssetMenu(menuName = "Game/Data/Student Stat Table", fileName = "SO_StudentStatTable")]
public sealed class StudentStatTableSO : ScriptableObject
{
    [SerializeField] private List<StudentStatRow> _rows = new();

    private Dictionary<int, StudentStatRow> _byStatId;

    public IReadOnlyList<StudentStatRow> Rows => _rows;

    void OnEnable()
    {
        BuildCache();
    }

    public void BuildCache()
    {
        _byStatId = new Dictionary<int, StudentStatRow>(_rows.Count);

        foreach (var r in _rows)
        {
            if (r == null) continue;
            _byStatId[r.statId] = r;
        }
    }

    public bool TryGet(int statId, out StudentStatRow row)
        => _byStatId.TryGetValue(statId, out row);

    public StudentStatRow GetOrNull(int statId)
        => _byStatId.TryGetValue(statId, out var r) ? r : null;

#if UNITY_EDITOR
    public void ReplaceAll(List<StudentStatRow> newRows)
    {
        _rows = newRows ?? new List<StudentStatRow>();
        BuildCache();
    }
#endif
}
