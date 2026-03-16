using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public sealed class FriendlyMatchScheduleMsgTextRow
{
    public string id;
    public string type;
    public string speaker;
    public string dialogue;
}

[CreateAssetMenu(menuName = "Game/Data/Friendly Match Schedule Msg Text Table", fileName = "SO_FriendlyMatchScheduleMsgTextTable")]
public sealed class FriendlyMatchScheduleMsgTextTableSO : ScriptableObject
{
    [SerializeField] private List<FriendlyMatchScheduleMsgTextRow> _rows = new();

    public IReadOnlyList<FriendlyMatchScheduleMsgTextRow> Rows => _rows;

#if UNITY_EDITOR
    public void ReplaceAll(List<FriendlyMatchScheduleMsgTextRow> newRows)
    {
        _rows = newRows ?? new List<FriendlyMatchScheduleMsgTextRow>();
    }
#endif
}
