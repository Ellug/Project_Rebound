using System;
using System.Collections.Generic;
using UnityEngine;

// CSV 한 행(상시 효과 1개)에 대응
[Serializable]
public sealed class AlwaysEffectRow
{
    public string id;
    public bool trainingState;

    public int conditionIncrease;
    public int conditionDecline;

    public int conditionRecoveryUp;
    public int conditionRecoveryDown;

    public float trainingIncrease;
    public float trainingDecline;

    public int mentalIncrease;
    public int mentalDecline;

    public int trustIncrease;
    public int trustDecline;
}

// 상시 효과 데이터테이블 SO
[CreateAssetMenu(menuName = "Game/Data/Always Effect Table", fileName = "SO_AlwaysEffectTable")]
public sealed class AlwaysEffectTableSO : ScriptableObject
{
    [SerializeField] private List<AlwaysEffectRow> _rows = new();

    private Dictionary<string, AlwaysEffectRow> _byId;

    public IReadOnlyList<AlwaysEffectRow> Rows => _rows;

    private void OnEnable()
    {
        BuildCache();
    }

    public void BuildCache()
    {
        _byId = new Dictionary<string, AlwaysEffectRow>(_rows.Count, StringComparer.Ordinal);

        for (int i = 0; i < _rows.Count; i++)
        {
            var r = _rows[i];
            if (r == null) continue;
            if (string.IsNullOrEmpty(r.id)) continue;

            _byId[r.id] = r;
        }
    }

    public bool TryGet(string id, out AlwaysEffectRow row)
        => _byId.TryGetValue(id, out row);

#if UNITY_EDITOR
    public void ReplaceAll(List<AlwaysEffectRow> newRows)
    {
        _rows = newRows ?? new List<AlwaysEffectRow>();
        BuildCache();
    }
#endif
}
