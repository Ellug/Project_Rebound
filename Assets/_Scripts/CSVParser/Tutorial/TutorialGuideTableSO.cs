using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public sealed class TutorialGuideRow
{
    public int index;
    public string img;
    public string titleText;
    public string desc;
}

[CreateAssetMenu(menuName = "Game/Data/Tutorial Guide Table", fileName = "SO_TutorialGuideTable")]
public sealed class TutorialGuideTableSO : ScriptableObject
{
    [SerializeField] private List<TutorialGuideRow> _rows = new();
    private Dictionary<int, TutorialGuideRow> _byIndex;

    public IReadOnlyList<TutorialGuideRow> Rows => _rows;

    void OnEnable() => BuildCache();

    public void BuildCache()
    {
        _byIndex = new Dictionary<int, TutorialGuideRow>(_rows.Count);
        foreach (var row in _rows)
        {
            if (row == null) continue;
            _byIndex[row.index] = row;
        }
    }

    public bool TryGet(int index, out TutorialGuideRow row) => _byIndex.TryGetValue(index, out row);

#if UNITY_EDITOR
    public void ReplaceAll(List<TutorialGuideRow> newRows)
    {
        _rows = newRows ?? new List<TutorialGuideRow>();
        BuildCache();
    }
#endif
}
