using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class TitleManager : MonoBehaviour
{
    [SerializeField] private Button _continueButton;
    [SerializeField] private GameObject _viewLoadPanel;

    // 인스펙터에 버튼 직접 연결
    public void OnClickStartButton()
    {
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