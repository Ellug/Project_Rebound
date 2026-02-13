using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class LoadConfirmPanel : MonoBehaviour
{
    [SerializeField] private TMP_Text _fileNumText;
    [SerializeField] private TMP_Text _playTimeText;
    [SerializeField] private Button _checkButtonn;
    [SerializeField] private Button _cancelButton;

    private int _slotIndex;
    private LoadUI _parent;
    private bool _bound;

    public void Open(int slotIndex, string playTime, LoadUI parent)
    {
        _slotIndex = slotIndex;
        _parent = parent;

        _fileNumText.text = $"FILE {slotIndex}";
        _playTimeText.text = playTime;

        gameObject.SetActive(true);
        Bind();
    }

    private void Bind()
    {
        if (_bound) Unbind();

        _checkButtonn.onClick.AddListener(OnConfirm);
        _cancelButton.onClick.AddListener(OnCancel);

        _bound = true;
    }

    private void Unbind()
    {
        _checkButtonn.onClick.RemoveListener(OnConfirm);
        _cancelButton.onClick.RemoveListener(OnCancel);
        _bound = false;
    }

    private void OnConfirm()
    {
        _parent?.OnClickLoad(_slotIndex);
        gameObject.SetActive(false);
    }

    private void OnCancel()
    {
        gameObject.SetActive(false);
    }
}
