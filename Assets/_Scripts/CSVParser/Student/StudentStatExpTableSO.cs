using System;
using System.Collections.Generic;
using UnityEngine;

// CSV 한 행(스탯 경험치 정보 1개)에 대응
[Serializable]
public sealed class StudentStatExpRow
{
    public int level;
    public int expNext;
    public int expTotal;
}

// 학생 스탯 경험치 데이터테이블 SO
[CreateAssetMenu(menuName = "Game/Data/Student Stat Exp Table", fileName = "SO_StudentStatExpTable")]
public sealed class StudentStatExpTableSO : ScriptableObject
{
    [SerializeField] private List<StudentStatExpRow> _rows = new();

    private Dictionary<int, StudentStatExpRow> _byLevel;

    public IReadOnlyList<StudentStatExpRow> Rows => _rows;

    void OnEnable()
    {
        BuildCache();
    }

    public void BuildCache()
    {
        _byLevel = new Dictionary<int, StudentStatExpRow>(_rows.Count);

        foreach (var r in _rows)
        {
            if (r == null) continue;
            _byLevel[r.level] = r;
        }
    }

    public bool TryGet(int level, out StudentStatExpRow row)
        => _byLevel.TryGetValue(level, out row);

    public StudentStatExpRow GetOrNull(int level)
        => _byLevel.TryGetValue(level, out var r) ? r : null;

#if UNITY_EDITOR
    public void ReplaceAll(List<StudentStatExpRow> newRows)
    {
        _rows = newRows ?? new List<StudentStatExpRow>();
        BuildCache();
    }
#endif
}
