using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public sealed class StudentTrustDownConditionStatusRow
{
    public int conditionMax;
    public int conditionMin;
    public int durationDays;
    public int favorabilityPenalty;
}

[CreateAssetMenu(menuName = "Game/Data/Student Trust Down Condition Status Table", fileName = "SO_StudentTrustDownConditionStatusTable")]
public sealed class StudentTrustDownConditionStatusTableSO : ScriptableObject
{
    [SerializeField] private List<StudentTrustDownConditionStatusRow> _rows = new();

    public IReadOnlyList<StudentTrustDownConditionStatusRow> Rows => _rows;

#if UNITY_EDITOR
    public void ReplaceAll(List<StudentTrustDownConditionStatusRow> newRows)
    {
        _rows = newRows ?? new List<StudentTrustDownConditionStatusRow>();
    }
#endif
}
