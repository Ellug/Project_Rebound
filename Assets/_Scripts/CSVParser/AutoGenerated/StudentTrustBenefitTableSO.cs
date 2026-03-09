using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public sealed class StudentTrustBenefitRow
{
    public int trustLevel;
    public int training;
    public int plusInsanity;
    public int plusInjury;
    public int plusAllstat;
}

[CreateAssetMenu(menuName = "Game/Data/Student Trust Benefit Table", fileName = "SO_StudentTrustBenefitTable")]
public sealed class StudentTrustBenefitTableSO : ScriptableObject
{
    [SerializeField] private List<StudentTrustBenefitRow> _rows = new();

    public IReadOnlyList<StudentTrustBenefitRow> Rows => _rows;

#if UNITY_EDITOR
    public void ReplaceAll(List<StudentTrustBenefitRow> newRows)
    {
        _rows = newRows ?? new List<StudentTrustBenefitRow>();
    }
#endif
}
