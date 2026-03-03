using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public sealed class SimplePanelController : MonoBehaviour
{
    [SerializeField] private TMP_Text _txtTitle;
    [SerializeField] private TMP_Text _txtMessage;

    [SerializeField] private Button _btnCancel;
    [SerializeField] private Button _btnConfirm;

    public void Bind(UIPopupRequest request, Action closeSelf)
    {
        if (_txtTitle != null) _txtTitle.text = request.Title ?? "";
        if (_txtMessage != null) _txtMessage.text = request.Message ?? "";

        if (_btnCancel != null)
        {
            _btnCancel.gameObject.SetActive(request.ShowCancel);
            _btnCancel.onClick.RemoveAllListeners();
            _btnCancel.onClick.AddListener(() =>
            {
                request.OnCancel?.Invoke();
                if (request.AutoCloseOnCancel)
                    closeSelf?.Invoke();
            });
        }

        if (_btnConfirm != null)
        {
            _btnConfirm.gameObject.SetActive(true);
            _btnConfirm.interactable = request.PrimaryInteractable;

            _btnConfirm.onClick.RemoveAllListeners();
            _btnConfirm.onClick.AddListener(() =>
            {
                request.OnPrimary?.Invoke();
                if (request.AutoCloseOnPrimary)
                    closeSelf?.Invoke();
            });
        }
    }
}