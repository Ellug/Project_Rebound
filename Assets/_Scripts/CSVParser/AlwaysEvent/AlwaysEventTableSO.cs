using System;
using System.Collections.Generic;
using UnityEngine;

// CSV 한 행(상시 이벤트 1개)에 대응
[Serializable]
public sealed class AlwaysEventRow
{
    public string id;
    public string name;
    public string type;

    public string termStart;
    public string termEnd;
    public int term;

    public int priority;
    public int range;

    public string effectId;
    public string description;
}

// 상시 이벤트 데이터테이블 SO
[CreateAssetMenu(menuName = "Game/Data/Always Event Table", fileName = "SO_AlwaysEventTable")]
public sealed class AlwaysEventTableSO : ScriptableObject
{
    [SerializeField] private List<AlwaysEventRow> _rows = new();

    // 복합키(id_termStart) → 행 캐시
    private Dictionary<string, AlwaysEventRow> _byCompositeKey;

    public IReadOnlyList<AlwaysEventRow> Rows => _rows;

    private void OnEnable()
    {
        BuildCache();
    }

    public void BuildCache()
    {
        _byCompositeKey = new Dictionary<string, AlwaysEventRow>(_rows.Count, StringComparer.Ordinal);

        for (int i = 0; i < _rows.Count; i++)
        {
            var r = _rows[i];
            if (r == null) continue;
            if (string.IsNullOrEmpty(r.id)) continue;

            string key = MakeKey(r.id, r.termStart);

            if (_byCompositeKey.ContainsKey(key))
            {
                Debug.LogWarning($"[AlwaysEventTable] Duplicate key detected: {key}");
                continue;
            }

            _byCompositeKey[key] = r;
        }
    }

    // id만으로 조회 (첫 번째 일치 항목 반환)
    public bool TryGet(string id, out AlwaysEventRow row)
    {
        row = null;
        for (int i = 0; i < _rows.Count; i++)
        {
            if (_rows[i] != null && _rows[i].id == id)
            {
                row = _rows[i];
                return true;
            }
        }
        return false;
    }

    // id + termStart 복합키로 정확한 행 조회
    public bool TryGetByKey(string id, string termStart, out AlwaysEventRow row)
        => _byCompositeKey.TryGetValue(MakeKey(id, termStart), out row);

    // AlwaysEventManager.GetRowId()와 동일한 키 생성 규칙
    public static string MakeKey(string id, string termStart)
    {
        string safeId = string.IsNullOrWhiteSpace(id) ? "(no-id)" : id.Trim();
        string safeStart = string.IsNullOrWhiteSpace(termStart) ? "" : termStart.Trim();
        return $"{safeId}_{safeStart}";
    }

#if UNITY_EDITOR
    public void ReplaceAll(List<AlwaysEventRow> newRows)
    {
        _rows = newRows ?? new List<AlwaysEventRow>();
        BuildCache();
    }
#endif
}
