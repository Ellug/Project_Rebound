using System;
using System.Collections.Generic;
using UnityEngine;

// ConfirmPopup(범용) 요청 데이터
// - 이벤트/훈련/모달 전부 대응
public sealed class ConfirmPopupRequest
{
    public string Title;                                // 제목
    public string Message;                              // 본문 메시지
    public string SubMessage;                           // 서브 메시지

    public string PrimaryLabel;                         // 확인 버튼 라벨
    public Action PrimaryAction;                        // 확인 버튼 액션

    public string SecondaryLabel;                       // 취소 버튼 라벨
    public Action SecondaryAction;                      // 취소 버튼 액션

    public Sprite PreviewSprite;                        // 미리보기 이미지

    public bool IsModal = true;                         // 모달 여부

    // Auto close flags (ConfirmPopup.cs가 이 이름으로 접근함)
    public bool AutoCloseOnPrimary = true;              // 확인 후 자동 닫기
    public bool AutoCloseOnSecondary = true;            // 취소 후 자동 닫기

    public bool RequiresStudentSelection = false;       // 학생 선택 필요 여부
    public int MaxSelectCount = 0;                      // 최대 선택 인원
    public Action<List<Student>> OnStudentsSelected;    // 학생 선택 완료 콜백

    public bool PrimaryInteractable = true; // 확인 버튼 활성/비활성

    // 기본 생성자 (필수 값 중심)
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

    // Fluent Setter 영역

    // 모달 설정
    public ConfirmPopupRequest SetModal(bool isModal)
    {
        IsModal = isModal;
        return this;
    }

    // 서브 메시지 설정
    public ConfirmPopupRequest SetSubMessage(string subMessage)
    {
        SubMessage = subMessage;
        return this;
    }

    // 확인 버튼 자동 닫기 여부 설정
    public ConfirmPopupRequest SetAutoCloseOnPrimary(bool autoClose)
    {
        AutoCloseOnPrimary = autoClose;
        return this;
    }

    // 취소 버튼 자동 닫기 여부 설정
    public ConfirmPopupRequest SetAutoCloseOnSecondary(bool autoClose)
    {
        AutoCloseOnSecondary = autoClose;
        return this;
    }

    // 학생 선택 설정 (Fluent 방식)
    public ConfirmPopupRequest SetStudentSelection(
        bool requiresStudentSelection,
        int maxSelectCount,
        Action<List<Student>> onStudentsSelected)
    {
        RequiresStudentSelection = requiresStudentSelection;
        MaxSelectCount = Mathf.Max(0, maxSelectCount);
        OnStudentsSelected = onStudentsSelected;
        return this;
    }

    public ConfirmPopupRequest SetPrimaryInteractable(bool interactable)
    {
        PrimaryInteractable = interactable;
        return this;
    }
}