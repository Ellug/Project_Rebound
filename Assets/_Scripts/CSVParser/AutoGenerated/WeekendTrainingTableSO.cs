using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public sealed class WeekendTrainingRow
{
    public int index;
    public int conditionCost;
    public int mental;
    public float shoot;
    public float speed;
    public float jump;
    public float stamina;
    public string linkedEventId;
}

[CreateAssetMenu(menuName = "Game/Data/Weekend Training Table", fileName = "SO_WeekendTrainingTable")]
public sealed class WeekendTrainingTableSO : ScriptableObject
{
    [SerializeField] private List<WeekendTrainingRow> _rows = new();

    public IReadOnlyList<WeekendTrainingRow> Rows => _rows;

#if UNITY_EDITOR
    public void ReplaceAll(List<WeekendTrainingRow> newRows)
    {
        _rows = newRows ?? new List<WeekendTrainingRow>();
    }
#endif
}
