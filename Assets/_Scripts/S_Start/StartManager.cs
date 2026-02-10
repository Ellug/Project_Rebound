using System.Collections;
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
    [SerializeField] private AssetReference _growthCommandTableReference;

    private readonly WaitForSeconds _waitOneSecond = new(1f);

    private void Start()
    {
        StartCoroutine(LoadingProcess());
    }

    private IEnumerator LoadingProcess()
    {
        // 1. Checking for updates (0% ~ 30%)
        _statusText.text = "Checking for updates...";

        // 서버에 카탈로그 조회
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

                // 카탈로그 다운로드 및 적용
                var updateHandle = Addressables.UpdateCatalogs(catalogsToUpdate, false);
                while (!updateHandle.IsDone)
                {
                    progress = 0.3f + (updateHandle.PercentComplete * 0.3f);
                    _loadingSlider.value = progress;
                    yield return null;
                }

                // 더 이상 사용하지 않는 핸들은 메모리 해제
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

        // 다운로드가 필요한 에셋의 총 용량 확인
        var sizeHandle = Addressables.GetDownloadSizeAsync(_growthCommandTableReference);
        while (!sizeHandle.IsDone)
        {
            progress = 0.6f + (sizeHandle.PercentComplete * 0.1f);
            _loadingSlider.value = progress;
            yield return null;
        }

        long downloadSize = 0;
        if (sizeHandle.Status == AsyncOperationStatus.Succeeded)
        {
            downloadSize = sizeHandle.Result; // 바이트 단위
        }

        Addressables.Release(sizeHandle);

        // 4. Downloading assets (70% ~ 80%)
        if (downloadSize > 0)
        {
            // 실제 에셋 번들 파일들을 서버에서 다운로드
            var downloadHandle = Addressables.DownloadDependenciesAsync(_growthCommandTableReference);
            while (!downloadHandle.IsDone)
            {
                progress = 0.7f + (downloadHandle.PercentComplete * 0.1f);
                _loadingSlider.value = progress;

                float downloadedKB = downloadHandle.PercentComplete * downloadSize / 1024f;
                float totalKB = downloadSize / 1024f;
                _statusText.text = $"Downloading... {downloadedKB:F1}/{totalKB:F1} KB";

                yield return null;
            }

            Addressables.Release(downloadHandle);
        }
        else
        {
            progress = 0.8f;
            _loadingSlider.value = progress;
        }

        // 5. Loading game data (80% ~ 90%)
        _statusText.text = "Loading game data...";

        // 다운로드한 에셋을 메모리에 로드 => 실제 사용 가능한 상태로 만듦
        var loadHandle = _growthCommandTableReference.LoadAssetAsync<GrowthCommandTableSO>();
        while (!loadHandle.IsDone)
        {
            progress = 0.8f + (loadHandle.PercentComplete * 0.1f);
            _loadingSlider.value = progress;
            yield return null;
        }

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
}
