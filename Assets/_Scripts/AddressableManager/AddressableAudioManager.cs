using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;

public sealed class AddressableAudioManager : Singleton<AddressableAudioManager>
{
    [Header("Addressable Library")]
    [SerializeField] private AssetReference _libraryReference; // 오디오 라이브러리 SO(Addressable) 참조

    private AddressableAudioLibrarySO _library;
    private AsyncOperationHandle<AddressableAudioLibrarySO> _libraryHandle;
    private bool _isLibraryLoading;

    private readonly Dictionary<int, AudioClip> _clipCache = new(); // id - AudioClip 캐시 구조
    private readonly Dictionary<int, AsyncOperationHandle<AudioClip>> _clipHandles = new(); // id - AudioClip 로드 핸들 캐시 구조
    private readonly Dictionary<int, List<Action<AudioClip>>> _pendingCallbacks = new(); // 같은 id 동시 요청 콜백 대기열

    public bool IsLibraryLoaded => _library != null; // 라이브러리 로드 상태

    // 싱글톤 초기화 시 라이브러리 선로딩
    protected override void OnSingletonAwake()
    {
        StartCoroutine(EnsureLibraryLoadedRoutine());
    }

    // 파괴 시 캐시/핸들 정리
    void OnDestroy()
    {
        ReleaseAllClips();
        ReleaseLibrary();
    }

    // id 기준 오디오 클립 비동기 로드
    public void LoadClip(int id, Action<AudioClip> onLoaded)
    {
        StartCoroutine(LoadClipRoutine(id, onLoaded));
    }

    // id 기준 오디오 클립 로드 코루틴
    public IEnumerator LoadClipRoutine(int id, Action<AudioClip> onLoaded)
    {
        if (id <= 0)
        {
            onLoaded?.Invoke(null);
            yield break;
        }

        if (_clipCache.TryGetValue(id, out AudioClip cachedClip) && cachedClip != null)
        {
            onLoaded?.Invoke(cachedClip);
            yield break;
        }

        if (_pendingCallbacks.TryGetValue(id, out List<Action<AudioClip>> pending))
        {
            pending.Add(onLoaded);
            yield break;
        }

        _pendingCallbacks[id] = new List<Action<AudioClip>>(1) { onLoaded };

        yield return EnsureLibraryLoadedRoutine();
        if (_library == null)
        {
            CompletePending(id, null);
            yield break;
        }

        if (!_library.TryGetClipReference(id, out AssetReference clipReference) ||
            clipReference == null ||
            !clipReference.RuntimeKeyIsValid())
        {
            Debug.LogWarning($"[AddressableAudioManager] Missing clip reference: {id}");
            CompletePending(id, null);
            yield break;
        }

        AsyncOperationHandle<AudioClip> loadHandle = clipReference.LoadAssetAsync<AudioClip>();
        yield return loadHandle;

        AudioClip loadedClip = null;

        if (loadHandle.Status == AsyncOperationStatus.Succeeded)
        {
            loadedClip = loadHandle.Result;
            _clipCache[id] = loadedClip;
            _clipHandles[id] = loadHandle;
        }
        else
        {
            Debug.LogError($"[AddressableAudioManager] Failed to load clip: {id}");
            if (loadHandle.IsValid())
                Addressables.Release(loadHandle);
        }

        CompletePending(id, loadedClip);
    }

    // 캐시에 이미 로드된 오디오 클립 조회
    public bool TryGetCachedClip(int id, out AudioClip clip)
    {
        if (id <= 0)
        {
            clip = null;
            return false;
        }

        return _clipCache.TryGetValue(id, out clip) && clip != null;
    }

    // 특정 id 오디오 클립만 캐시에서 해제
    public void ReleaseClip(int id)
    {
        if (id <= 0)
            return;

        ReleaseClipInternal(id);
    }

    // 로드한 모든 오디오 클립 캐시/핸들 해제
    public void ReleaseAllClips()
    {
        foreach (KeyValuePair<int, AsyncOperationHandle<AudioClip>> pair in _clipHandles)
        {
            AsyncOperationHandle<AudioClip> handle = pair.Value;
            if (handle.IsValid())
                Addressables.Release(handle);
        }

        _clipHandles.Clear();
        _clipCache.Clear();
        _pendingCallbacks.Clear();
    }

    // id 목록 오디오 클립을 미리 로드
    public void PreloadClips(IEnumerable<int> ids)
    {
        StartCoroutine(PreloadClipsRoutine(ids));
    }

    // id 목록 선로딩 코루틴
    public IEnumerator PreloadClipsRoutine(IEnumerable<int> ids)
    {
        if (ids == null)
            yield break;

        foreach (int id in ids)
            yield return LoadClipRoutine(id, null);
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
            Debug.LogError("[AddressableAudioManager] Library reference is not assigned.");
            yield break;
        }

        _isLibraryLoading = true;

        _libraryHandle = _libraryReference.LoadAssetAsync<AddressableAudioLibrarySO>();
        yield return _libraryHandle;

        if (_libraryHandle.Status == AsyncOperationStatus.Succeeded)
        {
            _library = _libraryHandle.Result;
        }
        else
        {
            Debug.LogError("[AddressableAudioManager] Failed to load audio library asset.");
            if (_libraryHandle.IsValid())
                Addressables.Release(_libraryHandle);
            _libraryHandle = default;
        }

        _isLibraryLoading = false;
    }

    // 같은 ID로 대기 중인 콜백들을 한 번에 완료
    private void CompletePending(int id, AudioClip clip)
    {
        if (!_pendingCallbacks.TryGetValue(id, out List<Action<AudioClip>> callbacks))
            return;

        _pendingCallbacks.Remove(id);

        for (int i = 0; i < callbacks.Count; i++)
            callbacks[i]?.Invoke(clip);
    }

    // 내부 공통 해제 로직
    private void ReleaseClipInternal(int id)
    {
        _clipCache.Remove(id);

        if (!_clipHandles.TryGetValue(id, out AsyncOperationHandle<AudioClip> handle))
            return;

        if (handle.IsValid())
            Addressables.Release(handle);

        _clipHandles.Remove(id);
    }
}
