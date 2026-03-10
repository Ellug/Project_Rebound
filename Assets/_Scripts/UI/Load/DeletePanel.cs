using UnityEngine;
using UnityEngine.UI;
using TMPro;

// 세이브 슬롯 삭제 패널
public class DeletePanel : MonoBehaviour
{
    [SerializeField] private TMP_Text _fileNumText;
    [SerializeField] private Button _checkButton;
    [SerializeField] private Button _cancelButton;

    private int _slotIndex;
    private LoadUI _parent;
    private bool _bound;

    void Awake()
    {
        gameObject.SetActive(false);
    }

    public void Open(int slotIndex, string playTime, LoadUI parent)
    {
        _slotIndex = slotIndex;
        _parent = parent;

        if (_fileNumText != null)
        {
            _fileNumText.text = $"FILE {slotIndex}: {playTime}";
        }

        gameObject.SetActive(true);
        Bind();
    }

    private void Bind()
    {
        if (_bound)
        {
            Unbind();
        }

        if (_checkButton != null)
        {
            _checkButton.onClick.AddListener(OnDestroy);
            _checkButton.onClick.AddListener(OnCancel);
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
            _checkButton.onClick.RemoveListener(OnDestroy);
            _checkButton.onClick.RemoveListener(OnCancel);
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
        gameObject.SetActive(false);
    }

    private void OnCancel()
    {
        Close();
    }

    private void OnDestroy()
    {
        _parent?.OnClickDelete(_slotIndex);
    }
}
