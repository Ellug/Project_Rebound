using System;
using System.Collections.Generic;
using UnityEngine;

// CSV 한 행(적 스탯 정보 1개)에 대응
[Serializable]
public sealed class EnemyStatRow
{
    public int day;
    public int mental;
    public int shoot;
    public int speed;
    public int jump;
    public int condition;
}

// 적 스탯 데이터테이블 SO
[CreateAssetMenu(menuName = "Game/Data/Enemy Stat Table", fileName = "SO_EnemyStatTable")]
public sealed class EnemyStatTableSO : ScriptableObject
{
    [SerializeField] private List<EnemyStatRow> _rows = new();

    private Dictionary<int, EnemyStatRow> _byDay;

    public IReadOnlyList<EnemyStatRow> Rows => _rows;

    void OnEnable()
    {
        BuildCache();
    }

    public void BuildCache()
    {
        _byDay = new Dictionary<int, EnemyStatRow>(_rows.Count);

        foreach (var r in _rows)
        {
            if (r == null) continue;
            _byDay[r.day] = r;
        }
    }

    public bool TryGet(int day, out EnemyStatRow row)
        => _byDay.TryGetValue(day, out row);

    public EnemyStatRow GetOrNull(int day)
        => _byDay.TryGetValue(day, out var r) ? r : null;

#if UNITY_EDITOR
    public void ReplaceAll(List<EnemyStatRow> newRows)
    {
        _rows = newRows ?? new List<EnemyStatRow>();
        BuildCache();
    }
#endif
}
