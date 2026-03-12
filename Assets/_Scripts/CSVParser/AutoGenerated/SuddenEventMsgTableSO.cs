using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public sealed class SuddenEventMsgRow
{
    public string iD;
    public string messageIndex;
    public string messageDialogue;
    public string next;
    public bool isChoice;
    public string choice1Dialogue;
    public string choice1Next;
    public string choice2Dialogue;
    public string choice2Next;
    public string choice3Dialogue;
    public string choice3Next;
    public string suddenEvent;
}

[CreateAssetMenu(menuName = "Game/Data/Sudden Event Msg Table", fileName = "SO_SuddenEventMsgTable")]
public sealed class SuddenEventMsgTableSO : ScriptableObject
{
    [SerializeField] private List<SuddenEventMsgRow> _rows = new();

    public IReadOnlyList<SuddenEventMsgRow> Rows => _rows;

#if UNITY_EDITOR
    public void ReplaceAll(List<SuddenEventMsgRow> newRows)
    {
        _rows = newRows ?? new List<SuddenEventMsgRow>();
    }
#endif
}
