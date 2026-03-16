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
    private const float TableLoadEnd = 0.93f;
    private const float ImagePreloadEnd = 0.96f;
    private const float InitStart = 0.97f;

    // 선다운로드 대상 1건의 메타 정보
    private sealed class PreloadAssetEntry
    {
        public AssetReference Reference;
        public string Category;
        public string DisplayName;
    }

    [SerializeField] private Slider _loadingSlider;
    [SerializeField] private TMP_Text _statusText;
    [SerializeField] private TableLoadConfigSO _tableLoadConfig; // 자동 동기화된 테이블 참조 목록
    [SerializeField] private List<AssetReference> _additionalPreloadRefs = new(); // 테이블 외 선다운로드 대상(AddressableImageLibrary SO, AddressableAudioLibrary SO)

    private readonly WaitForSeconds _waitOneSecond = new(1f);
    private readonly List<string> _preloadImageFileNames = new(); // 프리로드할 이미지 파일명 목록

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

        var additionalPreloadEntries = new List<PreloadAssetEntry>();
        yield return CollectAdditionalPreloadReferencesRoutine(additionalPreloadEntries);

        _statusText.text = "Verifying resources...";

        var downloadSizes = new List<long>(additionalPreloadEntries.Count);
        long totalDownloadSize = 0;

        if (additionalPreloadEntries.Count > 0)
        {
            for (int i = 0; i < additionalPreloadEntries.Count; i++)
            {
                PreloadAssetEntry entry = additionalPreloadEntries[i];
                _statusText.text = BuildVerifyStatusText(entry, i + 1, additionalPreloadEntries.Count);

                var sizeHandle = Addressables.GetDownloadSizeAsync(entry.Reference);
                yield return sizeHandle;

                long size = 0;
                if (sizeHandle.Status == AsyncOperationStatus.Succeeded)
                {
                    size = sizeHandle.Result;
                    totalDownloadSize += size;
                }

                downloadSizes.Add(size);
                Addressables.Release(sizeHandle);

                progress = CatalogUpdateEnd + ((float)(i + 1) / additionalPreloadEntries.Count) * (VerifyResourcesEnd - CatalogUpdateEnd);
                _loadingSlider.value = progress;
            }
        }

        _loadingSlider.value = VerifyResourcesEnd;

        if (totalDownloadSize > 0)
        {
            long completedDownloadedBytes = 0;

            for (int i = 0; i < additionalPreloadEntries.Count; i++)
            {
                PreloadAssetEntry entry = additionalPreloadEntries[i];
                long sizeForEntry = i < downloadSizes.Count ? downloadSizes[i] : 0;

                var downloadHandle = Addressables.DownloadDependenciesAsync(entry.Reference);
                while (!downloadHandle.IsDone)
                {
                    var downloadStatus = downloadHandle.GetDownloadStatus();
                    long currentDownloadedBytes = (long)downloadStatus.DownloadedBytes;
                    long totalDownloadedBytes = completedDownloadedBytes + currentDownloadedBytes;
                    if (totalDownloadedBytes > totalDownloadSize)
                        totalDownloadedBytes = totalDownloadSize;

                    float downloadRatio = (float)totalDownloadedBytes / totalDownloadSize;
                    progress = VerifyResourcesEnd + downloadRatio * (DownloadResourcesEnd - VerifyResourcesEnd);
                    _loadingSlider.value = progress;
                    _statusText.text = BuildDownloadStatusText(entry, i + 1, additionalPreloadEntries.Count, totalDownloadedBytes, totalDownloadSize);
                    yield return null;
                }

                var finalStatus = downloadHandle.GetDownloadStatus();
                long downloadedForEntry = sizeForEntry;
                if ((long)finalStatus.DownloadedBytes > downloadedForEntry)
                    downloadedForEntry = (long)finalStatus.DownloadedBytes;

                completedDownloadedBytes += downloadedForEntry;
                if (completedDownloadedBytes > totalDownloadSize)
                    completedDownloadedBytes = totalDownloadSize;

                if (downloadHandle.IsValid())
                    Addressables.Release(downloadHandle);
            }
        }

        _loadingSlider.value = DownloadResourcesEnd;

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

        // 이미지 라이브러리 SO의 모든 스프라이트를 AddressableImageManager 캐시에 미리 로드
        _statusText.text = "Loading images...";
        yield return PreloadAllImagesRoutine();
        _loadingSlider.value = ImagePreloadEnd;

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

    // 수집된 파일명으로 AddressableImageManager 캐시에 병렬 로드 후 고정 등록
    private IEnumerator PreloadAllImagesRoutine()
    {
        if (AddressableImageManager.Instance == null)
            yield break;

        if (_preloadImageFileNames.Count == 0)
            yield break;

        // 모든 이미지를 병렬로 로드 시작
        int total = _preloadImageFileNames.Count;
        int completed = 0;

        for (int i = 0; i < total; i++)
        {
            string fileName = _preloadImageFileNames[i];
            AddressableImageManager.Instance.LoadSprite(fileName, _ => completed++);
        }

        // 전부 완료될 때까지 대기
        while (completed < total)
            yield return null;

        // 프리로드된 이미지는 고정 캐시로 등록해 해제 방지
        foreach (string fileName in _preloadImageFileNames)
            AddressableImageManager.Instance.PinSprite(fileName);
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
    private IEnumerator CollectAdditionalPreloadReferencesRoutine(List<PreloadAssetEntry> result)
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

                    // 이미지 파일명 목록을 미리 수집해 PreloadAllImagesRoutine에서 재사용
                    foreach (var entry in imageLibrary.Entries)
                    {
                        if (entry != null && !string.IsNullOrEmpty(entry.fileName))
                            _preloadImageFileNames.Add(entry.fileName);
                    }
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
    private static void AddPreloadRefs(List<PreloadAssetEntry> result, IEnumerable<AssetReference> source)
    {
        if (source == null) return;

        foreach (var preloadRef in source)
        {
            if (preloadRef == null) continue;
            if (string.IsNullOrEmpty(preloadRef.AssetGUID)) continue;
            if (!preloadRef.RuntimeKeyIsValid()) continue;
            result.Add(new PreloadAssetEntry
            {
                Reference = preloadRef,
                Category = "library",
                DisplayName = preloadRef.AssetGUID
            });
        }
    }

    // 이미지 라이브러리 SO의 스프라이트 참조를 선다운로드 목록에 추가
    private static void AddImageLibraryRefs(List<PreloadAssetEntry> result, AddressableImageLibrarySO library)
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
            result.Add(new PreloadAssetEntry
            {
                Reference = entry.spriteReference,
                Category = "image",
                DisplayName = entry.fileName
            });
        }
    }

    // 오디오 라이브러리 SO의 클립 참조를 선다운로드 목록에 추가
    private static void AddAudioLibraryRefs(List<PreloadAssetEntry> result, AddressableAudioLibrarySO library)
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
            result.Add(new PreloadAssetEntry
            {
                Reference = entry.clipReference,
                Category = "audio",
                DisplayName = entry.name
            });
        }
    }

    // 검증 단계 상태 텍스트 구성
    private static string BuildVerifyStatusText(PreloadAssetEntry entry, int index, int total)
    {
        return $"Verifying {GetEntryLabel(entry)} ({index}/{total})";
    }

    // 다운로드 단계 상태 텍스트 구성
    private static string BuildDownloadStatusText(PreloadAssetEntry entry, int index, int total, long downloadedBytes, long totalBytes)
    {
        float percent = totalBytes > 0 ? (float)downloadedBytes / totalBytes * 100f : 100f;
        float downloadedMb = downloadedBytes / (1024f * 1024f);
        float totalMb = totalBytes / (1024f * 1024f);
        return $"Downloading {GetEntryLabel(entry)} ({index}/{total}) {percent:0.0}% ({downloadedMb:0.0}/{totalMb:0.0} MB)";
    }

    // 상태 텍스트용 항목 이름 반환
    private static string GetEntryLabel(PreloadAssetEntry entry)
    {
        if (entry == null)
            return "resource";

        string category = "library";
        if (entry.Category == "image")
            category = "image";
        else if (entry.Category == "audio")
            category = "audio";

        if (string.IsNullOrWhiteSpace(entry.DisplayName))
            return category;

        return $"{category}:{entry.DisplayName}";
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