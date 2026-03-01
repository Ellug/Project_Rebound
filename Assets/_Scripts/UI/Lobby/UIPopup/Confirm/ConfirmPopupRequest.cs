using System;
using System.Collections.Generic;
using UnityEngine;

// 확인 팝업 요청 데이터
// UIManager.ShowConfirm(request) 또는 UIPopup.Setup(request, PopupType.Confirm) 에 전달
public class ConfirmPopupRequest
{
    public string Title { get; }
    public string Message { get; }
    public string SubMessage { get; private set; }
    public string PrimaryLabel { get; }
    public string SecondaryLabel { get; }
    public Sprite PreviewSprite { get; }

    public Action PrimaryAction { get; set; }
    public Action SecondaryAction { get; }

    public bool IsModal { get; set; } = true;
    public bool AutoCloseOnPrimary { get; set; } = true;
    public bool AutoCloseOnSecondary { get; set; } = true;
    public bool InvokeConfirmOnClose { get; set; } = false;
    public bool PrimaryInteractable { get; set; } = true;

    // 학생 선택 관련
    public bool RequiresStudentSelection { get; set; } = false;
    public int MaxSelectCount { get; set; } = 0;
    public Action<List<Student>> OnStudentsSelected { get; set; }

    // 훈련 시작 버튼(_btnTrainingConfirm) 사용 여부
    // true  : 이미지11(웨이트 트레이닝)처럼 훈련 실행 확인 → "훈련 시작" 버튼 표시
    // false : 이미지6~10처럼 일반 이벤트/영입 확인         → "확인" 버튼 표시 (기본값)
    // SubMessage 유무로 판단하지 않음
    //   → 이미지8 주말훈련제안은 SubMessage 있어도 "확인" 버튼 사용
    //   → TrainingSelectPopup.OpenConfirmPopup에서만 true로 지정
    public bool UseTrainingConfirmButton { get; set; } = false;

    public ConfirmPopupRequest(
        string title,
        string message,
        string primaryLabel = "확인",
        Action primaryAction = null,
        string secondaryLabel = null,
        Action secondaryAction = null,
        string subMessage = null,
        Sprite previewSprite = null)
    {
        Title = title;
        Message = message;
        PrimaryLabel = primaryLabel;
        PrimaryAction = primaryAction;
        SecondaryLabel = secondaryLabel;
        SecondaryAction = secondaryAction;
        SubMessage = subMessage;
        PreviewSprite = previewSprite;
    }

    // ─────────────────────────────────────────────────────────────────────────
    // 메서드 체이닝용 설정 메서드
    // 사용 예:
    //   request.SetModal(false).SetInvokeConfirmOnClose(true)
    //   request.SetSubMessage("현재 정원으로 영입 불가").SetPrimaryInteractable(false)
    // ─────────────────────────────────────────────────────────────────────────

    public ConfirmPopupRequest SetModal(bool isModal)
    {
        IsModal = isModal;
        return this;
    }

    public ConfirmPopupRequest SetInvokeConfirmOnClose(bool invoke)
    {
        InvokeConfirmOnClose = invoke;
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

    public ConfirmPopupRequest SetPrimaryInteractable(bool interactable)
    {
        PrimaryInteractable = interactable;
        return this;
    }

    // RecruitmentManager 등에서 생성자 이후 서브메시지를 동적으로 변경할 때 사용
    // ex) 정원이 꽉 찼을 때만 경고 문구를 주입
    public ConfirmPopupRequest SetSubMessage(string subMessage)
    {
        SubMessage = subMessage;
        return this;
    }
}