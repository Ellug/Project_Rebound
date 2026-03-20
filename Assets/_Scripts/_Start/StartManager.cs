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
    // 로딩 단계별 진행률 구간
    private const float CatalogCheckEnd = 0.05f;
    private const float CatalogUpdateEnd = 0.12f;
    private const float PrepareResourcesEnd = 0.32f;
    private const float VerifyResourcesEnd = 0.44f;
    private const float DownloadResourcesEnd = 0.84f;
    private const float TableLoadEnd = 0.95f;
    private const float ImagePreloadEnd = 0.98f;
    private const float InitStart = 0.985f;
    private const float PrepareUiLerpSpeed = 0.08f; // Preparing resources 구간의 페이크 진행 보간 속도

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
    [SerializeField] private StartLegalConsentPopup _legalConsentPopup;

    private readonly WaitForSeconds _waitOneSecond = new(1f);
    private readonly List<string> _preloadImageFileNames = new(); // 프리로드할 이미지 파일명 목록
    private bool _loadingStarted;
    private bool _legalPopupEventsBound;

    void Awake()
    {
        Application.targetFrameRate = 60;
    }

    void Start()
    {
        if (StartLegalConsentPopup.HasConsent())
        {
            StartLoading();
            return;
        }

        BindLegalPopupEvents();
        _legalConsentPopup.Show();
    }

    void OnDestroy()
    {
        UnbindLegalPopupEvents();
    }

    private void BindLegalPopupEvents()
    {
        if (_legalPopupEventsBound)
            return;

        _legalConsentPopup.OnCancel += HandleLegalConsentCancel;
        _legalConsentPopup.OnAgree += HandleLegalConsentAgree;
        _legalPopupEventsBound = true;
    }

    private void UnbindLegalPopupEvents()
    {
        if (!_legalPopupEventsBound)
            return;

        _legalConsentPopup.OnCancel -= HandleLegalConsentCancel;
        _legalConsentPopup.OnAgree -= HandleLegalConsentAgree;
        _legalPopupEventsBound = false;
    }

    private void HandleLegalConsentCancel()
    {
        StartLegalConsentPopup.SaveConsent(false);

#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    private void HandleLegalConsentAgree()
    {
        StartLegalConsentPopup.SaveConsent(true);
        _legalConsentPopup.Hide();
        StartLoading();
    }

    private void StartLoading()
    {
        if (_loadingStarted)
            return;

        _loadingStarted = true;
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

        // 개별 호출(다건 순차) 대신, Addressables 다건 API에 넘길 키 목록을 한 번 구성
        // 이렇게 하면 GetDownloadSize/DownloadDependencies 호출 횟수가 크게 줄어듦
        List<object> preloadKeys = BuildPreloadKeys(additionalPreloadEntries);
        long totalDownloadSize = 0;

        if (preloadKeys.Count > 0)
        {
            // 전체 키를 한 번에 조회해 "다운로드 필요 총량"만 계산
            var sizeHandle = Addressables.GetDownloadSizeAsync(preloadKeys);
            float verifyUiRatio = 0f;

            while (!sizeHandle.IsDone)
            {
                // size 조회는 초반에 정체처럼 보일 수 있어, 실제값을 기준으로 하되 최소한의 보간을 섞어 UI 진행률이 멈춘 것처럼 보이지 않게
                float actualRatio = Mathf.Clamp01(sizeHandle.PercentComplete);
                verifyUiRatio = Mathf.Max(verifyUiRatio, actualRatio);
                verifyUiRatio = Mathf.Min(0.98f, verifyUiRatio + Time.unscaledDeltaTime * 0.35f);

                progress = PrepareResourcesEnd + verifyUiRatio * (VerifyResourcesEnd - PrepareResourcesEnd);
                _loadingSlider.value = progress;
                _statusText.text = BuildVerifyBatchStatusText(
                    totalImageCount,
                    totalAudioCount,
                    totalLibraryCount,
                    verifyUiRatio);
                yield return null;
            }

            if (sizeHandle.Status == AsyncOperationStatus.Succeeded)
                totalDownloadSize = sizeHandle.Result > 0 ? sizeHandle.Result : 0;

            if (sizeHandle.IsValid())
                Addressables.Release(sizeHandle);
        }

        _loadingSlider.value = VerifyResourcesEnd;

        // totalDownloadSize == 0이면 이미 캐시가 모두 유효한 상태라 다운로드 스킵
        if (totalDownloadSize > 0 && preloadKeys.Count > 0)
        {
            // 모든 키를 Union으로 묶어 한 번에 다운로드 (중복 의존성은 Addressables가 내부에서 정리)
            var downloadHandle = Addressables.DownloadDependenciesAsync(preloadKeys, Addressables.MergeMode.Union, false);
            float displayedDownloadRatio = 0f;

            while (!downloadHandle.IsDone)
            {
                var downloadStatus = downloadHandle.GetDownloadStatus();
                long downloadedTotalBytes = ClampDownloadedBytes((long)downloadStatus.DownloadedBytes, totalDownloadSize);

                float actualRatio = totalDownloadSize > 0
                    ? Mathf.Clamp01((float)downloadedTotalBytes / totalDownloadSize)
                    : Mathf.Clamp01(downloadStatus.Percent);

                // 진행률 역주행 방지 + 완료 직전(99.5%)까지만 보간 표시 후 완료 시점에 100%를 찍어 체감상 끊김을 줄임
                displayedDownloadRatio = Mathf.Max(displayedDownloadRatio, actualRatio);
                displayedDownloadRatio = Mathf.Min(0.995f, displayedDownloadRatio + Time.unscaledDeltaTime * 0.2f);

                progress = VerifyResourcesEnd + displayedDownloadRatio * (DownloadResourcesEnd - VerifyResourcesEnd);
                _loadingSlider.value = progress;
                _statusText.text = BuildDownloadBatchStatusText(
                    totalImageCount,
                    totalAudioCount,
                    totalLibraryCount,
                    downloadedTotalBytes,
                    totalDownloadSize,
                    displayedDownloadRatio);
                yield return null;
            }

            _statusText.text = BuildDownloadBatchStatusText(
                totalImageCount,
                totalAudioCount,
                totalLibraryCount,
                totalDownloadSize,
                totalDownloadSize,
                1f);

            if (downloadHandle.IsValid())
                Addressables.Release(downloadHandle);
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
        float displayedPrepareRatio = 0f;
        _statusText.text = BuildPrepareSummaryStatusText(loadedLibraryCount, totalLibraryCount, result.Count, displayedPrepareRatio);

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
                float targetPrepareRatio = ((float)loadedLibraryCount + currentLibraryProgress) / totalLibraryCount;

                // 실제 진행률을 기준으로 하되, 미세 보간을 더해 정체처럼 보이는 구간을 완화한다.
                displayedPrepareRatio = Mathf.Max(displayedPrepareRatio, targetPrepareRatio);
                displayedPrepareRatio = Mathf.Min(0.985f, displayedPrepareRatio + Time.unscaledDeltaTime * PrepareUiLerpSpeed);

                _loadingSlider.value = CatalogUpdateEnd + displayedPrepareRatio * (PrepareResourcesEnd - CatalogUpdateEnd);
                _statusText.text = BuildPrepareSummaryStatusText(loadedLibraryCount, totalLibraryCount, result.Count, displayedPrepareRatio);
                yield return null;
            }

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

            loadedLibraryCount++;
            float completedRatio = (float)loadedLibraryCount / totalLibraryCount;
            displayedPrepareRatio = Mathf.Max(displayedPrepareRatio, completedRatio);
            _loadingSlider.value = CatalogUpdateEnd + displayedPrepareRatio * (PrepareResourcesEnd - CatalogUpdateEnd);
            _statusText.text = BuildPrepareSummaryStatusText(loadedLibraryCount, totalLibraryCount, result.Count, displayedPrepareRatio);

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
    private static void CountPreloadCategories(List<PreloadAssetEntry> entries, out int imageCount, out int audioCount, out int libraryCount)
    {
        imageCount = 0;
        audioCount = 0;
        libraryCount = 0;

        if (entries == null) return;

        for (int i = 0; i < entries.Count; i++)
        {
            string category = NormalizePreloadCategory(entries[i]?.Category);
            IncrementCategoryCount(category, ref imageCount, ref audioCount, ref libraryCount);
        }
    }

    // 선다운로드 대상 참조를 Addressables 다건 API에 전달할 키 목록으로 변환
    // - GUID 기준으로 중복 제거
    // - RuntimeKey가 유효한 참조만 사용
    private static List<object> BuildPreloadKeys(List<PreloadAssetEntry> entries)
    {
        var result = new List<object>();
        if (entries == null || entries.Count == 0)
            return result;

        var addedGuids = new HashSet<string>();

        for (int i = 0; i < entries.Count; i++)
        {
            AssetReference reference = entries[i]?.Reference;
            if (reference == null)
                continue;

            string guid = reference.AssetGUID;
            if (string.IsNullOrEmpty(guid))
                continue;
            if (!reference.RuntimeKeyIsValid())
                continue;
            if (!addedGuids.Add(guid))
                continue;

            object runtimeKey = reference.RuntimeKey;
            if (runtimeKey == null)
                continue;

            result.Add(runtimeKey);
        }

        return result;
    }

    // 카테고리 문자열을 고정 키로 정규화
    private static string NormalizePreloadCategory(string category)
    {
        if (category == "image") return "image";
        if (category == "audio") return "audio";
        return "library";
    }

    // 카테고리 카운트 증가
    private static void IncrementCategoryCount(string category, ref int imageCount, ref int audioCount, ref int libraryCount)
    {
        if (category == "image")        imageCount++;
        else if (category == "audio")   audioCount++;
        else                            libraryCount++;
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

    // 묶음 검증 단계 상태 텍스트 구성
    private static string BuildVerifyBatchStatusText(int totalImageCount, int totalAudioCount, int totalLibraryCount, float verifyRatio)
    {
        int totalCount = totalImageCount + totalAudioCount + totalLibraryCount;
        float percent = Mathf.Clamp01(verifyRatio) * 100f;
        return $"Verifying resources... {percent:0.0}% | refs {totalCount} (img {totalImageCount}, aud {totalAudioCount}, lib {totalLibraryCount})";
    }

    // 묶음 다운로드 단계 상태 텍스트 구성
    private static string BuildDownloadBatchStatusText(
        int totalImageCount,
        int totalAudioCount,
        int totalLibraryCount,
        long downloadedTotalBytes,
        long totalBytes,
        float downloadRatio)
    {
        int totalCount = totalImageCount + totalAudioCount + totalLibraryCount;
        int downloadedCount = totalCount <= 0
            ? 0
            : Mathf.Clamp(Mathf.RoundToInt(Mathf.Clamp01(downloadRatio) * totalCount), 0, totalCount);
        float percent = Mathf.Clamp01(downloadRatio) * 100f;
        return $"Downloading resources... {percent:0.0}% {downloadedCount}/{totalCount} | img {totalImageCount}, aud {totalAudioCount}, lib {totalLibraryCount} | total {ToMb(downloadedTotalBytes):0.0}/{ToMb(totalBytes):0.0}MB";
    }

    // 준비 단계 상태 텍스트 구성
    private static string BuildPrepareSummaryStatusText(int loadedLibraryCount, int totalLibraryCount, int collectedRefCount, float prepareRatio)
    {
        if (totalLibraryCount <= 0)
            return $"Preparing resources... 100.0% | lib 0/0 | refs {collectedRefCount}";

        float percent = Mathf.Clamp01(prepareRatio) * 100f;

        return $"Preparing resources... {percent:0.0}% | lib {loadedLibraryCount}/{totalLibraryCount} | refs {collectedRefCount}";
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
