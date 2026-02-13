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

    [Header("Student Data Tables")]
    [SerializeField] private AssetReference _studentNameTableRef;
    [SerializeField] private AssetReference _studentBodyTableRef;
    [SerializeField] private AssetReference _studentStatTableRef;
    [SerializeField] private AssetReference _studentStartStateTableRef;
    [SerializeField] private AssetReference _studentPotentialTableRef;
    [SerializeField] private AssetReference _studentStatusProbTableRef;
    [SerializeField] private AssetReference _studentStatExpTableRef;
    [SerializeField] private AssetReference _studentPlusExpTableRef;
    [SerializeField] private AssetReference _studentPositionTableRef;

    // 로드된 테이블들 (임시 저장용)
    private GrowthCommandTableSO _growthCommandTable;
    private SuddenEventTableSO _suddenEventTable;
    private SuddenEventEffectTableSO _suddenEventEffectTable;
    private SuddenEventTextTableSO _suddenEventTextTable;
    private StatusTextTableSO _statusTextTable;
    private StudentNameTableSO _studentNameTable;
    private StudentBodyTableSO _studentBodyTable;
    private StudentStatTableSO _studentStatTable;
    private StudentStartStatTableSO _studentStartStateTable;
    private StudentPotentialTableSO _studentPotentialTable;
    private StudentStatusProbTableSO _studentStatusProbTable;
    private StudentStatExpTableSO _studentStatExpTable;
    private StudentPlusExpTableSO _studentPlusExpTable;
    private StudentPositionTableSO _studentPositionTable;

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
            _statusTextTableRef,
            _studentNameTableRef,
            _studentBodyTableRef,
            _studentStatTableRef,
            _studentStartStateTableRef,
            _studentPotentialTableRef,
            _studentStatusProbTableRef,
            _studentStatExpTableRef,
            _studentPlusExpTableRef,
            _studentPositionTableRef
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
        yield return LoadTable<GrowthCommandTableSO>(_growthCommandTableRef, t => _growthCommandTable = t);
        progress = 0.8071f;
        _loadingSlider.value = progress;

        yield return LoadTable<SuddenEventTableSO>(_suddenEventTableRef, t => _suddenEventTable = t);
        progress = 0.8143f;
        _loadingSlider.value = progress;

        yield return LoadTable<SuddenEventEffectTableSO>(_suddenEventEffectTableRef, t => _suddenEventEffectTable = t);
        progress = 0.8214f;
        _loadingSlider.value = progress;

        yield return LoadTable<SuddenEventTextTableSO>(_suddenEventTextTableRef, t => _suddenEventTextTable = t);
        progress = 0.8286f;
        _loadingSlider.value = progress;

        yield return LoadTable<StatusTextTableSO>(_statusTextTableRef, t => _statusTextTable = t);
        progress = 0.8357f;
        _loadingSlider.value = progress;

        yield return LoadTable<StudentNameTableSO>(_studentNameTableRef, t => _studentNameTable = t);
        progress = 0.8429f;
        _loadingSlider.value = progress;

        yield return LoadTable<StudentBodyTableSO>(_studentBodyTableRef, t => _studentBodyTable = t);
        progress = 0.85f;
        _loadingSlider.value = progress;

        yield return LoadTable<StudentStatTableSO>(_studentStatTableRef, t => _studentStatTable = t);
        progress = 0.8571f;
        _loadingSlider.value = progress;

        yield return LoadTable<StudentStartStatTableSO>(_studentStartStateTableRef, t => _studentStartStateTable = t);
        progress = 0.8643f;
        _loadingSlider.value = progress;

        yield return LoadTable<StudentPotentialTableSO>(_studentPotentialTableRef, t => _studentPotentialTable = t);
        progress = 0.8714f;
        _loadingSlider.value = progress;

        yield return LoadTable<StudentStatusProbTableSO>(_studentStatusProbTableRef, t => _studentStatusProbTable = t);
        progress = 0.8786f;
        _loadingSlider.value = progress;

        yield return LoadTable<StudentStatExpTableSO>(_studentStatExpTableRef, t => _studentStatExpTable = t);
        progress = 0.8857f;
        _loadingSlider.value = progress;

        yield return LoadTable<StudentPlusExpTableSO>(_studentPlusExpTableRef, t => _studentPlusExpTable = t);
        progress = 0.8929f;
        _loadingSlider.value = progress;

        yield return LoadTable<StudentPositionTableSO>(_studentPositionTableRef, t => _studentPositionTable = t);
        progress = 0.9f;
        _loadingSlider.value = progress;

        // 6. Initializing DataManager (90% ~ 95%)
        _statusText.text = "Initializing...";

        // DataManager에 테이블 등록
        CachedSOData.RegisterTables(
            _growthCommandTable,
            _suddenEventTable,
            _suddenEventEffectTable,
            _suddenEventTextTable,
            _statusTextTable,
            _studentNameTable,
            _studentBodyTable,
            _studentStatTable,
            _studentStartStateTable,
            _studentPotentialTable,
            _studentStatusProbTable,
            _studentStatExpTable,
            _studentPlusExpTable,
            _studentPositionTable
        );

        progress = 0.95f;
        _loadingSlider.value = progress;

        // 7. Finalizing (95% ~ 100%)
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

    // 제네릭 테이블 로드 메서드
    private IEnumerator LoadTable<T>(AssetReference assetRef, System.Action<T> onLoaded) where T : ScriptableObject
    {
        var loadHandle = assetRef.LoadAssetAsync<T>();
        yield return loadHandle;

        if (loadHandle.Status == AsyncOperationStatus.Succeeded)
            onLoaded?.Invoke(loadHandle.Result);
        else
            Debug.LogError($"[LoadTable] Failed to load {typeof(T).Name}: {loadHandle.Status}");
    }
}
