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

    // Simple: 기존 팝업처럼 "확인"만 쓰거나, 필요 시 취소도 노출
    public static UIPopupRequest Simple(
        string title,
        string message,
        Action onPrimary = null,
        Action onCancel = null,
        bool showCancel = false,
        bool autoCloseOnPrimary = true,
        bool autoCloseOnCancel = true)
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
            AutoCloseOnCancel = autoCloseOnCancel
        };
    }

    // Default: 서브/이미지 포함 가능 + 취소/PrimaryKind 제어
    public static UIPopupRequest Default(
        string title,
        string message,
        Action onPrimary = null,
        Action onCancel = null,
        string subMessage = null,
        Sprite previewSprite = null,
        bool showCancel = true,
        PrimaryButtonKind primaryKind = PrimaryButtonKind.Confirm,
        bool autoCloseOnPrimary = true,
        bool autoCloseOnCancel = true)
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
            OnCancel = onCancel,
            AutoCloseOnPrimary = autoCloseOnPrimary,
            AutoCloseOnCancel = autoCloseOnCancel
        };
    }

    public static UIPopupRequest Guide(
        string title,
        List<GuidePage> pages,
        Action onClose = null,
        Action onCancel = null,
        bool showCancel = true,
        bool autoCloseOnPrimary = true,
        bool autoCloseOnCancel = true)
    {
        return new UIPopupRequest
        {
            Type = PanelType.Guide,
            Title = title,
            Pages = pages ?? new List<GuidePage>(),
            ShowCancel = showCancel,
            OnPrimary = onClose,
            OnCancel = onCancel,
            AutoCloseOnPrimary = autoCloseOnPrimary,
            AutoCloseOnCancel = autoCloseOnCancel
        };
    }
}