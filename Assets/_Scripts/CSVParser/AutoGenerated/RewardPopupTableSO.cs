using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public sealed class RewardPopupRow
{
    public int id;
    public int money;
    public int fame;
    public string img;
    public string titleText;
    public string desc;
}

[CreateAssetMenu(menuName = "Game/Data/Reward Popup Table", fileName = "SO_RewardPopupTable")]
public sealed class RewardPopupTableSO : ScriptableObject
{
    [SerializeField] private List<RewardPopupRow> _rows = new();

    public IReadOnlyList<RewardPopupRow> Rows => _rows;

#if UNITY_EDITOR
    public void ReplaceAll(List<RewardPopupRow> newRows)
    {
        _rows = newRows ?? new List<RewardPopupRow>();
    }
#endif
}
