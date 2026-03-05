using System;
using System.Collections.Generic;

public static class PopupRequestAdapter
{
    // 기존 PopupData를 새로운 UIPopupRequest로 변환
    public static UIPopupRequest FromPopupData(PopupData data)
    {
        if (data == null)
            return null;

        // 버튼 리스트를 Cancel/Primary로 매핑
        ResolveButtons(
            data.Buttons,
            out bool showCancel,
            out Action onCancel,
            out Action onPrimary
        );

        // 이미지 or 서브텍스트가 있으면 Default 패널 사용, 없으면 Simple 사용
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

    // PopupButtonInfo 리스트를 (Cancel, Primary) 형태로 정규화
    // 1개면 Primary만 사용
    // 2개 이상이면 [0]=Cancel, [1]=Primary로 사용
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