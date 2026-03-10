using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AddressableAssets;

[CreateAssetMenu(menuName = "Game/Data/Table Load Config", fileName = "TableLoadConfig")]
public sealed class TableLoadConfigSO : ScriptableObject
{
    // StartManager가 순서대로 로드할 Addressable 테이블 참조 목록
    [SerializeField] private List<AssetReference> _tableRefs = new();

    public IReadOnlyList<AssetReference> TableRefs => _tableRefs;

#if UNITY_EDITOR
    // 자동 동기화 시 현재 목록을 통째로 교체
    public void ReplaceAll(List<AssetReference> tableRefs)
    {
        _tableRefs = tableRefs ?? new List<AssetReference>();
    }
#endif
}
