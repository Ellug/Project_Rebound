using System;
using System.Collections.Generic;
using UnityEngine;

// ConfirmPopup(범용) 요청 데이터
// - 이벤트/훈련/모달 전부 대응
public sealed class ConfirmPopupRequest
{
    // Texts
    public string Title;
    public string Message;
    public string SubMessage;

    // Buttons
    public string PrimaryLabel;
    public Action PrimaryAction;

    public string SecondaryLabel;
    public Action SecondaryAction;

    // Optional preview
    public Sprite PreviewSprite;

    // Modal behavior
    public bool IsModal = true;

    // Auto close flags (ConfirmPopup.cs가 이 이름으로 접근함)
    public bool AutoCloseOnPrimary = true;
    public bool AutoCloseOnSecondary = true;

    // Student selection support
    public bool RequiresStudentSelection = false;
    public int MaxSelectCount = 0;
    public Action<List<Student>> OnStudentsSelected;

    public ConfirmPopupRequest(
        string title,
        string message,
        string primaryLabel,
        Action primaryAction,
        string secondaryLabel = null,
        Action secondaryAction = null,
        Sprite previewSprite = null,
        string subMessage = null)
    {
        Title = title;
        Message = message;
        SubMessage = subMessage;

        PrimaryLabel = primaryLabel;
        PrimaryAction = primaryAction;

        SecondaryLabel = secondaryLabel;
        SecondaryAction = secondaryAction;

        PreviewSprite = previewSprite;
    }

    // 선택사항: 호출부에서 설정 편하게 쓰는 Setter들
    public ConfirmPopupRequest SetModal(bool isModal)
    {
        IsModal = isModal;
        return this;
    }

    public ConfirmPopupRequest SetSubMessage(string subMessage)
    {
        SubMessage = subMessage;
        return this;
    }

    public ConfirmPopupRequest SetAutoCloseOnPrimary(bool autoClose)
    {
        AutoCloseOnPrimary = autoClose;
        return this;
    }

    public ConfirmPopupRequest SetAutoCloseOnSecondary(bool autoClose)
    {
        AutoCloseOnSecondary = autoClose;
        return this;
    }

    public ConfirmPopupRequest SetStudentSelection(bool requiresStudentSelection, int maxSelectCount, Action<List<Student>> onStudentsSelected)
    {
        RequiresStudentSelection = requiresStudentSelection;
        MaxSelectCount = Mathf.Max(0, maxSelectCount);
        OnStudentsSelected = onStudentsSelected;
        return this;
    }
}