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
    private const float PrepareResourcesEnd = 0.26f;
    private const float VerifyResourcesEnd = 0.34f;
    private const float DownloadResourcesEnd = 0.80f;
    private const float TableLoadEnd = 0.95f;
    private const float InitStart = 0.96f;

    // 선다운로드 대상 1건의 메타 정보
    private sealed class PreloadAssetEntry
    {
        public AssetReference Reference;
        public string Category;
    }

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

        var additionalPreloadEntries = new List<PreloadAssetEntry>();
        yield return CollectAdditionalPreloadReferencesRoutine(additionalPreloadEntries);
        _loadingSlider.value = PrepareResourcesEnd;

        _statusText.text = "Verifying resources...";

        CountPreloadCategories(
            additionalPreloadEntries,
            out int totalImageCount,
            out int totalAudioCount,
            out int totalLibraryCount);

        var downloadSizes = new List<long>(additionalPreloadEntries.Count);
        long totalDownloadSize = 0;
        int verifiedImageCount = 0;
        int verifiedAudioCount = 0;
        int verifiedLibraryCount = 0;
        long verifiedImageBytes = 0;
        long verifiedAudioBytes = 0;
        long verifiedLibraryBytes = 0;

        if (additionalPreloadEntries.Count > 0)
        {
            for (int i = 0; i < additionalPreloadEntries.Count; i++)
            {
                PreloadAssetEntry entry = additionalPreloadEntries[i];
                string category = NormalizePreloadCategory(entry.Category);

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

                IncrementCategoryCount(
                    category,
                    ref verifiedImageCount,
                    ref verifiedAudioCount,
                    ref verifiedLibraryCount);
                AddCategoryBytes(
                    category,
                    size,
                    ref verifiedImageBytes,
                    ref verifiedAudioBytes,
                    ref verifiedLibraryBytes);

                _statusText.text = BuildVerifySummaryStatusText(
                    totalImageCount,
                    totalAudioCount,
                    totalLibraryCount,
                    verifiedImageCount,
                    verifiedAudioCount,
                    verifiedLibraryCount,
                    verifiedImageBytes,
                    verifiedAudioBytes,
                    verifiedLibraryBytes);

                progress = PrepareResourcesEnd + ((float)(i + 1) / additionalPreloadEntries.Count) * (VerifyResourcesEnd - PrepareResourcesEnd);
                _loadingSlider.value = progress;
            }
        }

        _loadingSlider.value = VerifyResourcesEnd;

        if (totalDownloadSize > 0)
        {
            long completedDownloadedBytes = 0;
            int downloadedImageCount = 0;
            int downloadedAudioCount = 0;
            int downloadedLibraryCount = 0;
            long downloadedImageBytes = 0;
            long downloadedAudioBytes = 0;
            long downloadedLibraryBytes = 0;

            for (int i = 0; i < additionalPreloadEntries.Count; i++)
            {
                PreloadAssetEntry entry = additionalPreloadEntries[i];
                long sizeForEntry = i < downloadSizes.Count ? downloadSizes[i] : 0;
                string category = NormalizePreloadCategory(entry.Category);

                var downloadHandle = Addressables.DownloadDependenciesAsync(entry.Reference);
                while (!downloadHandle.IsDone)
                {
                    var downloadStatus = downloadHandle.GetDownloadStatus();
                    long currentDownloadedBytes = ClampDownloadedBytes((long)downloadStatus.DownloadedBytes, sizeForEntry);
                    long totalDownloadedBytes = completedDownloadedBytes + currentDownloadedBytes;
                    if (totalDownloadedBytes > totalDownloadSize)
                        totalDownloadedBytes = totalDownloadSize;

                    long currentImageBytes = downloadedImageBytes;
                    long currentAudioBytes = downloadedAudioBytes;
                    long currentLibraryBytes = downloadedLibraryBytes;
                    AddCategoryBytes(
                        category,
                        currentDownloadedBytes,
                        ref currentImageBytes,
                        ref currentAudioBytes,
                        ref currentLibraryBytes);

                    float downloadRatio = (float)totalDownloadedBytes / totalDownloadSize;
                    progress = VerifyResourcesEnd + downloadRatio * (DownloadResourcesEnd - VerifyResourcesEnd);
                    _loadingSlider.value = progress;
                    _statusText.text = BuildDownloadSummaryStatusText(
                        totalImageCount,
                        totalAudioCount,
                        totalLibraryCount,
                        downloadedImageCount,
                        downloadedAudioCount,
                        downloadedLibraryCount,
                        currentImageBytes,
                        currentAudioBytes,
                        currentLibraryBytes,
                        totalDownloadedBytes,
                        totalDownloadSize);
                    yield return null;
                }

                var finalStatus = downloadHandle.GetDownloadStatus();
                long downloadedForEntry = ClampDownloadedBytes((long)finalStatus.DownloadedBytes, sizeForEntry);
                if (downloadedForEntry < sizeForEntry)
                    downloadedForEntry = sizeForEntry;

                completedDownloadedBytes += downloadedForEntry;
                if (completedDownloadedBytes > totalDownloadSize)
                    completedDownloadedBytes = totalDownloadSize;

                IncrementCategoryCount(
                    category,
                    ref downloadedImageCount,
                    ref downloadedAudioCount,
                    ref downloadedLibraryCount);
                AddCategoryBytes(
                    category,
                    downloadedForEntry,
                    ref downloadedImageBytes,
                    ref downloadedAudioBytes,
                    ref downloadedLibraryBytes);

                float completedRatio = (float)completedDownloadedBytes / totalDownloadSize;
                _loadingSlider.value = VerifyResourcesEnd + completedRatio * (DownloadResourcesEnd - VerifyResourcesEnd);
                _statusText.text = BuildDownloadSummaryStatusText(
                    totalImageCount,
                    totalAudioCount,
                    totalLibraryCount,
                    downloadedImageCount,
                    downloadedAudioCount,
                    downloadedLibraryCount,
                    downloadedImageBytes,
                    downloadedAudioBytes,
                    downloadedLibraryBytes,
                    completedDownloadedBytes,
                    totalDownloadSize);

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
    private IEnumerator CollectAdditionalPreloadReferencesRoutine(List<PreloadAssetEntry> result)
    {
        if (result == null)
            yield break;

        var validPreloadRefs = new List<AssetReference>();
        for (int i = 0; i < _additionalPreloadRefs.Count; i++)
        {
            AssetReference preloadRef = _additionalPreloadRefs[i];
            if (preloadRef == null) continue;
            if (string.IsNullOrEmpty(preloadRef.AssetGUID)) continue;
            if (!preloadRef.RuntimeKeyIsValid()) continue;
            validPreloadRefs.Add(preloadRef);
        }

        AddPreloadRefs(result, validPreloadRefs);

        int totalLibraryCount = validPreloadRefs.Count;
        int loadedLibraryCount = 0;
        _statusText.text = BuildPrepareSummaryStatusText(loadedLibraryCount, totalLibraryCount, result.Count, 0f);

        if (totalLibraryCount == 0)
        {
            _loadingSlider.value = PrepareResourcesEnd;
            yield break;
        }

        for (int i = 0; i < validPreloadRefs.Count; i++)
        {
            AssetReference preloadRef = validPreloadRefs[i];

            var loadHandle = preloadRef.LoadAssetAsync<ScriptableObject>();
            while (!loadHandle.IsDone)
            {
                float currentLibraryProgress = Mathf.Clamp01(loadHandle.PercentComplete);
                float prepareRatio = ((float)loadedLibraryCount + currentLibraryProgress) / totalLibraryCount;
                _loadingSlider.value = CatalogUpdateEnd + prepareRatio * (PrepareResourcesEnd - CatalogUpdateEnd);
                _statusText.text = BuildPrepareSummaryStatusText(loadedLibraryCount, totalLibraryCount, result.Count, currentLibraryProgress);
                yield return null;
            }

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

            loadedLibraryCount++;
            float completedRatio = (float)loadedLibraryCount / totalLibraryCount;
            _loadingSlider.value = CatalogUpdateEnd + completedRatio * (PrepareResourcesEnd - CatalogUpdateEnd);
            _statusText.text = BuildPrepareSummaryStatusText(loadedLibraryCount, totalLibraryCount, result.Count, 0f);

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
                Category = "library"
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
                Category = "image"
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
                Category = "audio"
            });
        }
    }

    // 선다운로드 목록의 카테고리별 개수를 집계
    private static void CountPreloadCategories(
        List<PreloadAssetEntry> entries,
        out int imageCount,
        out int audioCount,
        out int libraryCount)
    {
        imageCount = 0;
        audioCount = 0;
        libraryCount = 0;

        if (entries == null)
            return;

        for (int i = 0; i < entries.Count; i++)
        {
            string category = NormalizePreloadCategory(entries[i]?.Category);
            IncrementCategoryCount(category, ref imageCount, ref audioCount, ref libraryCount);
        }
    }

    // 카테고리 문자열을 고정 키로 정규화
    private static string NormalizePreloadCategory(string category)
    {
        if (category == "image")
            return "image";
        if (category == "audio")
            return "audio";
        return "library";
    }

    // 카테고리 카운트 증가
    private static void IncrementCategoryCount(
        string category,
        ref int imageCount,
        ref int audioCount,
        ref int libraryCount)
    {
        if (category == "image")
            imageCount++;
        else if (category == "audio")
            audioCount++;
        else
            libraryCount++;
    }

    // 카테고리 바이트 누적
    private static void AddCategoryBytes(
        string category,
        long bytes,
        ref long imageBytes,
        ref long audioBytes,
        ref long libraryBytes)
    {
        if (category == "image")
            imageBytes += bytes;
        else if (category == "audio")
            audioBytes += bytes;
        else
            libraryBytes += bytes;
    }

    // 다운로드 상태의 바이트 값을 예상 용량 범위로 보정
    private static long ClampDownloadedBytes(long downloadedBytes, long expectedBytes)
    {
        if (downloadedBytes < 0)
            return 0;
        if (expectedBytes > 0 && downloadedBytes > expectedBytes)
            return expectedBytes;
        return downloadedBytes;
    }

    // 검증 단계 상태 텍스트 구성
    private static string BuildVerifySummaryStatusText(
        int totalImageCount,
        int totalAudioCount,
        int totalLibraryCount,
        int verifiedImageCount,
        int verifiedAudioCount,
        int verifiedLibraryCount,
        long verifiedImageBytes,
        long verifiedAudioBytes,
        long verifiedLibraryBytes)
    {
        int totalCount = totalImageCount + totalAudioCount + totalLibraryCount;
        int verifiedCount = verifiedImageCount + verifiedAudioCount + verifiedLibraryCount;
        return $"Verifying resources... {verifiedCount}/{totalCount} | img {verifiedImageCount}/{totalImageCount} {ToMb(verifiedImageBytes):0.0}MB, aud {verifiedAudioCount}/{totalAudioCount} {ToMb(verifiedAudioBytes):0.0}MB, lib {verifiedLibraryCount}/{totalLibraryCount} {ToMb(verifiedLibraryBytes):0.0}MB";
    }

    // 준비 단계 상태 텍스트 구성
    private static string BuildPrepareSummaryStatusText(
        int loadedLibraryCount,
        int totalLibraryCount,
        int collectedRefCount,
        float currentLibraryProgress)
    {
        if (totalLibraryCount <= 0)
            return $"Preparing resources... 100.0% | lib 0/0 | refs {collectedRefCount}";

        float inFlightProgress = loadedLibraryCount < totalLibraryCount ? Mathf.Clamp01(currentLibraryProgress) : 0f;
        float percent = ((loadedLibraryCount + inFlightProgress) / totalLibraryCount) * 100f;
        if (percent > 100f)
            percent = 100f;

        return $"Preparing resources... {percent:0.0}% | lib {loadedLibraryCount}/{totalLibraryCount} | refs {collectedRefCount}";
    }

    // 다운로드 단계 상태 텍스트 구성
    private static string BuildDownloadSummaryStatusText(
        int totalImageCount,
        int totalAudioCount,
        int totalLibraryCount,
        int downloadedImageCount,
        int downloadedAudioCount,
        int downloadedLibraryCount,
        long downloadedImageBytes,
        long downloadedAudioBytes,
        long downloadedLibraryBytes,
        long downloadedTotalBytes,
        long totalBytes)
    {
        int totalCount = totalImageCount + totalAudioCount + totalLibraryCount;
        int downloadedCount = downloadedImageCount + downloadedAudioCount + downloadedLibraryCount;
        float percent = totalBytes > 0 ? (float)downloadedTotalBytes / totalBytes * 100f : 100f;
        return $"Downloading resources... {percent:0.0}% {downloadedCount}/{totalCount} | img {downloadedImageCount}/{totalImageCount} {ToMb(downloadedImageBytes):0.0}MB, aud {downloadedAudioCount}/{totalAudioCount} {ToMb(downloadedAudioBytes):0.0}MB, lib {downloadedLibraryCount}/{totalLibraryCount} {ToMb(downloadedLibraryBytes):0.0}MB | total {ToMb(downloadedTotalBytes):0.0}/{ToMb(totalBytes):0.0}MB";
    }

    // 바이트를 MB(float)로 변환
    private static float ToMb(long bytes)
    {
        return bytes / (1024f * 1024f);
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
