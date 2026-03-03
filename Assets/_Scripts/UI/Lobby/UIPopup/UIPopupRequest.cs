using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public sealed class UIPopupRequest
{
    public enum PanelType
    {
        Simple,
        Default,
        Guide
    }

    public enum PrimaryButtonKind
    {
        Confirm,
        StartTraining
    }

    [Serializable]
    public sealed class GuidePage
    {
        public string Title;
        public string Message;
        public string SubMessage;
        public Sprite PreviewSprite;
    }

    public PanelType Type = PanelType.Default;

    public string Title;
    public string Message;
    public string SubMessage;
    public Sprite PreviewSprite;

    public bool ShowCancel = true;

    public PrimaryButtonKind PrimaryKind = PrimaryButtonKind.Confirm;

    public bool AutoCloseOnPrimary = true;
    public bool AutoCloseOnCancel = true;

    public bool InvokePrimaryOnClose = false;

    public bool PrimaryInteractable = true;

    public bool RequiresStudentSelection = false;
    public int MaxSelectCount = 0;
    public Action<List<Student>> OnStudentsSelected;

    public Action OnPrimary;
    public Action OnCancel;

    public List<GuidePage> Pages = new List<GuidePage>();

    public static UIPopupRequest Simple(
     string title,
     string message,
     Action onPrimary,
     Action onCancel,
     bool showCancel = true,
     bool autoCloseOnPrimary = true,
     bool autoCloseOnCancel = true,
     bool primaryInteractable = true)
    {
        return new UIPopupRequest
        {
            Type = PanelType.Simple,
            Title = title,
            Message = message,
            ShowCancel = showCancel,

            OnPrimary = onPrimary,
            OnCancel = onCancel,

            AutoCloseOnPrimary = autoCloseOnPrimary,
            AutoCloseOnCancel = autoCloseOnCancel,
            PrimaryInteractable = primaryInteractable,
        };
    }

    public static UIPopupRequest Default(
        string title,
        string message,
        Action onPrimary = null,
        Action onCancel = null,
        string subMessage = null,
        Sprite previewSprite = null,
        bool showCancel = true,
        PrimaryButtonKind primaryKind = PrimaryButtonKind.Confirm
    )
    {
        return new UIPopupRequest
        {
            Type = PanelType.Default,
            Title = title,
            Message = message,
            SubMessage = subMessage,
            PreviewSprite = previewSprite,
            ShowCancel = showCancel,
            PrimaryKind = primaryKind,
            OnPrimary = onPrimary,
            OnCancel = onCancel
        };
    }

    public static UIPopupRequest Guide(
        string title,
        List<GuidePage> pages,
        Action onClose = null,
        Action onCancel = null,
        bool showCancel = false
    )
    {
        return new UIPopupRequest
        {
            Type = PanelType.Guide,
            Title = title,
            Pages = pages ?? new List<GuidePage>(),
            ShowCancel = showCancel,
            OnPrimary = onClose,
            OnCancel = onCancel
        };
    }
}