using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;

public sealed class AddressableImageManager : Singleton<AddressableImageManager>
{
    [Header("Addressable Library")]
    [SerializeField] private AssetReference _libraryReference; // 이미지 라이브러리 SO(Addressable) 참조

    private AddressableImageLibrarySO _library;
    private AsyncOperationHandle<AddressableImageLibrarySO> _libraryHandle;
    private bool _isLibraryLoading;

    private readonly Dictionary<string, Sprite> _spriteCache = new(StringComparer.OrdinalIgnoreCase); // 파일명 id - Sprite 캐시 구조
    private readonly Dictionary<string, AsyncOperationHandle<Sprite>> _spriteHandles = new(StringComparer.OrdinalIgnoreCase); // 파일명 id - Sprite 로드 핸들 캐시 구조
    private readonly Dictionary<string, List<Action<Sprite>>> _pendingCallbacks = new(StringComparer.OrdinalIgnoreCase); // 같은 id 동시 요청 콜백 대기열

    public bool IsLibraryLoaded => _library != null; // 라이브러리 로드 상태

    // 싱글톤 초기화 시 라이브러리 선로딩
    protected override void OnSingletonAwake()
    {
        StartCoroutine(EnsureLibraryLoadedRoutine());
    }

    // 파괴 시 캐시/핸들 정리
    void OnDestroy()
    {
        ReleaseAllSprites();
        ReleaseLibrary();
    }

    // 파일명(ID) 기준 스프라이트 비동기 로드
    public void LoadSprite(string fileName, Action<Sprite> onLoaded)
    {
        StartCoroutine(LoadSpriteRoutine(fileName, onLoaded));
    }

    // 파일명(ID) 기준 스프라이트 로드 코루틴
    public IEnumerator LoadSpriteRoutine(string fileName, Action<Sprite> onLoaded)
    {
        string normalized = NormalizeKey(fileName);
        if (string.IsNullOrEmpty(normalized))
        {
            onLoaded?.Invoke(null);
            yield break;
        }

        if (_spriteCache.TryGetValue(normalized, out Sprite cachedSprite) && cachedSprite != null)
        {
            onLoaded?.Invoke(cachedSprite);
            yield break;
        }

        if (_pendingCallbacks.TryGetValue(normalized, out List<Action<Sprite>> pending))
        {
            pending.Add(onLoaded);
            yield break;
        }

        _pendingCallbacks[normalized] = new List<Action<Sprite>>(1) { onLoaded };

        yield return EnsureLibraryLoadedRoutine();
        if (_library == null)
        {
            CompletePending(normalized, null);
            yield break;
        }

        if (!_library.TryGetSpriteReference(normalized, out AssetReferenceSprite spriteReference) ||
            spriteReference == null ||
            !spriteReference.RuntimeKeyIsValid())
        {
            Debug.LogWarning($"[AddressableImageManager] Missing sprite reference: {normalized}");
            CompletePending(normalized, null);
            yield break;
        }

        AsyncOperationHandle<Sprite> loadHandle = spriteReference.LoadAssetAsync<Sprite>();
        yield return loadHandle;

        Sprite loadedSprite = null;

        if (loadHandle.Status == AsyncOperationStatus.Succeeded)
        {
            loadedSprite = loadHandle.Result;
            _spriteCache[normalized] = loadedSprite;
            _spriteHandles[normalized] = loadHandle;
        }
        else
        {
            Debug.LogError($"[AddressableImageManager] Failed to load sprite: {normalized}");
            if (loadHandle.IsValid())
                Addressables.Release(loadHandle);
        }

        CompletePending(normalized, loadedSprite);
    }

    // 캐시에 이미 로드된 스프라이트 조회
    public bool TryGetCachedSprite(string fileName, out Sprite sprite)
    {
        string normalized = NormalizeKey(fileName);
        if (string.IsNullOrEmpty(normalized))
        {
            sprite = null;
            return false;
        }

        return _spriteCache.TryGetValue(normalized, out sprite) && sprite != null;
    }

    // 특정 파일명(ID) 스프라이트만 캐시에서 해제
    public void ReleaseSprite(string fileName)
    {
        string normalized = NormalizeKey(fileName);
        if (string.IsNullOrEmpty(normalized))
            return;

        ReleaseSpriteInternal(normalized);
    }

    // 로드한 모든 스프라이트 캐시/핸들 해제
    public void ReleaseAllSprites()
    {
        foreach (KeyValuePair<string, AsyncOperationHandle<Sprite>> pair in _spriteHandles)
        {
            AsyncOperationHandle<Sprite> handle = pair.Value;
            if (handle.IsValid())
                Addressables.Release(handle);
        }

        _spriteHandles.Clear();
        _spriteCache.Clear();
        _pendingCallbacks.Clear();
    }

    // 파일명(ID) 목록을 미리 로드
    public void PreloadSprites(IEnumerable<string> fileNames)
    {
        StartCoroutine(PreloadSpritesRoutine(fileNames));
    }

    // 파일명(ID) 목록 선로딩 코루틴
    public IEnumerator PreloadSpritesRoutine(IEnumerable<string> fileNames)
    {
        if (fileNames == null)
            yield break;

        foreach (string fileName in fileNames)
            yield return LoadSpriteRoutine(fileName, null);
    }

    // 라이브러리 SO 핸들 해제
    public void ReleaseLibrary()
    {
        _library = null;

        if (!_libraryHandle.IsValid())
            return;

        Addressables.Release(_libraryHandle);
        _libraryHandle = default;
    }

    // 라이브러리 SO를 필요 시 한 번만 로드
    private IEnumerator EnsureLibraryLoadedRoutine()
    {
        if (_library != null)
            yield break;

        if (_isLibraryLoading)
        {
            while (_isLibraryLoading)
                yield return null;

            yield break;
        }

        if (_libraryReference == null ||
            string.IsNullOrEmpty(_libraryReference.AssetGUID) ||
            !_libraryReference.RuntimeKeyIsValid())
        {
            Debug.LogError("[AddressableImageManager] Library reference is not assigned.");
            yield break;
        }

        _isLibraryLoading = true;

        _libraryHandle = _libraryReference.LoadAssetAsync<AddressableImageLibrarySO>();
        yield return _libraryHandle;

        if (_libraryHandle.Status == AsyncOperationStatus.Succeeded)
        {
            _library = _libraryHandle.Result;
        }
        else
        {
            Debug.LogError("[AddressableImageManager] Failed to load image library asset.");
            if (_libraryHandle.IsValid())
                Addressables.Release(_libraryHandle);
            _libraryHandle = default;
        }

        _isLibraryLoading = false;
    }

    // 같은 ID로 대기 중인 콜백들을 한 번에 완료
    private void CompletePending(string normalizedFileName, Sprite sprite)
    {
        if (!_pendingCallbacks.TryGetValue(normalizedFileName, out List<Action<Sprite>> callbacks))
            return;

        _pendingCallbacks.Remove(normalizedFileName);

        for (int i = 0; i < callbacks.Count; i++)
            callbacks[i]?.Invoke(sprite);
    }

    // 내부 공통 해제 로직
    private void ReleaseSpriteInternal(string normalizedFileName)
    {
        _spriteCache.Remove(normalizedFileName);

        if (!_spriteHandles.TryGetValue(normalizedFileName, out AsyncOperationHandle<Sprite> handle))
            return;

        if (handle.IsValid())
            Addressables.Release(handle);

        _spriteHandles.Remove(normalizedFileName);
    }

    // 조회 키를 trim 처리해 정규화
    private static string NormalizeKey(string key)
    {
        return string.IsNullOrWhiteSpace(key) ? string.Empty : key.Trim();
    }
}
