using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public sealed class HalftimeSelectTextRow
{
    public string id;
    public string description;
}

[CreateAssetMenu(menuName = "Game/Data/Halftime Select Text Table", fileName = "SO_HalftimeSelectTextTable")]
public sealed class HalftimeSelectTextTableSO : ScriptableObject
{
    [SerializeField] private List<HalftimeSelectTextRow> _rows = new();

    public IReadOnlyList<HalftimeSelectTextRow> Rows => _rows;

#if UNITY_EDITOR
    public void ReplaceAll(List<HalftimeSelectTextRow> newRows)
    {
        _rows = newRows ?? new List<HalftimeSelectTextRow>();
    }
#endif
}
