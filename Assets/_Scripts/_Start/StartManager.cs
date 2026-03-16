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
    [SerializeField] private Slider _loadingSlider;
    [SerializeField] private TMP_Text _statusText;
    // 자동 동기화된 테이블 참조 목록
    [SerializeField] private TableLoadConfigSO _tableLoadConfig;

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
        var allTableRefs = CollectTableReferences();
        if (allTableRefs.Count == 0)
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
            progress = checkHandle.PercentComplete * 0.1f;
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
                    progress = 0.1f + (updateHandle.PercentComplete * 0.15f);
                    _loadingSlider.value = progress;
                    yield return null;
                }

                Addressables.Release(updateHandle);
            }
            else
            {
                _loadingSlider.value = 0.25f;
            }
        }

        Addressables.Release(checkHandle);

        _statusText.text = "Verifying resources...";

        long totalDownloadSize = 0;
        for (int i = 0; i < allTableRefs.Count; i++)
        {
            var sizeHandle = Addressables.GetDownloadSizeAsync(allTableRefs[i]);
            yield return sizeHandle;

            if (sizeHandle.Status == AsyncOperationStatus.Succeeded)
                totalDownloadSize += sizeHandle.Result;

            Addressables.Release(sizeHandle);

            progress = 0.25f + ((float)(i + 1) / allTableRefs.Count) * 0.1f;
            _loadingSlider.value = progress;
        }

        _loadingSlider.value = 0.35f;

        if (totalDownloadSize > 0)
        {
            _statusText.text = "Downloading game data...";

            for (int i = 0; i < allTableRefs.Count; i++)
            {
                var downloadHandle = Addressables.DownloadDependenciesAsync(allTableRefs[i]);
                while (!downloadHandle.IsDone)
                {
                    float tableProgress = downloadHandle.PercentComplete / Mathf.Max(1, allTableRefs.Count);
                    progress = 0.35f + ((i + tableProgress) / Mathf.Max(1, allTableRefs.Count)) * 0.2f;
                    _loadingSlider.value = progress;
                    yield return null;
                }

                Addressables.Release(downloadHandle);
            }
        }
        else
        {
            _loadingSlider.value = 0.55f;
        }

        _statusText.text = "Loading game data...";

        int tableCount = Mathf.Max(1, allTableRefs.Count);
        float tableStep = 0.3f / tableCount;
        float baseProgress = 0.55f;

        for (int i = 0; i < allTableRefs.Count; i++)
        {
            bool loadSucceeded = false;
            yield return LoadTable(allTableRefs[i], table =>
            {
                CachedSOData.RegisterTable(table);
                loadSucceeded = true;
            });

            if (!loadSucceeded)
            {
                FailLoading("Failed to load game data", $"[StartManager] Failed to load required table: {allTableRefs[i].AssetGUID}");
                yield break;
            }

            baseProgress += tableStep;
            _loadingSlider.value = Mathf.Min(baseProgress, 0.85f);
        }

        _loadingSlider.value = 0.85f;

        _statusText.text = "Initializing...";
        _loadingSlider.value = 0.9f;

        progress = 0.95f;
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
        var seenGuids = new HashSet<string>();

        if (_tableLoadConfig == null)
        {
            Debug.LogError("[StartManager] TableLoadConfig is not assigned.");
            return result;
        }

        AddTableRefs(result, seenGuids, _tableLoadConfig.TableRefs);

        return result;
    }

    // 유효한 Addressable 참조만 중복 없이 수집
    private static void AddTableRefs(List<AssetReference> result, HashSet<string> seenGuids, IEnumerable<AssetReference> source)
    {
        foreach (var tableRef in source)
        {
            if (tableRef == null) continue;
            if (string.IsNullOrEmpty(tableRef.AssetGUID)) continue;
            if (!tableRef.RuntimeKeyIsValid()) continue;
            if (!seenGuids.Add(tableRef.AssetGUID)) continue;
            result.Add(tableRef);
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
