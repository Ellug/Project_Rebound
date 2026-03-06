using System;
using System.Collections.Generic;
using UnityEngine;

// CSV 한 행(체격 정보 1개)에 대응
[Serializable]
public sealed class StudentBodyRow
{
    public string id;
    public string positionName;
    public int minHeight;
    public int maxHeight;
    public int minWeight;
    public int maxWeight;
}

// 학생 체격 데이터테이블 SO
[CreateAssetMenu(menuName = "Game/Data/Student Body Table", fileName = "SO_StudentBodyTable")]
public sealed class StudentBodyTableSO : ScriptableObject
{
    [SerializeField] private List<StudentBodyRow> _rows = new();

    private Dictionary<string, StudentBodyRow> _byId;

    public IReadOnlyList<StudentBodyRow> Rows => _rows;

    void OnEnable()
    {
        BuildCache();
    }

    public void BuildCache()
    {
        _byId = new Dictionary<string, StudentBodyRow>(_rows.Count);

        foreach (var r in _rows)
        {
            if (r == null) continue;
            _byId[r.id] = r;
        }
    }

    public bool TryGet(string id, out StudentBodyRow row)
        => _byId.TryGetValue(id, out row);

    public StudentBodyRow GetOrNull(string id)
        => _byId.TryGetValue(id, out var r) ? r : null;

#if UNITY_EDITOR
    public void ReplaceAll(List<StudentBodyRow> newRows)
    {
        _rows = newRows ?? new List<StudentBodyRow>();
        BuildCache();
    }
#endif
}
