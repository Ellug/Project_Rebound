using System;
using System.Collections.Generic;
using UnityEngine;

// CSV 한 행(시작 스탯 정보 1개)에 대응
[Serializable]
public sealed class StudentStartStatRow
{
    public string statId;
    public string statName;
    public int grade;
    public int statMin;
    public int statMax;
    public int baseMin;
    public int baseMax;
}

// 학생 시작 스탯 데이터테이블 SO
[CreateAssetMenu(menuName = "Game/Data/Student Start Stat Table", fileName = "SO_StudentStartStatTable")]
public sealed class StudentStartStatTableSO : ScriptableObject
{
    [SerializeField] private List<StudentStartStatRow> _rows = new();

    private Dictionary<int, List<StudentStartStatRow>> _byStatId;
    private Dictionary<(int statId, int grade), StudentStartStatRow> _byStatIdAndGrade;

    public IReadOnlyList<StudentStartStatRow> Rows => _rows;

    void OnEnable()
    {
        BuildCache();
    }

    public void BuildCache()
    {
        _byStatId = new Dictionary<int, List<StudentStartStatRow>>();
        _byStatIdAndGrade = new Dictionary<(int, int), StudentStartStatRow>();

        foreach (var r in _rows)
        {
            if (r == null) continue;
            if (!TryParseStatId(r.statId, out var statId)) continue;

            if (!_byStatId.TryGetValue(statId, out var list))
            {
                list = new List<StudentStartStatRow>();
                _byStatId[statId] = list;
            }
            list.Add(r);

            _byStatIdAndGrade[(statId, r.grade)] = r;
        }
    }

    public IReadOnlyList<StudentStartStatRow> GetByStatId(int statId)
        => _byStatId.TryGetValue(statId, out var list) ? list : Array.Empty<StudentStartStatRow>();

    public bool TryGet(int statId, int grade, out StudentStartStatRow row)
        => _byStatIdAndGrade.TryGetValue((statId, grade), out row);

    public StudentStartStatRow GetOrNull(int statId, int grade)
        => _byStatIdAndGrade.TryGetValue((statId, grade), out var r) ? r : null;

    public bool TryGet(string statId, int grade, out StudentStartStatRow row)
    {
        row = null;
        return TryParseStatId(statId, out var parsed) && _byStatIdAndGrade.TryGetValue((parsed, grade), out row);
    }

    static bool TryParseStatId(string value, out int parsed)
    {
        parsed = 0;
        if (string.IsNullOrEmpty(value)) return false;
        int splitIndex = value.LastIndexOf('_');
        if (splitIndex >= 0 && splitIndex < value.Length - 1)
            value = value.Substring(splitIndex + 1);
        return int.TryParse(value, out parsed);
    }

#if UNITY_EDITOR
    public void ReplaceAll(List<StudentStartStatRow> newRows)
    {
        _rows = newRows ?? new List<StudentStartStatRow>();
        BuildCache();
    }
#endif
}
