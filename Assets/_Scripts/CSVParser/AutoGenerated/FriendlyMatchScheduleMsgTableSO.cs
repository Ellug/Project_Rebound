using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public sealed class FriendlyMatchScheduleMsgRow
{
    public string iD;
    public string messageIndex;
    public string messageDialogue;
    public int branchType;
    public string next;
    public string choice1Dialogue;
    public string choice1Next;
    public string choice2Dialogue;
    public string choice2Next;
    public string choice3Dialogue;
    public string choice3Next;
}

[CreateAssetMenu(menuName = "Game/Data/Friendly Match Schedule Msg Table", fileName = "SO_FriendlyMatchScheduleMsgTable")]
public sealed class FriendlyMatchScheduleMsgTableSO : ScriptableObject
{
    [SerializeField] private List<FriendlyMatchScheduleMsgRow> _rows = new();

    public IReadOnlyList<FriendlyMatchScheduleMsgRow> Rows => _rows;

#if UNITY_EDITOR
    public void ReplaceAll(List<FriendlyMatchScheduleMsgRow> newRows)
    {
        _rows = newRows ?? new List<FriendlyMatchScheduleMsgRow>();
    }
#endif
}
