using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public sealed class CoachNodeMasterRow
{
    public int id;
    public int tierId;
    public string name;
    public int unlockCost;
    public string description;
    public int effectId;
}

[CreateAssetMenu(menuName = "Game/Data/Coach Node Master Table", fileName = "SO_CoachNodeMasterTable")]
public sealed class CoachNodeMasterTableSO : ScriptableObject
{
    [SerializeField] private List<CoachNodeMasterRow> _rows = new();

    public IReadOnlyList<CoachNodeMasterRow> Rows => _rows;

#if UNITY_EDITOR
    public void ReplaceAll(List<CoachNodeMasterRow> newRows)
    {
        _rows = newRows ?? new List<CoachNodeMasterRow>();
    }
#endif
}
