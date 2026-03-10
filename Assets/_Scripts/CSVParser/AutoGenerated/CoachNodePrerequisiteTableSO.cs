using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public sealed class CoachNodePrerequisiteRow
{
    public int col1;
    public int nodeId;
    public int targetPrerequisiteId;
    public string 비고;
}

[CreateAssetMenu(menuName = "Game/Data/Coach Node Prerequisite Table", fileName = "SO_CoachNodePrerequisiteTable")]
public sealed class CoachNodePrerequisiteTableSO : ScriptableObject
{
    [SerializeField] private List<CoachNodePrerequisiteRow> _rows = new();

    public IReadOnlyList<CoachNodePrerequisiteRow> Rows => _rows;

#if UNITY_EDITOR
    public void ReplaceAll(List<CoachNodePrerequisiteRow> newRows)
    {
        _rows = newRows ?? new List<CoachNodePrerequisiteRow>();
    }
#endif
}
