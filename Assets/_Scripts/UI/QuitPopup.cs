using UnityEngine;
using UnityEngine.UI;

public class QuitPopup : MonoBehaviour
{
    [SerializeField] private Button _confirmButton;
    [SerializeField] private Button _cancelButton;

    [Header("애니메이션")]
    [SerializeField] private PopupAnimator _animator;

    private void Awake()
    {
        _confirmButton.onClick.AddListener(OnConfirm);
        _cancelButton.onClick.AddListener(OnCancel);
    }

    public void Show()
    {
        if (_animator == null)
        {
            gameObject.SetActive(true);
            return;
        }

        _animator.Initialize();
        gameObject.SetActive(true);
        _animator.PlayIn();
    }

    public void Hide()
    {
        if (_animator == null)
        {
            gameObject.SetActive(false);
            return;
        }

        _animator.PlayOut(() => gameObject.SetActive(false));
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