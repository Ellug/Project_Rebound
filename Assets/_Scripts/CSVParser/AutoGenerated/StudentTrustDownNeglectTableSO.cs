using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public sealed class StudentTrustDownNeglectRow
{
    public int trustLevel;
    public string idletime;
    public int dayTrust;
}

[CreateAssetMenu(menuName = "Game/Data/Student Trust Down Neglect Table", fileName = "SO_StudentTrustDownNeglectTable")]
public sealed class StudentTrustDownNeglectTableSO : ScriptableObject
{
    [SerializeField] private List<StudentTrustDownNeglectRow> _rows = new();

    public IReadOnlyList<StudentTrustDownNeglectRow> Rows => _rows;

#if UNITY_EDITOR
    public void ReplaceAll(List<StudentTrustDownNeglectRow> newRows)
    {
        _rows = newRows ?? new List<StudentTrustDownNeglectRow>();
    }
#endif
}
