using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public sealed class SuddenEventMsgListRow
{
    public string iDIndex;
    public string iDSpeaker;
    public string iDDialogue;
    public string senderCategory;
    public int date;
}

[CreateAssetMenu(menuName = "Game/Data/Sudden Event Msg List Table", fileName = "SO_SuddenEventMsgListTable")]
public sealed class SuddenEventMsgListTableSO : ScriptableObject
{
    [SerializeField] private List<SuddenEventMsgListRow> _rows = new();

    public IReadOnlyList<SuddenEventMsgListRow> Rows => _rows;

#if UNITY_EDITOR
    public void ReplaceAll(List<SuddenEventMsgListRow> newRows)
    {
        _rows = newRows ?? new List<SuddenEventMsgListRow>();
    }
#endif
}
