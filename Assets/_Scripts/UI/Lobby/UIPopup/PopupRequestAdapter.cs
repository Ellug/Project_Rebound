public static class PopupRequestAdapter
{
    public static UIPopupRequest FromPopupData(PopupData data)
    {
        if (data == null) return null;

        bool showCancel = false;
        System.Action onCancel = null;
        System.Action onPrimary = null;

        int btnCount = data.Buttons != null ? data.Buttons.Count : 0;

        if (btnCount <= 0)
        {
            showCancel = false;
            onPrimary = null;
        }
        else if (btnCount == 1)
        {
            showCancel = false;
            onPrimary = data.Buttons[0].OnClick;
        }
        else
        {
            showCancel = true;
            onCancel = data.Buttons[0].OnClick;
            onPrimary = data.Buttons[1].OnClick;
        }

        bool hasDefaultExtras = data.Image != null || !string.IsNullOrEmpty(data.SubContent);

        if (hasDefaultExtras)
        {
            return UIPopupRequest.Default(
                title: data.Title,
                message: data.Content,
                onPrimary: onPrimary,
                onCancel: onCancel,
                subMessage: data.SubContent,
                previewSprite: data.Image,
                showCancel: showCancel
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
}