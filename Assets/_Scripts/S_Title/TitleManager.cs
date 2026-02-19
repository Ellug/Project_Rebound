using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class TitleManager : MonoBehaviour
{
    [SerializeField] private Button _continueButton;
    [SerializeField] private GameObject _viewLoadPanel;

    // 이어하기 현재 미구현이므로 비활성화
    void Start()
    {
        //_continueButton.interactable = false;
    }

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
