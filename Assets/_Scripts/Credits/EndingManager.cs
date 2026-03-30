using UnityEngine;
using UnityEngine.SceneManagement;

// 엔딩 전체 흐름 관리
// GameManager.TryTriggerEnding()에서 TriggerEnding() 호출 -> 엔딩 팝업 -> 엔딩 크레딧 시퀀스 -> 타이틀 복귀
public class EndingManager : MonoBehaviour
{
    private const string TitleScene = "Title";

    [Header("References")]
    [SerializeField] private EndingCreditUI _endingCreditUI;

    [Header("Debug")]
    [Tooltip("true 시 Start()에서 자동으로 TriggerEnding() 호출 (테스트용)")]
    [SerializeField] private bool _autoTriggerOnStart = false;

    private void Start()
    {
        if (_endingCreditUI != null)
            _endingCreditUI.OnCreditFinished += ReturnToTitle;

        if (_autoTriggerOnStart)
            TriggerEnding();
    }

    private void OnDestroy()
    {
        if (_endingCreditUI != null)
            _endingCreditUI.OnCreditFinished -= ReturnToTitle;
    }

    // GameManager.TryTriggerEnding()에서 호출
    public void TriggerEnding()
    {
        ShowEndingPopup();
    }

    // 엔딩 팝업 UI
    private void ShowEndingPopup()
    {
        if (UIManager.Instance == null)
        {
            StartCreditSequence();
            return;
        }

        UIPopupRequest request = UIPopupRequest.Default(
            title: "[엔딩] 끝이 아닌 시작",
            message: "1년이 하나 둘 쌓여 4년이란 시간이 흐르고\n" +
                            "수많은 선택 속에서 세심한 것들이 조금씩 변해간다.\n" +
                            "아이들이 졸업과 입학을 번갈아 가듯이.\n" +
                            "이야기는 계속 시작을 반복한다.",
            onPrimary: StartCreditSequence,
            onCancel: null,
            subMessage: null,
            previewImageId: "EndingPopup_img",
            showCancel: false,
            primaryKind: UIPopupRequest.PrimaryButtonKind.Confirm
        );

        request.AutoCloseOnPrimary = true;
        request.DisableBackKey = true;

        UIManager.Instance.ShowPopup(request);
    }

    private void StartCreditSequence()
    {
        if (_endingCreditUI == null)
        {
            Debug.LogWarning("[EndingManager] EndingCreditUI가 연결되지 않았습니다. 타이틀로 복귀합니다.");
            ReturnToTitle();
            return;
        }

        _endingCreditUI.gameObject.SetActive(true);
        _endingCreditUI.Play();
    }

    private void ReturnToTitle()
    {
        if (SceneTransitionManager.Instance != null && SceneTransitionManager.Instance.IsTransitioning)
        {
            Debug.LogWarning("[EndingManager] SceneTransitionManager가 이미 전환 중입니다.");
            return;
        }

        if (SceneTransitionManager.Instance != null)
        {
            // onMidpoint: ScaleX 퇴장 완료 후, 씬 로드 직전에 GameManager 상태 정리
            SceneTransitionManager.Instance.LoadScene(TitleScene, onMidpoint: CleanupGameState);
        }
        else
        {
            CleanupGameState();
            SceneManager.LoadScene(TitleScene);
        }
    }

    private static void CleanupGameState()
    {
        if (GameManager.Instance != null)
            GameManager.Instance.ClearFlowRuntimeState();
    }
}