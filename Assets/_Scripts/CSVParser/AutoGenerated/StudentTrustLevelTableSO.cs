using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public sealed class StudentTrustLevelRow
{
    public int trustLevel;
    public string levelName;
    public int minLevel;
    public int maxLevel;
}

[CreateAssetMenu(menuName = "Game/Data/Student Trust Level Table", fileName = "SO_StudentTrustLevelTable")]
public sealed class StudentTrustLevelTableSO : ScriptableObject
{
    [SerializeField] private List<StudentTrustLevelRow> _rows = new();

    public IReadOnlyList<StudentTrustLevelRow> Rows => _rows;

#if UNITY_EDITOR
    public void ReplaceAll(List<StudentTrustLevelRow> newRows)
    {
        _rows = newRows ?? new List<StudentTrustLevelRow>();
    }
#endif
}
