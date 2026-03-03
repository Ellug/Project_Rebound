using System;
using System.Collections.Generic;

public static class PopupRequestAdapter
{
    public static UIPopupRequest FromPopupData(PopupData data)
    {
        if (data == null)
            return null;

        ResolveButtons(
            data.Buttons,
            out bool showCancel,
            out Action onCancel,
            out Action onPrimary
        );

        bool useDefaultPanel = data.Image != null || !string.IsNullOrEmpty(data.SubContent);

        if (useDefaultPanel)
        {
            return UIPopupRequest.Default(
                title: data.Title,
                message: data.Content,
                onPrimary: onPrimary,
                onCancel: onCancel,
                subMessage: data.SubContent,
                previewSprite: data.Image,
                showCancel: showCancel,
                primaryKind: UIPopupRequest.PrimaryButtonKind.Confirm
            );
        }

        return UIPopupRequest.Simple(
            title: data.Title,
            message: data.Content,
            onPrimary: onPrimary,
            onCancel: onCancel,
            showCancel: showCancel
        );
    }

    private static void ResolveButtons(
        List<PopupButtonInfo> buttons,
        out bool showCancel,
        out Action onCancel,
        out Action onPrimary
    )
    {
        showCancel = false;
        onCancel = null;
        onPrimary = null;

        int count = buttons != null ? buttons.Count : 0;

        if (count <= 0)
            return;

        if (count == 1)
        {
            onPrimary = buttons[0] != null ? buttons[0].OnClick : null;
            return;
        }

        showCancel = true;
        onCancel = buttons[0] != null ? buttons[0].OnClick : null;
        onPrimary = buttons[1] != null ? buttons[1].OnClick : null;
    }
}