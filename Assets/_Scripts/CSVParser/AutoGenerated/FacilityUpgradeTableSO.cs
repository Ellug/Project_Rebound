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
    // 조회용 딕셔너리
    private Dictionary<string, List<FacilityUpgradeRow>> _byFacility;
    void OnEnable()
    {
        BuildCache();
    }

    private void BuildCache()
    {
        _byFacility = new Dictionary<string, List<FacilityUpgradeRow>>();

        foreach (var row in _rows)
        {
            if (!_byFacility.TryGetValue(row.facilityReq, out var list))
            {
                list = new List<FacilityUpgradeRow>();
                _byFacility[row.facilityReq] = list;
            }

            list.Add(row);
        }
    }

    public FacilityUpgradeRow Get(string facilityReq, int level)
    {
        if (_byFacility.TryGetValue(facilityReq, out var list))
        {
            return list.Find(x => x.facilityLv == level);
        }

        return null;
    }

    public IReadOnlyList<FacilityUpgradeRow> Rows => _rows;

#if UNITY_EDITOR
    public void ReplaceAll(List<FacilityUpgradeRow> newRows)
    {
        _rows = newRows ?? new List<FacilityUpgradeRow>();
    }
#endif
}
