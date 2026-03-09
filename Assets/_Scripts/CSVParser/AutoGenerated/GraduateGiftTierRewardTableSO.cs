using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public sealed class GraduateGiftTierRewardRow
{
    public string id;
    public string rewardType;
    public int grade1;
    public int grade2;
    public int grade3;
    public int grade4;
    public string description;
}

[CreateAssetMenu(menuName = "Game/Data/Graduate Gift Tier Reward Table", fileName = "SO_GraduateGiftTierRewardTable")]
public sealed class GraduateGiftTierRewardTableSO : ScriptableObject
{
    [SerializeField] private List<GraduateGiftTierRewardRow> _rows = new();

    public IReadOnlyList<GraduateGiftTierRewardRow> Rows => _rows;

#if UNITY_EDITOR
    public void ReplaceAll(List<GraduateGiftTierRewardRow> newRows)
    {
        _rows = newRows ?? new List<GraduateGiftTierRewardRow>();
    }
#endif
}
