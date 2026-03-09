using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public sealed class CoachNodeEffectDetailRow
{
    public int id;
    public string targetStat;
    public string applyMethod;
    public float effectValue;
    public int functionId;
}

[CreateAssetMenu(menuName = "Game/Data/Coach Node Effect Detail Table", fileName = "SO_CoachNodeEffectDetailTable")]
public sealed class CoachNodeEffectDetailTableSO : ScriptableObject
{
    [SerializeField] private List<CoachNodeEffectDetailRow> _rows = new();

    public IReadOnlyList<CoachNodeEffectDetailRow> Rows => _rows;

#if UNITY_EDITOR
    public void ReplaceAll(List<CoachNodeEffectDetailRow> newRows)
    {
        _rows = newRows ?? new List<CoachNodeEffectDetailRow>();
    }
#endif
}
