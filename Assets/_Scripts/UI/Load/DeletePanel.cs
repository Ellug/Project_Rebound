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

    [Header("애니메이션")]
    [SerializeField] private PopupAnimator _animator;

    private int _slotIndex;
    private LoadUI _parent;
    private bool _bound;
    private Coroutine _enableRoutine;

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
            _fileNumText.text = $"FILE {slotIndex}: {playTime}";

        // Initialize는 SetActive(true) 전에 호출해야 anchoredPosition을 올바르게 읽음
        // SetActive(true) 시점에 OnEnable이 실행되므로 그 전에 초기화 완료
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

        if (_animator == null)
        {
            gameObject.SetActive(false);
            return;
        }

        _animator.PlayOut(() => gameObject.SetActive(false));
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