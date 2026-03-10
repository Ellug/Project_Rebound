using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public sealed class StudentTrustStartRow
{
    public int grade;
    public int minTrust;
    public int maxTrust;
}

[CreateAssetMenu(menuName = "Game/Data/Student Trust Start Table", fileName = "SO_StudentTrustStartTable")]
public sealed class StudentTrustStartTableSO : ScriptableObject
{
    [SerializeField] private List<StudentTrustStartRow> _rows = new();

    public IReadOnlyList<StudentTrustStartRow> Rows => _rows;

#if UNITY_EDITOR
    public void ReplaceAll(List<StudentTrustStartRow> newRows)
    {
        _rows = newRows ?? new List<StudentTrustStartRow>();
    }
#endif
}
