using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public sealed class ContentUnlockFeatureRow
{
    public int id;
    public string functionKey;
    public string name;
    public string category;
    public string description;
}

[CreateAssetMenu(menuName = "Game/Data/Content Unlock Feature Table", fileName = "SO_ContentUnlockFeatureTable")]
public sealed class ContentUnlockFeatureTableSO : ScriptableObject
{
    [SerializeField] private List<ContentUnlockFeatureRow> _rows = new();

    public IReadOnlyList<ContentUnlockFeatureRow> Rows => _rows;

#if UNITY_EDITOR
    public void ReplaceAll(List<ContentUnlockFeatureRow> newRows)
    {
        _rows = newRows ?? new List<ContentUnlockFeatureRow>();
    }
#endif
}
