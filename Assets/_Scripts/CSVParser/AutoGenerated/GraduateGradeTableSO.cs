using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public sealed class GraduateGradeRow
{
    public string id;
    public string grade;
    public int statAll;
    public int potentialMin;
    public int trustLevel;
    public int semiFinalPlus;
}

[CreateAssetMenu(menuName = "Game/Data/Graduate Grade Table", fileName = "SO_GraduateGradeTable")]
public sealed class GraduateGradeTableSO : ScriptableObject
{
    [SerializeField] private List<GraduateGradeRow> _rows = new();

    public IReadOnlyList<GraduateGradeRow> Rows => _rows;

#if UNITY_EDITOR
    public void ReplaceAll(List<GraduateGradeRow> newRows)
    {
        _rows = newRows ?? new List<GraduateGradeRow>();
    }
#endif
}
