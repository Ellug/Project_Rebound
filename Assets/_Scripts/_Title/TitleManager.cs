using UnityEngine;
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

    [Header("애니메이션")]
    [SerializeField] private PopupAnimator _viewLoadAnimator;   // View_Load 오브젝트에 붙인 PopupAnimator
    [SerializeField] private PopupAnimator _slotFullAnimator;   // SlotFullPopup 오브젝트에 붙인 PopupAnimator

    private InputSystem_Actions _input;

    private void Awake()
    {
        // UIManager 유무와 무관하게 타이틀에서는 TitleManager가 직접 ESC 처리
        // UIManager가 있으면 스택 비었을 때 ShowQuitPopup()을 호출해 중복이 생기므로
        // TitleManager가 ESC를 먼저 소비해 UIManager까지 전달되지 않도록 함
        _input = new InputSystem_Actions();
        _input.UI.Cancel.performed += ctx => OnEscKey();
        _input.Enable();
    }

    void Start()
    {
        if (SaveManager.Instance != null)
            SaveManager.Instance.DeleteCurrentRunIfMarked();

        SoundManager.Instance.PlayBGM(101);

        // 테이블 로드 완료 후 감독 노드 시스템 초기화
        HeadCoachTableInitializer.Init();

        if (_slotFullPopup != null)
            _slotFullPopup.SetActive(false);

        // 슬롯 가득 참 팝업 닫기 버튼 — PlayOut 후 비활성화
        if (_slotFullConfirmButton != null)
            _slotFullConfirmButton.onClick.AddListener(CloseSlotFullPopup);

        if (_quitPopup != null)
            _quitPopup.Hide();
    }

    private void OnEnable()
    {
        _input?.Enable();
    }

    private void OnDisable()
    {
        // TitleManager가 비활성화되면 UIManager의 ESC 처리가 동작하도록 해제
        _input?.Disable();
    }

    private void OnDestroy()
    {
        _input?.Disable();
        _input?.Dispose();
    }

    public void OnClickStartButton()
    {
        SoundManager.Instance.PlayEffect(204);

        if (!SaveManager.Instance.CreateNewGameSlot("기본 학교"))   // 새 게임 시작 시 빈 슬롯 자동 할당
        {
            // 슬롯 생성 실패 시 팝업 표시 후 씬 전환 차단
            if (_slotFullPopup != null)
                OpenSlotFullPopup();
            else
                Debug.LogWarning("[TitleManager] _slotFullPopup이 연결되지 않았습니다.");
            return;
        }

        // 새 게임 시작 시에만 튜토리얼 가이드 버튼 재노출 가능하도록 리셋
        TutorialGuidePrefs.ResetDismissed();
        VNBridge.RequestStory(IntroStoryId, VNBridge.DefaultReturnSceneName);
        SceneTransitionManager.Instance.LoadScene(VNBridge.VNSceneName);
    }

    public void OnClickContinueButton()
    {
        if (_viewLoadAnimator == null)
        {
            _viewLoadPanel.SetActive(true);
            return;
        }

        _viewLoadAnimator.Initialize();
        _viewLoadPanel.SetActive(true);
        _viewLoadAnimator.PlayIn();
    }

    // View_Load 닫기 (LoadUI.TitleSceneLoad에서 호출됨)
    public void CloseViewLoadPanel()
    {
        if (_viewLoadAnimator == null)
        {
            _viewLoadPanel.SetActive(false);
            return;
        }

        _viewLoadAnimator.PlayOut(() => _viewLoadPanel.SetActive(false));
    }

    private void OpenSlotFullPopup()
    {
        if (_slotFullAnimator == null)
        {
            _slotFullPopup.SetActive(true);
            return;
        }

        _slotFullAnimator.Initialize();
        _slotFullPopup.SetActive(true);
        _slotFullAnimator.PlayIn();
    }

    private void CloseSlotFullPopup()
    {
        if (_slotFullAnimator == null)
        {
            _slotFullPopup.SetActive(false);
            return;
        }

        _slotFullAnimator.PlayOut(() => _slotFullPopup.SetActive(false));
    }

    private void OnEscKey()
    {
        // UIManager가 있으면 UIManager의 ESC 처리 중복 방지를 위해 이 프레임 입력 소비
        if (UIManager.Instance != null)
            UIManager.Instance.ConsumeBackKey();

        // View_Load 열려있으면 먼저 닫기
        if (_viewLoadPanel != null && _viewLoadPanel.activeSelf)
        {
            CloseViewLoadPanel();
            return;
        }

        // SlotFull 팝업 열려있으면 닫기
        if (_slotFullPopup != null && _slotFullPopup.activeSelf)
        {
            CloseSlotFullPopup();
            return;
        }

        // QuitPopup이 열려있으면 닫기, 없으면 띄우기
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