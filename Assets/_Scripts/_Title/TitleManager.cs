using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class TitleManager : MonoBehaviour
{
    [SerializeField] private Button _continueButton;
    [SerializeField] private GameObject _viewLoadPanel;

    // 타이틀 씬 전용 슬롯 가득 찼을 때 팝업
    [SerializeField] private GameObject _slotFullPopup;
    [SerializeField] private Button _slotFullConfirmButton;

    void Start()
    {
        // 테이블 로드 완료 후 감독 노드 시스템 초기화
        HeadCoachTableInitializer.Init();

        if (_slotFullPopup != null)
            _slotFullPopup.SetActive(false);

        if (_slotFullConfirmButton != null)
            _slotFullConfirmButton.onClick.AddListener(() => _slotFullPopup.SetActive(false));
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
        SceneManager.LoadScene("Lobby");
    }

    public void OnClickContinueButton()
    {
        _viewLoadPanel.SetActive(true);
    }

    public void OnClickExitButton()
    {
        Application.Quit();
    }
}