using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class TitleManager : MonoBehaviour
{
    [SerializeField] private Button _continueButton;
    [SerializeField] private GameObject _viewLoadPanel;

    void Start()
    {
        // 테이블 로드 완료 후 감독 노드 시스템 초기화
        HeadCoachTableInitializer.Init();
    }

    public void OnClickStartButton()
    {
        if (!SaveManager.Instance.CreateNewGameSlot("기본 학교"))   // 새 게임 시작 시 빈 슬롯 자동 할당
        {
            Debug.LogWarning("세이브 슬롯이 가득 찼습니다. 기존의 세이브를 삭제하여 빈 세이브슬롯을 확보해주세요.");
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