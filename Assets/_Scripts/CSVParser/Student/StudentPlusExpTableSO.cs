using System;
using System.Collections.Generic;
using UnityEngine;

// CSV 한 행(포지션별 추가 경험치 정보 1개)에 대응
[Serializable]
public sealed class StudentPlusExpRow
{
    public string id;
    public string positionName;
    public int tier;
    public int minValue;
    public int maxValue;
}

// 학생 추가 경험치 데이터테이블 SO
[CreateAssetMenu(menuName = "Game/Data/Student Plus Exp Table", fileName = "SO_StudentPlusExpTable")]
public sealed class StudentPlusExpTableSO : ScriptableObject
{
    [SerializeField] private List<StudentPlusExpRow> _rows = new();

    private Dictionary<(string id, int tier), StudentPlusExpRow> _byPositionIdAndTier;
    private Dictionary<string, List<StudentPlusExpRow>> _byPositionId;

    public IReadOnlyList<StudentPlusExpRow> Rows => _rows;

    void OnEnable()
    {
        BuildCache();
    }

    public void BuildCache()
    {
        _byPositionIdAndTier = new Dictionary<(string, int), StudentPlusExpRow>();
        _byPositionId = new Dictionary<string, List<StudentPlusExpRow>>();

        foreach (var r in _rows)
        {
            if (r == null) continue;

            _byPositionIdAndTier[(r.id, r.tier)] = r;

            if (!_byPositionId.TryGetValue(r.id, out var list))
            {
                list = new List<StudentPlusExpRow>();
                _byPositionId[r.id] = list;
            }
            list.Add(r);
        }
    }

    public bool TryGet(string id, int tier, out StudentPlusExpRow row)
        => _byPositionIdAndTier.TryGetValue((id, tier), out row);

    public StudentPlusExpRow GetOrNull(string id, int tier)
        => _byPositionIdAndTier.TryGetValue((id, tier), out var r) ? r : null;

    public IReadOnlyList<StudentPlusExpRow> GetByPositionId(string id)
        => _byPositionId.TryGetValue(id, out var list) ? list : Array.Empty<StudentPlusExpRow>();

#if UNITY_EDITOR
    public void ReplaceAll(List<StudentPlusExpRow> newRows)
    {
        _rows = newRows ?? new List<StudentPlusExpRow>();
        BuildCache();
    }
#endif
}
