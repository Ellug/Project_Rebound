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

    [Header("Game Data Tables")]
    [SerializeField] private AssetReference _growthCommandTableRef;
    [SerializeField] private AssetReference _suddenEventTableRef;
    [SerializeField] private AssetReference _suddenEventEffectTableRef;
    [SerializeField] private AssetReference _suddenEventTextTableRef;
    [SerializeField] private AssetReference _statusTextTableRef;

    // 로드된 테이블들 (나중에 DataManager로 옮길 예정)
    private static readonly Dictionary<System.Type, ScriptableObject> _loadedTables = new();

    private readonly WaitForSeconds _waitOneSecond = new(1f);

    void Start()
    {
        StartCoroutine(LoadingProcess());
    }

    private IEnumerator LoadingProcess()
    {
        // 테이블 레퍼런스 수집
        var allTableRefs = new List<AssetReference>
        {
            _growthCommandTableRef,
            _suddenEventTableRef,
            _suddenEventEffectTableRef,
            _suddenEventTextTableRef,
            _statusTextTableRef
        };

        // 1. Checking for updates (0% ~ 30%)
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

            // 2. Applying updates (30% ~ 60%)
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
                progress = 0.6f;
                _loadingSlider.value = progress;
            }
        }

        Addressables.Release(checkHandle);

        // 3. Calculating download size (60% ~ 70%)
        _statusText.text = "Verifying resources...";

        long totalDownloadSize = 0;
        foreach (var tableRef in allTableRefs)
        {
            var sizeHandle = Addressables.GetDownloadSizeAsync(tableRef);
            yield return sizeHandle;

            if (sizeHandle.Status == AsyncOperationStatus.Succeeded)
                totalDownloadSize += sizeHandle.Result;

            Addressables.Release(sizeHandle);
        }

        progress = 0.7f;
        _loadingSlider.value = progress;

        // 4. Downloading assets (70% ~ 80%)
        if (totalDownloadSize > 0)
        {
            _statusText.text = "Downloading game data...";

            int downloadedCount = 0;
            foreach (var tableRef in allTableRefs)
            {
                var downloadHandle = Addressables.DownloadDependenciesAsync(tableRef);
                while (!downloadHandle.IsDone)
                {
                    float tableProgress = downloadHandle.PercentComplete / allTableRefs.Count;
                    progress = 0.7f + ((downloadedCount + tableProgress) / allTableRefs.Count) * 0.1f;
                    _loadingSlider.value = progress;

                    yield return null;
                }

                Addressables.Release(downloadHandle);
                downloadedCount++;
            }
        }
        else
        {
            progress = 0.8f;
            _loadingSlider.value = progress;
        }

        // 5. Loading game data (80% ~ 90%)
        _statusText.text = "Loading game data...";

        // 모든 테이블 로드
        yield return LoadTable<GrowthCommandTableSO>(_growthCommandTableRef);
        progress = 0.82f;
        _loadingSlider.value = progress;

        yield return LoadTable<SuddenEventTableSO>(_suddenEventTableRef);
        progress = 0.84f;
        _loadingSlider.value = progress;

        yield return LoadTable<SuddenEventEffectTableSO>(_suddenEventEffectTableRef);
        progress = 0.86f;
        _loadingSlider.value = progress;

        yield return LoadTable<SuddenEventTextTableSO>(_suddenEventTextTableRef);
        progress = 0.88f;
        _loadingSlider.value = progress;

        yield return LoadTable<StatusTextTableSO>(_statusTextTableRef);
        progress = 0.9f;
        _loadingSlider.value = progress;

        // 6. Finalizing (90% ~ 100%)
        _statusText.text = "Initializing...";

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

    private IEnumerator LoadTable<T>(AssetReference assetRef) where T : ScriptableObject
    {
        var loadHandle = assetRef.LoadAssetAsync<T>();
        yield return loadHandle;

        if (loadHandle.Status == AsyncOperationStatus.Succeeded)
        {
            _loadedTables[typeof(T)] = loadHandle.Result;
        }
    }
}
