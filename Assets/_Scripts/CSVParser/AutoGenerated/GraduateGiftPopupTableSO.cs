using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public sealed class GraduateGiftPopupRow
{
    public string id;
    public string rewardType;
    public string rewardHeader;
    public string rewardBody;
}

[CreateAssetMenu(menuName = "Game/Data/Graduate Gift Popup Table", fileName = "SO_GraduateGiftPopupTable")]
public sealed class GraduateGiftPopupTableSO : ScriptableObject
{
    [SerializeField] private List<GraduateGiftPopupRow> _rows = new();

    public IReadOnlyList<GraduateGiftPopupRow> Rows => _rows;

#if UNITY_EDITOR
    public void ReplaceAll(List<GraduateGiftPopupRow> newRows)
    {
        _rows = newRows ?? new List<GraduateGiftPopupRow>();
    }
#endif
}
