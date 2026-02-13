using System;
using System.Collections.Generic;
using UnityEngine;

// CSV 한 행(포지션 정보 1개)에 대응
[Serializable]
public sealed class StudentPositionRow
{
    public int id;
    public string positionName;
    public int spawnRate;
    public string mainStats;
    public string subStats;
    public string designIntent;
}

// 학생 포지션 데이터테이블 SO
[CreateAssetMenu(menuName = "Game/Data/Student Position Table", fileName = "SO_StudentPositionTable")]
public sealed class StudentPositionTableSO : ScriptableObject
{
    [SerializeField] private List<StudentPositionRow> _rows = new();

    private Dictionary<int, StudentPositionRow> _byId;

    public IReadOnlyList<StudentPositionRow> Rows => _rows;

    void OnEnable()
    {
        BuildCache();
    }

    public void BuildCache()
    {
        _byId = new Dictionary<int, StudentPositionRow>(_rows.Count);

        foreach (var r in _rows)
        {
            if (r == null) continue;
            _byId[r.id] = r;
        }
    }

    public bool TryGet(int id, out StudentPositionRow row)
        => _byId.TryGetValue(id, out row);

    public StudentPositionRow GetOrNull(int id)
        => _byId.TryGetValue(id, out var r) ? r : null;

#if UNITY_EDITOR
    public void ReplaceAll(List<StudentPositionRow> newRows)
    {
        _rows = newRows ?? new List<StudentPositionRow>();
        BuildCache();
    }
#endif
}
