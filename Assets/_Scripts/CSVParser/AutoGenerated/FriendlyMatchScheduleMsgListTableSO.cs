using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public sealed class FriendlyMatchScheduleMsgListRow
{
    public string id;
    public string schoolName;
    public int latestScheduledDate;
}

[CreateAssetMenu(menuName = "Game/Data/Friendly Match Schedule Msg List Table", fileName = "SO_FriendlyMatchScheduleMsgListTable")]
public sealed class FriendlyMatchScheduleMsgListTableSO : ScriptableObject
{
    [SerializeField] private List<FriendlyMatchScheduleMsgListRow> _rows = new();

    public IReadOnlyList<FriendlyMatchScheduleMsgListRow> Rows => _rows;

#if UNITY_EDITOR
    public void ReplaceAll(List<FriendlyMatchScheduleMsgListRow> newRows)
    {
        _rows = newRows ?? new List<FriendlyMatchScheduleMsgListRow>();
    }
#endif
}
