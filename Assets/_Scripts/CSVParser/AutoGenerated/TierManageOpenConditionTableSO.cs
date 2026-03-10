using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public sealed class TierManageOpenConditionRow
{
    public int id;
    public int tierLevel;
    public int tierName;
    public int unlockConditionCount;
    public int maxNodeCount;
    public int tierBonusEffectId;
}

[CreateAssetMenu(menuName = "Game/Data/Tier Manage Open Condition Table", fileName = "SO_TierManageOpenConditionTable")]
public sealed class TierManageOpenConditionTableSO : ScriptableObject
{
    [SerializeField] private List<TierManageOpenConditionRow> _rows = new();

    public IReadOnlyList<TierManageOpenConditionRow> Rows => _rows;

#if UNITY_EDITOR
    public void ReplaceAll(List<TierManageOpenConditionRow> newRows)
    {
        _rows = newRows ?? new List<TierManageOpenConditionRow>();
    }
#endif
}
