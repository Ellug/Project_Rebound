using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public sealed class SuddenEventMsgTextRow
{
    public string iD;
    public string speaker;
    public string dialogue;
}

[CreateAssetMenu(menuName = "Game/Data/Sudden Event Msg Text Table", fileName = "SO_SuddenEventMsgTextTable")]
public sealed class SuddenEventMsgTextTableSO : ScriptableObject
{
    [SerializeField] private List<SuddenEventMsgTextRow> _rows = new();

    public IReadOnlyList<SuddenEventMsgTextRow> Rows => _rows;

#if UNITY_EDITOR
    public void ReplaceAll(List<SuddenEventMsgTextRow> newRows)
    {
        _rows = newRows ?? new List<SuddenEventMsgTextRow>();
    }
#endif
}
