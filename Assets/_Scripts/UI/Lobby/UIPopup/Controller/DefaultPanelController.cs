using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public sealed class DefaultPanelController : MonoBehaviour
{
    [SerializeField] private TMP_Text _txtTitle;
    [SerializeField] private TMP_Text _txtSub;
    [SerializeField] private TMP_Text _txtMessage;
    [SerializeField] private Image _imgPreview;

    [SerializeField] private Button _btnCancel;

    [SerializeField] private GameObject _primaryConfirmRoot;
    [SerializeField] private Button _btnConfirm;

    [SerializeField] private GameObject _primaryStartTrainingRoot;
    [SerializeField] private Button _btnStartTraining;

    public void Bind(UIPopupRequest request, Action closeSelf)
    {
        if (_txtTitle != null) _txtTitle.text = request.Title ?? "";
        if (_txtMessage != null) _txtMessage.text = request.Message ?? "";

        if (_txtSub != null)
        {
            bool hasSub = !string.IsNullOrEmpty(request.SubMessage);
            _txtSub.gameObject.SetActive(hasSub);
            if (hasSub) _txtSub.text = request.SubMessage;
        }

        if (_imgPreview != null)
        {
            bool hasSprite = request.PreviewSprite != null;
            _imgPreview.gameObject.SetActive(hasSprite);
            if (hasSprite)
            {
                _imgPreview.sprite = request.PreviewSprite;
                _imgPreview.preserveAspect = true;
            }
        }

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

        ApplyPrimaryKind(request, closeSelf);
    }

    private void ApplyPrimaryKind(UIPopupRequest request, Action closeSelf)
    {
        if (_primaryConfirmRoot != null)
            _primaryConfirmRoot.SetActive(request.PrimaryKind == UIPopupRequest.PrimaryButtonKind.Confirm);

        if (_primaryStartTrainingRoot != null)
            _primaryStartTrainingRoot.SetActive(request.PrimaryKind == UIPopupRequest.PrimaryButtonKind.StartTraining);

        if (_btnConfirm != null)
        {
            _btnConfirm.interactable = request.PrimaryInteractable;
            _btnConfirm.onClick.RemoveAllListeners();
            _btnConfirm.onClick.AddListener(() =>
            {
                request.OnPrimary?.Invoke();
                if (request.AutoCloseOnPrimary)
                    closeSelf?.Invoke();
            });
        }

        if (_btnStartTraining != null)
        {
            _btnStartTraining.interactable = request.PrimaryInteractable;
            _btnStartTraining.onClick.RemoveAllListeners();
            _btnStartTraining.onClick.AddListener(() =>
            {
                request.OnPrimary?.Invoke();
                if (request.AutoCloseOnPrimary)
                    closeSelf?.Invoke();
            });
        }
    }
}