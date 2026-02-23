using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ConfirmPopup : UIPopup
{
    [Header("UI")]
    [SerializeField] private TMP_Text _txtName;
    [SerializeField] private TMP_Text _txtSub;
    [SerializeField] private TMP_Text _txtMessage;
    [SerializeField] private Image _imgPreview;

    [Header("Buttons")]
    [SerializeField] private Button _btnSecondary;
    [SerializeField] private TMP_Text _txtSecondary;
    [SerializeField] private Button _btnPrimary;
    [SerializeField] private TMP_Text _txtPrimary;

    [Header("Student Select")]
    [SerializeField] private StudentSelectPopup _studentSelectPrefab;

    private ConfirmPopupRequest _request;

    public override void Init()
    {
        base.Init();

        if (_btnPrimary != null)
        {
            _btnPrimary.onClick.RemoveAllListeners();
            _btnPrimary.onClick.AddListener(HandlePrimaryClicked);
        }

        if (_btnSecondary != null)
        {
            _btnSecondary.onClick.RemoveAllListeners();
            _btnSecondary.onClick.AddListener(HandleSecondaryClicked);
        }
    }

    public void Setup(ConfirmPopupRequest request)
    {
        _request = request;

        ApplyTexts(request);
        ApplyPreview(request);
        ApplyButtons(request);
    }

    private void ApplyTexts(ConfirmPopupRequest request)
    {
        if (_txtName != null)
        {
            bool hasTitle = !string.IsNullOrEmpty(request.Title);
            _txtName.gameObject.SetActive(hasTitle);
            if (hasTitle) _txtName.text = request.Title;
        }

        if (_txtSub != null)
        {
            bool hasSub = !string.IsNullOrEmpty(request.SubMessage);
            _txtSub.gameObject.SetActive(hasSub);
            if (hasSub) _txtSub.text = request.SubMessage;
        }

        if (_txtMessage != null)
        {
            bool hasMsg = !string.IsNullOrEmpty(request.Message);
            _txtMessage.gameObject.SetActive(hasMsg);
            if (hasMsg) _txtMessage.text = request.Message;
        }
    }

    private void ApplyPreview(ConfirmPopupRequest request)
    {
        if (_imgPreview == null) return;

        bool hasSprite = request.PreviewSprite != null;
        _imgPreview.gameObject.SetActive(hasSprite);

        if (hasSprite)
        {
            _imgPreview.sprite = request.PreviewSprite;
            _imgPreview.preserveAspect = true;
        }
    }

    private void ApplyButtons(ConfirmPopupRequest request)
    {
        if (_txtPrimary != null)
            _txtPrimary.text = string.IsNullOrEmpty(request.PrimaryLabel) ? "확인" : request.PrimaryLabel;

        bool hasSecondary = !string.IsNullOrEmpty(request.SecondaryLabel);

        if (_btnSecondary != null)
            _btnSecondary.gameObject.SetActive(hasSecondary);

        if (hasSecondary && _txtSecondary != null)
            _txtSecondary.text = request.SecondaryLabel;
    }

    private void HandlePrimaryClicked()
    {
        if (_request == null)
        {
            CloseSelf();
            return;
        }

        if (_request.RequiresStudentSelection)
        {
            OpenStudentSelect();
            return;
        }

        _request.PrimaryAction?.Invoke();

        if (_request.AutoCloseOnPrimary)
            CloseSelf();
    }

    private void HandleSecondaryClicked()
    {
        if (_request == null)
        {
            CloseSelf();
            return;
        }

        _request.SecondaryAction?.Invoke();

        if (_request.AutoCloseOnSecondary)
            CloseSelf();
    }

    private void OpenStudentSelect()
    {
        if (_studentSelectPrefab == null)
        {
            List<Student> fallback = StudentManager.Instance != null
                ? new List<Student>(StudentManager.Instance.Students)
                : new List<Student>();

            _request.OnStudentsSelected?.Invoke(fallback);

            if (_request.AutoCloseOnPrimary)
                CloseSelf();

            return;
        }

        Close();

        StudentSelectPopup popup = Instantiate(_studentSelectPrefab, transform.parent);
        popup.SetMaxSelectCount(_request.MaxSelectCount);
        popup.Init();
        popup.Open();

        popup.OnSelectionConfirmed += HandleStudentsSelected;
        popup.OnCancelled += HandleStudentSelectCancelled;
    }

    private void HandleStudentsSelected(List<Student> students)
    {
        _request.OnStudentsSelected?.Invoke(students);

        if (_request.AutoCloseOnPrimary)
            CloseSelf();
    }

    private void HandleStudentSelectCancelled()
    {
        Open();
    }

    protected override void OnCloseButtonClicked()
    {
        CloseSelf();
    }

    private void CloseSelf()
    {
        if (UIManager.Instance != null)
        {
            UIManager.Instance.Close(this);
            return;
        }

        Close();
        Destroy(gameObject);
    }
}