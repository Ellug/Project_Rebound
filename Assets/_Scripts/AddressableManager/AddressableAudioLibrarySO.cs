using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AddressableAssets;

[CreateAssetMenu(menuName = "Game/Data/Addressable Audio Library", fileName = "SO_AddressableAudioLibrary")]
public sealed class AddressableAudioLibrarySO : ScriptableObject
{
    // 오디오 ID와 Addressables 참조를 묶는 엔트리
    [Serializable]
    public sealed class Entry
    {
        // 오디오 식별자
        public int id;
        // 오디오 이름
        public string name;
        // Addressables 오디오 클립 참조
        public AssetReference clipReference;
    }

    // Inspector에서 수동으로 채우는 원본 목록
    [SerializeField] private List<Entry> _entries = new();

    // 런타임 조회용 캐시 딕셔너리
    private Dictionary<int, AssetReference> _byId;

    // 외부 읽기 전용 목록
    public IReadOnlyList<Entry> Entries => _entries;

    // 에셋 로드 시 캐시를 준비
    void OnEnable()
    {
        BuildCache();
    }

    // 오디오 ID로 Addressable 참조를 조회
    public bool TryGetClipReference(int id, out AssetReference clipReference)
    {
        if (_byId == null)
            BuildCache();

        return _byId.TryGetValue(id, out clipReference);
    }

    // 원본 목록을 id 키 딕셔너리로 재구성
    public void BuildCache()
    {
        _byId = new Dictionary<int, AssetReference>(_entries.Count);

        for (int i = 0; i < _entries.Count; i++)
        {
            Entry entry = _entries[i];
            if (entry == null)
                continue;

            if (entry.id <= 0)
                continue;

            if (entry.clipReference == null)
                continue;

            if (_byId.ContainsKey(entry.id))
            {
                Debug.LogWarning($"[AddressableAudioLibrarySO] Duplicate id detected: {entry.id}", this);
                continue;
            }

            _byId.Add(entry.id, entry.clipReference);
        }
    }

#if UNITY_EDITOR
    // 에디터에서 엔트리 목록을 통째로 교체
    public void ReplaceAll(List<Entry> entries)
    {
        _entries = entries ?? new List<Entry>();
        BuildCache();
    }
#endif
}
