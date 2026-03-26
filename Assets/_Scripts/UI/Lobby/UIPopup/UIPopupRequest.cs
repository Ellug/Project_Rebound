using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public sealed class UIPopupRequest
{
    // 팝업 패널 프리셋 타입
    public enum PanelType
    {
        Simple,     // 단순 안내(텍스트 위주)
        Default,    // 일반 확인 팝업(서브/이미지 선택 가능)
        Guide       // 페이지형 가이드 팝업(다중 페이지)
    }

    // Primary 버튼의 프리셋 종류(라벨/동작 분기용)
    public enum PrimaryButtonKind
    {
        Confirm,        // 확인
        StartTraining   // 훈련 시작
    }

    [Serializable]
    public sealed class GuidePage
    {
        public string Title;                                // 페이지 제목
        public string Message;                              // 페이지 본문
        public string SubMessage;                           // 페이지 서브 문구
        public string PreviewImageId;                       // 페이지 이미지 파일명 ID (Addressable)
    }

    public PanelType Type = PanelType.Default;              // 패널 타입

    public string Title;                                    // 팝업 제목
    public string Message;                                  // 본문 메시지
    public string SubMessage;                               // 서브 메시지
    public string PreviewImageId;                           // 미리보기 이미지 파일명 ID (Addressable)

    public bool ShowCancel = true;                                    // 취소 버튼 표시 여부
    public PrimaryButtonKind PrimaryKind = PrimaryButtonKind.Confirm; // Primary 버튼 종류

    public bool AutoCloseOnPrimary = true;                  // Primary 클릭 시 자동 닫기
    public bool AutoCloseOnCancel = true;                   // Cancel 클릭 시 자동 닫기

    public bool InvokePrimaryOnClose = false;               // 닫힐 때 Primary 콜백을 호출할지 여부

    public bool PrimaryInteractable = true;                 // Primary 버튼 인터랙션 가능 여부

    public bool DisableBackKey = false;

    // 학생 선택이 필요한 팝업(훈련/이벤트 등) 옵션
    public bool RequiresStudentSelection = false;           // 학생 선택 필요 여부
    public int MaxSelectCount = 0;                          // 선택 최대 인원(0=무제한)
    public StudentCardPreviewDelta StudentCardPreviewDelta; // 카드 프리뷰 UI 보정값(있으면 적용)
    public Action<List<Student>> OnStudentsSelected;        // 선택 완료 시 전달 콜백

    // 버튼 콜백
    public Action OnPrimary; // Primary 버튼 클릭 시
    public Action OnCancel;  // Cancel 버튼 클릭 시

    public List<GuidePage> Pages = new List<GuidePage>(); // Guide 모드 페이지 목록


    // Simple 프리셋 생성(최소 필드만 세팅)
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

    // Default 프리셋 생성(서브/이미지/버튼 종류까지 지원)
    public static UIPopupRequest Default(
        string title,
        string message,
        Action onPrimary = null,
        Action onCancel = null,
        string subMessage = null,
        string previewImageId = null,
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
            PreviewImageId = previewImageId,
            ShowCancel = showCancel,
            PrimaryKind = primaryKind,
            OnPrimary = onPrimary,
            OnCancel = onCancel
        };
    }

    // Guide 프리셋 생성(페이지 리스트 기반, Primary는 "닫기/다음" 쪽으로 사용)
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