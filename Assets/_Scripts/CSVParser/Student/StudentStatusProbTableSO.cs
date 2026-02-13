using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public sealed class StudentStatusProbRow
{
    public bool isInsane;
    public int conditionMax;
    public int probInsanity;
    public int probInjury;
    public int probDisease;
    public int totalRisk;
}

// 학생 상태이상 확률 데이터테이블 SO
[CreateAssetMenu(menuName = "Game/Data/Student Status Prob Table", fileName = "SO_StudentStatusProbTable")]
public sealed class StudentStatusProbTableSO : ScriptableObject
{
    [SerializeField] private List<StudentStatusProbRow> _rows = new();

    private List<StudentStatusProbRow> _saneRows;
    private List<StudentStatusProbRow> _insaneRows;

    public IReadOnlyList<StudentStatusProbRow> Rows => _rows;

    void OnEnable()
    {
        BuildCache();
    }

    public void BuildCache()
    {
        _saneRows = new List<StudentStatusProbRow>();
        _insaneRows = new List<StudentStatusProbRow>();

        foreach (var r in _rows)
        {
            if (r == null) continue;

            if (r.isInsane)
                _insaneRows.Add(r);
            else
                _saneRows.Add(r);
        }

        // conditionMax 기준으로 오름차순 정렬 (최적화)
        _saneRows.Sort((a, b) => a.conditionMax.CompareTo(b.conditionMax));
        _insaneRows.Sort((a, b) => a.conditionMax.CompareTo(b.conditionMax));
    }

    public StudentStatusProbRow GetByCondition(bool isInsane, int conditionValue)
    {
        var targetList = isInsane ? _insaneRows : _saneRows;

        foreach (var r in targetList)
        {
            if (conditionValue <= r.conditionMax)
                return r;
        }
        return null;
    }

#if UNITY_EDITOR
    public void ReplaceAll(List<StudentStatusProbRow> newRows)
    {
        _rows = newRows ?? new List<StudentStatusProbRow>();
        BuildCache();
    }
#endif
}
