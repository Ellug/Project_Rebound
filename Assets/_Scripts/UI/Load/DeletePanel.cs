using UnityEngine;
using UnityEngine.UI;
using TMPro;

// 세이브 슬롯 삭제 패널
public class DeletePanel : MonoBehaviour
{
    [SerializeField] private TMP_Text _fileNumText;
    [SerializeField] private Button _checkButton;
    [SerializeField] private Button _cancelButton;
    [SerializeField] private float _confirmEnableDelay = 3f;

    private int _slotIndex;
    private LoadUI _parent;
    private bool _bound;
    private Coroutine _enableRoutine;

    //void Awake()
    //{
    //    gameObject.SetActive(false);
    //}

    void OnEnable()
    {
        if (_checkButton != null)
            _checkButton.interactable = false;

        if (_enableRoutine != null)
            StopCoroutine(_enableRoutine);

        _enableRoutine = StartCoroutine(CoEnableConfirmButton());
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
            _checkButton.onClick.AddListener(OnConfirmDelete);
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
            _checkButton.onClick.RemoveListener(OnConfirmDelete);
        }

        if (_cancelButton != null)
        {
            _cancelButton.onClick.RemoveListener(OnCancel);
        }

        _bound = false;
    }

    private void Close()
    {
        if (_enableRoutine != null)
        {
            StopCoroutine(_enableRoutine);
            _enableRoutine = null;
        }

        Unbind();
        gameObject.SetActive(false);
    }

    private void OnConfirmDelete()
    {
        _parent?.OnClickDelete(_slotIndex);
        Close();
    }

    private void OnCancel()
    {
        Close();
    }

    private System.Collections.IEnumerator CoEnableConfirmButton()
    {
        yield return new WaitForSeconds(_confirmEnableDelay);

        if (_checkButton != null)
            _checkButton.interactable = true;

        _enableRoutine = null;
    }

    void OnDestroy()
    {
        Unbind();
    }
}