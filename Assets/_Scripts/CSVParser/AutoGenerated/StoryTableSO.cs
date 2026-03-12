using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public sealed class StoryRow
{
    public int id;
    public int line;
    public string name;
    public string context;
    public string bgImg;
    public string imgLeft;
    public string imgRight;
    public int bgmIndex;
    public string sfxName;
}

[CreateAssetMenu(menuName = "Game/Data/Story Table", fileName = "SO_StoryTable")]
public sealed class StoryTableSO : ScriptableObject
{
    [SerializeField] private List<StoryRow> _rows = new();

    public IReadOnlyList<StoryRow> Rows => _rows;

#if UNITY_EDITOR
    public void ReplaceAll(List<StoryRow> newRows)
    {
        _rows = newRows ?? new List<StoryRow>();
    }
#endif
}
