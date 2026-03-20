using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public sealed class SuddenEventMsgSaveDataRow
{
    public int iDStudent;
    public int index;
    public string sender;
    public string recentDialogue;
    public string status;
    public int dateStart;
    public int dateEnd;
    public int recentUpdate;
    public string latestMessage;
    public string messageTitle;
}

[CreateAssetMenu(menuName = "Game/Data/Sudden Event Msg Save Data Table", fileName = "SO_SuddenEventMsgSaveDataTable")]
public sealed class SuddenEventMsgSaveDataTableSO : ScriptableObject
{
    [SerializeField] private List<SuddenEventMsgSaveDataRow> _rows = new();

    public IReadOnlyList<SuddenEventMsgSaveDataRow> Rows => _rows;

#if UNITY_EDITOR
    public void ReplaceAll(List<SuddenEventMsgSaveDataRow> newRows)
    {
        _rows = newRows ?? new List<SuddenEventMsgSaveDataRow>();
    }
#endif
}
