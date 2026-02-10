using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class StartManager : MonoBehaviour
{
    [SerializeField] private Slider _loadingSlider;

    private readonly WaitForSeconds _waitOneSecond = new(1f);

    private void Start()
    {
        StartCoroutine(LoadingProcess());
    }

    private IEnumerator LoadingProcess()
    {
        // 어드레서블 체크 및 적용 단계 (0% ~ 90%)
        float progress = 0f;
        float targetProgress = 0.9f;

        // 실제 어드레서블 다운로드 / 체크 로직을 여기에 추가
        while (progress < targetProgress)
        {
            // TODO: 어드레서블 도입 후 실제 어드레서블 진행률로 대체
            progress += Time.deltaTime * 0.3f; // 임시 진행률 증가

            if (progress > targetProgress)
                progress = targetProgress;

            _loadingSlider.value = progress;
            yield return null;
        }

        // 에드레서블 완료되면 100%까지 진행
        while (progress < 1f)
        {
            progress += Time.deltaTime * 0.5f;
            if (progress > 1f)
                progress = 1f;

            _loadingSlider.value = progress;
            yield return null;
        }

        yield return _waitOneSecond;
        SceneManager.LoadScene("Title");
    }
}
