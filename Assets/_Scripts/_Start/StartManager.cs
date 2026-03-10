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
            _statusText.text = "Missing table config";
            Debug.LogError("[StartManager] No table references found. Check TableLoadConfig.");
            yield break;
        }

        CachedSOData.Clear();

        _statusText.text = "Checking for updates...";

        var checkHandle = Addressables.CheckForCatalogUpdates(false);
        float progress;
        while (!checkHandle.IsDone)
        {
            progress = checkHandle.PercentComplete * 0.3f;
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
                    progress = 0.3f + (updateHandle.PercentComplete * 0.3f);
                    _loadingSlider.value = progress;
                    yield return null;
                }

                Addressables.Release(updateHandle);
            }
            else
            {
                _loadingSlider.value = 0.6f;
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
        }

        _loadingSlider.value = 0.7f;

        if (totalDownloadSize > 0)
        {
            _statusText.text = "Downloading game data...";

            for (int i = 0; i < allTableRefs.Count; i++)
            {
                var downloadHandle = Addressables.DownloadDependenciesAsync(allTableRefs[i]);
                while (!downloadHandle.IsDone)
                {
                    float tableProgress = downloadHandle.PercentComplete / Mathf.Max(1, allTableRefs.Count);
                    progress = 0.7f + ((i + tableProgress) / Mathf.Max(1, allTableRefs.Count)) * 0.1f;
                    _loadingSlider.value = progress;
                    yield return null;
                }

                Addressables.Release(downloadHandle);
            }
        }
        else
        {
            _loadingSlider.value = 0.8f;
        }

        _statusText.text = "Loading game data...";

        int tableCount = Mathf.Max(1, allTableRefs.Count);
        float tableStep = 0.1f / tableCount;
        float baseProgress = 0.8f;

        for (int i = 0; i < allTableRefs.Count; i++)
        {
            yield return LoadTable(allTableRefs[i], CachedSOData.RegisterTable);
            baseProgress += tableStep;
            _loadingSlider.value = Mathf.Min(baseProgress, 0.9f);
        }

        _loadingSlider.value = 0.9f;

        _statusText.text = "Initializing...";
        _loadingSlider.value = 0.95f;

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
            _tableLoadConfig = Resources.Load<TableLoadConfigSO>("TableLoadConfig");

        if (_tableLoadConfig == null)
        {
            Debug.LogError("[StartManager] TableLoadConfig not found.");
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
}
