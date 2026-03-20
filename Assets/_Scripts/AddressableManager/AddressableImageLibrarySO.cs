using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AddressableAssets;
#if UNITY_EDITOR
using System.IO;
using UnityEditor;
#endif

[CreateAssetMenu(menuName = "Game/Data/Addressable Image Library", fileName = "SO_AddressableImageLibrary")]
public sealed class AddressableImageLibrarySO : ScriptableObject
{
    // 파일명 키와 스프라이트 참조를 묶는 엔트리
    [Serializable]
    public sealed class Entry
    {
        // 조회용 ID(확장자 제외 파일명)
        public string fileName;
        // Addressables 스프라이트 참조
        public AssetReferenceSprite spriteReference;
    }

    // Inspector에서 수동으로 채우는 원본 목록
    [SerializeField] private List<Entry> _entries = new();

    // 런타임 조회용 캐시 딕셔너리
    private Dictionary<string, AssetReferenceSprite> _byFileName;

    // 외부 읽기 전용 목록
    public IReadOnlyList<Entry> Entries => _entries;

    // 에셋 로드 시 캐시를 준비
    void OnEnable()
    {
        BuildCache();
    }

#if UNITY_EDITOR
    // Inspector 값 변경 시 비어있는 fileName을 자동 채움
    void OnValidate()
    {
        FillEmptyFileNamesFromReference();
        BuildCache();
    }
#endif

    // 파일명(ID)으로 스프라이트 참조를 조회
    public bool TryGetSpriteReference(string fileName, out AssetReferenceSprite spriteReference)
    {
        if (_byFileName == null)
            BuildCache();

        string normalized = NormalizeKey(fileName);
        if (string.IsNullOrEmpty(normalized))
        {
            spriteReference = null;
            return false;
        }

        return _byFileName.TryGetValue(normalized, out spriteReference);
    }

    // 원본 목록을 파일명 키 딕셔너리로 재구성
    public void BuildCache()
    {
        _byFileName = new Dictionary<string, AssetReferenceSprite>(StringComparer.OrdinalIgnoreCase);

        for (int i = 0; i < _entries.Count; i++)
        {
            Entry entry = _entries[i];
            if (entry == null)
                continue;

            string normalized = NormalizeKey(entry.fileName);
            if (string.IsNullOrEmpty(normalized))
                continue;

            if (entry.spriteReference == null)
                continue;

            if (_byFileName.ContainsKey(normalized))
            {
                Debug.LogWarning($"[AddressableImageLibrarySO] Duplicate fileName detected: {normalized}", this);
                continue;
            }

            _byFileName.Add(normalized, entry.spriteReference);
        }
    }

    // 조회 키를 trim 처리해 정규화
    private static string NormalizeKey(string key)
    {
        return string.IsNullOrWhiteSpace(key) ? string.Empty : key.Trim();
    }

#if UNITY_EDITOR
    // 비어있는 fileName만 spriteReference의 실제 파일명으로 채움
    private void FillEmptyFileNamesFromReference()
    {
        bool changed = false;

        for (int i = 0; i < _entries.Count; i++)
        {
            Entry entry = _entries[i];
            if (entry == null)
                continue;

            if (!string.IsNullOrWhiteSpace(entry.fileName))
                continue;

            if (entry.spriteReference == null)
                continue;

            string guid = entry.spriteReference.AssetGUID;
            if (string.IsNullOrEmpty(guid))
                continue;

            string assetPath = AssetDatabase.GUIDToAssetPath(guid);
            if (string.IsNullOrEmpty(assetPath))
                continue;

            string inferredName = Path.GetFileNameWithoutExtension(assetPath);
            if (string.IsNullOrEmpty(inferredName))
                continue;

            entry.fileName = inferredName;
            changed = true;
        }

        if (changed)
            EditorUtility.SetDirty(this);
    }

    // 컨텍스트 메뉴로 비어있는 fileName 자동 채움 실행
    [ContextMenu("Fill Empty FileNames From Sprite")]
    private void FillEmptyFileNamesFromSpriteMenu()
    {
        FillEmptyFileNamesFromReference();
        BuildCache();
    }

    // 에디터에서 엔트리 목록을 통째로 교체
    public void ReplaceAll(List<Entry> entries)
    {
        _entries = entries ?? new List<Entry>();
        FillEmptyFileNamesFromReference();
        BuildCache();
    }
#endif
}
