using UnityEngine;
using UnityEngine.UI;

public class QuitPopup : MonoBehaviour
{
    [SerializeField] private Button _confirmButton;
    [SerializeField] private Button _cancelButton;

    private void Awake()
    {
        _confirmButton.onClick.AddListener(OnConfirm);
        _cancelButton.onClick.AddListener(OnCancel);
    }

    public void Show()
    {
        gameObject.SetActive(true);
    }

    public void Hide()
    {
        gameObject.SetActive(false);
    }

    private void OnConfirm()
    {
        Application.Quit();
    }

    private void OnCancel()
    {
        Hide();
    }
}