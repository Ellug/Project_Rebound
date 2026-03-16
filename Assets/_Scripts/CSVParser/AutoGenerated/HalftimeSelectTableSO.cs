using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public sealed class HalftimeSelectRow
{
    public string id;
    public string choiceName;
    public string target;
    public int targetNum;
    public string description;
    public string effectDescription;
    public string effect1;
    public string effect2;
    public string effect3;
}

[CreateAssetMenu(menuName = "Game/Data/Halftime Select Table", fileName = "SO_HalftimeSelectTable")]
public sealed class HalftimeSelectTableSO : ScriptableObject
{
    [SerializeField] private List<HalftimeSelectRow> _rows = new();

    public IReadOnlyList<HalftimeSelectRow> Rows => _rows;

#if UNITY_EDITOR
    public void ReplaceAll(List<HalftimeSelectRow> newRows)
    {
        _rows = newRows ?? new List<HalftimeSelectRow>();
    }
#endif
}
