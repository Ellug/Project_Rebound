using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public sealed class LobbySuddenEventMsgSaveDataRow
{
    public string iDSpeaker;
    public string iDDialogue;
    public int date;
    public string latestMessage;
    public string iconImage;
    public bool isVisibleIcon;
}

[CreateAssetMenu(menuName = "Game/Data/Lobby Sudden Event Msg Save Data Table", fileName = "SO_LobbySuddenEventMsgSaveDataTable")]
public sealed class LobbySuddenEventMsgSaveDataTableSO : ScriptableObject
{
    [SerializeField] private List<LobbySuddenEventMsgSaveDataRow> _rows = new();

    public IReadOnlyList<LobbySuddenEventMsgSaveDataRow> Rows => _rows;

#if UNITY_EDITOR
    public void ReplaceAll(List<LobbySuddenEventMsgSaveDataRow> newRows)
    {
        _rows = newRows ?? new List<LobbySuddenEventMsgSaveDataRow>();
    }
#endif
}
