using System;
using System.Collections.Generic;
using UnityEngine;

// CSV 한 행(포지션별 잠재력 정보 1개)에 대응
[Serializable]
public sealed class StudentPotentialRow
{
    public int positionId;
    public string positionName;
    public string tier1Stat;
    public int tier1Prob;
    public string tier2Stat;
    public int tier2Prob;
    public string tier3Stat;
    public int tier3Prob;
}

// 학생 잠재력 데이터테이블 SO
[CreateAssetMenu(menuName = "Game/Data/Student Potential Table", fileName = "SO_StudentPotentialTable")]
public sealed class StudentPotentialTableSO : ScriptableObject
{
    [SerializeField] private List<StudentPotentialRow> _rows = new();

    private Dictionary<int, StudentPotentialRow> _byPositionId;

    public IReadOnlyList<StudentPotentialRow> Rows => _rows;

    void OnEnable()
    {
        BuildCache();
    }

    public void BuildCache()
    {
        _byPositionId = new Dictionary<int, StudentPotentialRow>(_rows.Count);

        foreach (var r in _rows)
        {
            if (r == null) continue;
            _byPositionId[r.positionId] = r;
        }
    }

    public bool TryGet(int positionId, out StudentPotentialRow row)
        => _byPositionId.TryGetValue(positionId, out row);

    public StudentPotentialRow GetOrNull(int positionId)
        => _byPositionId.TryGetValue(positionId, out var r) ? r : null;

#if UNITY_EDITOR
    public void ReplaceAll(List<StudentPotentialRow> newRows)
    {
        _rows = newRows ?? new List<StudentPotentialRow>();
        BuildCache();
    }
#endif
}
