using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class CheckPanel : MonoBehaviour
{
    [SerializeField] private TMP_Text _fileNumText;
    [SerializeField] private Button _checkButton;
    [SerializeField] private Button _cancelButton;

    [Header("애니메이션")]
    [SerializeField] private PopupAnimator _animator;

    private int _slotIndex;
    private LoadUI _parent;
    private bool _bound;

    public void Open(int slotIndex, string playTime, LoadUI parent)
    {
        _slotIndex = slotIndex;
        _parent = parent;

        if (_fileNumText != null)
        {
            _fileNumText.text = $"FILE {slotIndex}: {playTime}";
        }

        // Initialize는 SetActive(true) 전에 호출해야 anchoredPosition을 올바르게 읽음
        if (_animator != null)
            _animator.Initialize();

        gameObject.SetActive(true);

        Bind();

        if (_animator == null)
            return;

        _animator.PlayIn();
    }

    private void Bind()
    {
        if (_bound)
        {
            Unbind();
        }

        if (_checkButton != null)
        {
            _checkButton.onClick.AddListener(OnConfirm);
        }

        if (_cancelButton != null)
        {
            _cancelButton.onClick.AddListener(OnCancel);
        }

        _bound = true;
    }

    private void Unbind()
    {
        if (_checkButton != null)
        {
            _checkButton.onClick.RemoveListener(OnConfirm);
        }

        if (_cancelButton != null)
        {
            _cancelButton.onClick.RemoveListener(OnCancel);
        }

        _bound = false;
    }

    private void Close()
    {
        Unbind();

        if (_animator == null)
        {
            gameObject.SetActive(false);
            return;
        }

        _animator.PlayOut(() => gameObject.SetActive(false));
    }

    private void OnConfirm()
    {
        _parent?.OnClickLoad(_slotIndex);
        Close();
    }

    private void OnCancel()
    {
        Close();
    }

    private void OnDestroy()
    {
        Unbind();
    }
}