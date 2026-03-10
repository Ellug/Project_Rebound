using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public sealed class FacilityUpgradeRow
{
    public string id;
    public string facilityReq;
    public string facilityName;
    public int facilityLv;
    public int upgradeCost;
    public int conditionDecayEfficiency;
    public int trainingExpEfficiency;
    public string resourceId;
}

[CreateAssetMenu(menuName = "Game/Data/Facility Upgrade Table", fileName = "SO_FacilityUpgradeTable")]
public sealed class FacilityUpgradeTableSO : ScriptableObject
{
    [SerializeField] private List<FacilityUpgradeRow> _rows = new();

    public IReadOnlyList<FacilityUpgradeRow> Rows => _rows;

#if UNITY_EDITOR
    public void ReplaceAll(List<FacilityUpgradeRow> newRows)
    {
        _rows = newRows ?? new List<FacilityUpgradeRow>();
    }
#endif
}
