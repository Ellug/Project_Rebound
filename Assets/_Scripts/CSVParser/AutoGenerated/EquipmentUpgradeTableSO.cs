using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public sealed class EquipmentUpgradeRow
{
    public string id;
    public string presentCategory;
    public string outlineImage;
    public string tierName;
    public int rank;
    public string target1;
    public float amount1;
    public string target2;
    public float amount;
}

[CreateAssetMenu(menuName = "Game/Data/Equipment Upgrade Table", fileName = "SO_EquipmentUpgradeTable")]
public sealed class EquipmentUpgradeTableSO : ScriptableObject
{
    [SerializeField] private List<EquipmentUpgradeRow> _rows = new();

    public IReadOnlyList<EquipmentUpgradeRow> Rows => _rows;

#if UNITY_EDITOR
    public void ReplaceAll(List<EquipmentUpgradeRow> newRows)
    {
        _rows = newRows ?? new List<EquipmentUpgradeRow>();
    }
#endif
}
