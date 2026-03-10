using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class TitleManager : MonoBehaviour
{
    [SerializeField] private Button _continueButton;
    [SerializeField] private GameObject _viewLoadPanel;

    public void OnClickStartButton()
    {
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