using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class StartManager : MonoBehaviour
{
    private const float CatalogCheckEnd = 0.08f;
    private const float CatalogUpdateEnd = 0.18f;
    private const float VerifyResourcesEnd = 0.28f;
    private const float DownloadResourcesEnd = 0.80f;
    private const float TableLoadEnd = 0.95f;
    private const float InitStart = 0.96f;

    [SerializeField] private Slider _loadingSlider;
    [SerializeField] private TMP_Text _statusText;    
    [SerializeField] private TableLoadConfigSO _tableLoadConfig; // 자동 동기화된 테이블 참조 목록
    [SerializeField] private List<AssetReference> _additionalPreloadRefs = new(); // 테이블 외 선다운로드 대상(AddressableImageLibrary SO, AddressableAudioLibrary SO)

    private readonly WaitForSeconds _waitOneSecond = new(1f);

    void Awake()
    {
        Application.targetFrameRate = 60;
    }

    void Start()
    {
        StartCoroutine(LoadingProcess());
    }

    // 업데이트 확인 -> 다운로드 -> 테이블 로드 -> 캐시 등록까지 초기 로딩 전체 수행
    private IEnumerator LoadingProcess()
    {
        var tableRefs = CollectTableReferences();
        if (tableRefs.Count == 0)
        {
            FailLoading("Missing table config", "[StartManager] No table references found. Check TableLoadConfig.");
            yield break;
        }

        CachedSOData.Clear();

        _statusText.text = "Checking for updates...";

        var checkHandle = Addressables.CheckForCatalogUpdates(false);
        float progress;
        while (!checkHandle.IsDone)
        {
            progress = checkHandle.PercentComplete * CatalogCheckEnd;
            _loadingSlider.value = progress;
            yield return null;
        }

        if (checkHandle.Status == AsyncOperationStatus.Succeeded)
        {
            var catalogsToUpdate = checkHandle.Result;
            if (catalogsToUpdate.Count > 0)
            {
                _statusText.text = "Updating game data...";

                var updateHandle = Addressables.UpdateCatalogs(catalogsToUpdate, false);
                while (!updateHandle.IsDone)
                {
                    progress = CatalogCheckEnd + (updateHandle.PercentComplete * (CatalogUpdateEnd - CatalogCheckEnd));
                    _loadingSlider.value = progress;
                    yield return null;
                }

                Addressables.Release(updateHandle);
            }
            else
            {
                _loadingSlider.value = CatalogUpdateEnd;
            }
        }

        Addressables.Release(checkHandle);

        _statusText.text = "Preparing resources...";

        var additionalPreloadRefs = new List<AssetReference>();
        yield return CollectAdditionalPreloadReferencesRoutine(additionalPreloadRefs);

        _statusText.text = "Verifying resources...";

        long totalDownloadSize = 0;
        for (int i = 0; i < additionalPreloadRefs.Count; i++)
        {
            var sizeHandle = Addressables.GetDownloadSizeAsync(additionalPreloadRefs[i]);
            yield return sizeHandle;

            if (sizeHandle.Status == AsyncOperationStatus.Succeeded)
                totalDownloadSize += sizeHandle.Result;

            Addressables.Release(sizeHandle);

            progress = CatalogUpdateEnd + ((float)(i + 1) / additionalPreloadRefs.Count) * (VerifyResourcesEnd - CatalogUpdateEnd);
            _loadingSlider.value = progress;
        }

        _loadingSlider.value = VerifyResourcesEnd;

        if (totalDownloadSize > 0)
        {
            _statusText.text = "Downloading game data...";

            for (int i = 0; i < additionalPreloadRefs.Count; i++)
            {
                var downloadHandle = Addressables.DownloadDependenciesAsync(additionalPreloadRefs[i]);
                while (!downloadHandle.IsDone)
                {
                    float preloadProgress = downloadHandle.PercentComplete / Mathf.Max(1, additionalPreloadRefs.Count);
                    progress = VerifyResourcesEnd + ((i + preloadProgress) / Mathf.Max(1, additionalPreloadRefs.Count)) * (DownloadResourcesEnd - VerifyResourcesEnd);
                    _loadingSlider.value = progress;
                    yield return null;
                }

                Addressables.Release(downloadHandle);
            }
        }
        else
        {
            _loadingSlider.value = DownloadResourcesEnd;
        }

        _statusText.text = "Loading game data...";

        int tableCount = Mathf.Max(1, tableRefs.Count);
        float tableStep = (TableLoadEnd - DownloadResourcesEnd) / tableCount;
        float baseProgress = DownloadResourcesEnd;

        for (int i = 0; i < tableRefs.Count; i++)
        {
            bool loadSucceeded = false;
            yield return LoadTable(tableRefs[i], table =>
            {
                CachedSOData.RegisterTable(table);
                loadSucceeded = true;
            });

            if (!loadSucceeded)
            {
                FailLoading("Failed to load game data", $"[StartManager] Failed to load required table: {tableRefs[i].AssetGUID}");
                yield break;
            }

            baseProgress += tableStep;
            _loadingSlider.value = Mathf.Min(baseProgress, TableLoadEnd);
        }

        _loadingSlider.value = TableLoadEnd;

        _statusText.text = "Initializing...";
        _loadingSlider.value = InitStart;

        progress = InitStart;
        while (progress < 1f)
        {
            progress += Time.deltaTime * 0.5f;
            if (progress > 1f)
                progress = 1f;

            _loadingSlider.value = progress;
            yield return null;
        }

        _statusText.text = "Ready";

        yield return _waitOneSecond;
        SceneManager.LoadScene("Title");
    }

    // TableLoadConfig에서 유효한 Addressable 참조를 수집
    private List<AssetReference> CollectTableReferences()
    {
        var result = new List<AssetReference>();

        if (_tableLoadConfig == null)
        {
            Debug.LogError("[StartManager] TableLoadConfig is not assigned.");
            return result;
        }

        AddTableRefs(result, _tableLoadConfig.TableRefs);

        return result;
    }

    // 테이블 외 선다운로드 대상 Addressable 참조를 수집
    private IEnumerator CollectAdditionalPreloadReferencesRoutine(List<AssetReference> result)
    {
        if (result == null)
            yield break;

        AddPreloadRefs(result, _additionalPreloadRefs);

        for (int i = 0; i < _additionalPreloadRefs.Count; i++)
        {
            AssetReference preloadRef = _additionalPreloadRefs[i];
            if (preloadRef == null) continue;
            if (string.IsNullOrEmpty(preloadRef.AssetGUID)) continue;
            if (!preloadRef.RuntimeKeyIsValid()) continue;

            var loadHandle = preloadRef.LoadAssetAsync<ScriptableObject>();
            yield return loadHandle;

            if (loadHandle.Status == AsyncOperationStatus.Succeeded)
            {
                if (loadHandle.Result is AddressableImageLibrarySO imageLibrary)
                {
                    AddImageLibraryRefs(result, imageLibrary);
                }
                else if (loadHandle.Result is AddressableAudioLibrarySO audioLibrary)
                {
                    AddAudioLibraryRefs(result, audioLibrary);
                }
            }
            else
            {
                Debug.LogWarning($"[StartManager] Failed to load preload library: {preloadRef.AssetGUID}");
            }

            if (loadHandle.IsValid())
                Addressables.Release(loadHandle);
        }
    }

    // 유효한 Addressable 참조만 수집
    private static void AddTableRefs(List<AssetReference> result, IEnumerable<AssetReference> source)
    {
        if (source == null) return;

        foreach (var tableRef in source)
        {
            if (tableRef == null) continue;
            if (string.IsNullOrEmpty(tableRef.AssetGUID)) continue;
            if (!tableRef.RuntimeKeyIsValid()) continue;
            result.Add(tableRef);
        }
    }

    // 유효한 선다운로드 Addressable 참조만 수집
    private static void AddPreloadRefs(List<AssetReference> result, IEnumerable<AssetReference> source)
    {
        if (source == null) return;

        foreach (var preloadRef in source)
        {
            if (preloadRef == null) continue;
            if (string.IsNullOrEmpty(preloadRef.AssetGUID)) continue;
            if (!preloadRef.RuntimeKeyIsValid()) continue;
            result.Add(preloadRef);
        }
    }

    // 이미지 라이브러리 SO의 스프라이트 참조를 선다운로드 목록에 추가
    private static void AddImageLibraryRefs(List<AssetReference> result, AddressableImageLibrarySO library)
    {
        if (library == null) return;

        var entries = library.Entries;
        if (entries == null) return;

        for (int i = 0; i < entries.Count; i++)
        {
            var entry = entries[i];
            if (entry == null) continue;
            if (entry.spriteReference == null) continue;
            if (string.IsNullOrEmpty(entry.spriteReference.AssetGUID)) continue;
            if (!entry.spriteReference.RuntimeKeyIsValid()) continue;
            result.Add(entry.spriteReference);
        }
    }

    // 오디오 라이브러리 SO의 클립 참조를 선다운로드 목록에 추가
    private static void AddAudioLibraryRefs(List<AssetReference> result, AddressableAudioLibrarySO library)
    {
        if (library == null) return;

        var entries = library.Entries;
        if (entries == null) return;

        for (int i = 0; i < entries.Count; i++)
        {
            var entry = entries[i];
            if (entry == null) continue;
            if (entry.clipReference == null) continue;
            if (string.IsNullOrEmpty(entry.clipReference.AssetGUID)) continue;
            if (!entry.clipReference.RuntimeKeyIsValid()) continue;
            result.Add(entry.clipReference);
        }
    }

    // Addressables에서 ScriptableObject를 로드해 콜백으로 전달
    private IEnumerator LoadTable(AssetReference assetRef, System.Action<ScriptableObject> onLoaded)
    {
        var loadHandle = assetRef.LoadAssetAsync<ScriptableObject>();
        yield return loadHandle;

        if (loadHandle.Status == AsyncOperationStatus.Succeeded)
        {
            onLoaded?.Invoke(loadHandle.Result);
        }
        else
        {
            Debug.LogError($"[LoadTable] Failed to load {assetRef.AssetGUID}: {loadHandle.Status}");
        }
    }

    private void FailLoading(string statusMessage, string logMessage)
    {
        _statusText.text = statusMessage;
        _loadingSlider.value = 0f;
        Debug.LogError(logMessage);
    }
}
