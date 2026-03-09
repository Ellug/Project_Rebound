using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public sealed class StudentTrustUpConditionRow
{
    public string triggerCondition;
    public string targetScope;
    public int increaseAmount;
}

[CreateAssetMenu(menuName = "Game/Data/Student Trust Up Condition Table", fileName = "SO_StudentTrustUpConditionTable")]
public sealed class StudentTrustUpConditionTableSO : ScriptableObject
{
    [SerializeField] private List<StudentTrustUpConditionRow> _rows = new();

    public IReadOnlyList<StudentTrustUpConditionRow> Rows => _rows;

#if UNITY_EDITOR
    public void ReplaceAll(List<StudentTrustUpConditionRow> newRows)
    {
        _rows = newRows ?? new List<StudentTrustUpConditionRow>();
    }
#endif
}
