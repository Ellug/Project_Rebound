using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public sealed class PositionInfoPopupRow
{
    public string id;
    public string titleText;
    public string desc;
}

[CreateAssetMenu(menuName = "Game/Data/Position Info Popup Table", fileName = "SO_PositionInfoPopupTable")]
public sealed class PositionInfoPopupTableSO : ScriptableObject
{
    [SerializeField] private List<PositionInfoPopupRow> _rows = new();

    public IReadOnlyList<PositionInfoPopupRow> Rows => _rows;

#if UNITY_EDITOR
    public void ReplaceAll(List<PositionInfoPopupRow> newRows)
    {
        _rows = newRows ?? new List<PositionInfoPopupRow>();
    }
#endif
}
