using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class TitleManager : MonoBehaviour
{
    private const int IntroStoryId = 10001;

    [SerializeField] private Button _continueButton;
    [SerializeField] private GameObject _viewLoadPanel;

    // 타이틀 씬 전용 슬롯 가득 찼을 때 팝업
    [SerializeField] private GameObject _slotFullPopup;
    [SerializeField] private Button _slotFullConfirmButton;
    [SerializeField] private QuitPopup _quitPopup;
    private InputSystem_Actions _input;

    private void Awake()
    {
        if (UIManager.Instance == null)
        {
            _input = new InputSystem_Actions();
            _input.UI.Cancel.performed += ctx => OnEscKey();
            _input.Enable();
        }
    }
    void Start()
    {
        if (SaveManager.Instance != null)
        {
            SaveManager.Instance.DeleteCurrentRunIfMarked();
        }

        // 테이블 로드 완료 후 감독 노드 시스템 초기화
        HeadCoachTableInitializer.Init();

        if (_slotFullPopup != null)
            _slotFullPopup.SetActive(false);

        if (_slotFullConfirmButton != null)
            _slotFullConfirmButton.onClick.AddListener(() => _slotFullPopup.SetActive(false));

        if (_quitPopup != null)
            _quitPopup.Hide();
    }

    private void OnDestroy()
    {
        _input?.Disable();
        _input?.Dispose();
    }


    public void OnClickStartButton()
    {
        if (!SaveManager.Instance.CreateNewGameSlot("기본 학교"))   // 새 게임 시작 시 빈 슬롯 자동 할당
        {
            // 슬롯 생성 실패 시 팝업 표시 후 씬 전환 차단
            if (_slotFullPopup != null)
                _slotFullPopup.SetActive(true);
            else
                Debug.LogWarning("[TitleManager] _slotFullPopup이 연결되지 않았습니다.");

            return;
        }

        // 새 게임 시작 시에만 튜토리얼 가이드 버튼 재노출 가능하도록 리셋
        TutorialGuidePrefs.ResetDismissed();
        VNBridge.RequestStory(IntroStoryId, VNBridge.DefaultReturnSceneName);
        SceneManager.LoadScene(VNBridge.VNSceneName);
    }

    public void OnClickContinueButton()
    {
        _viewLoadPanel.SetActive(true);
    }

    private void OnEscKey()
    {
        // 팝업이 열려있으면 닫기, 없으면 종료 팝업 띄우기
        if (_quitPopup != null && _quitPopup.gameObject.activeSelf)
        {
            _quitPopup.Hide();
            return;
        }

        _quitPopup?.Show();
    }

    public void OnClickExitButton()
    {
        _quitPopup?.Show();
    }
}
