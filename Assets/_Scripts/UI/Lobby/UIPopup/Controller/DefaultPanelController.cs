using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public sealed class DefaultPanelController : MonoBehaviour
{
    [SerializeField] private TMP_Text _txtTitle;                    // 제목
    [SerializeField] private TMP_Text _txtSub;                      // 서브 텍스트
    [SerializeField] private TMP_Text _txtMessage;                  // 본문
    [SerializeField] private Image _imgPreview;                     // 프리뷰 이미지

    [SerializeField] private Button _btnCancel;                     // 취소 버튼

    [SerializeField] private GameObject _primaryConfirmRoot;        // Confirm Primary 루트(프리팹에서 위치 고정)
    [SerializeField] private Button _btnConfirm;                    // 확인 버튼

    [SerializeField] private GameObject _primaryStartTrainingRoot;  // StartTraining Primary 루트(프리팹에서 위치 고정)
    [SerializeField] private Button _btnStartTraining;              // 훈련 시작 버튼

    // 요청 데이터를 Default 패널 UI에 바인딩
    // closeSelf: AutoClose 옵션에 따라 패널/팝업을 닫기 위해 외부에서 주입
    public void Bind(UIPopupRequest request, Action closeSelf)
    {
        if (_txtTitle != null) _txtTitle.text = request.Title ?? "";
        if (_txtMessage != null) _txtMessage.text = request.Message ?? "";

        if (_txtSub != null)
        {
            bool hasSub = !string.IsNullOrEmpty(request.SubMessage);
            _txtSub.gameObject.SetActive(hasSub);
            if (hasSub) _txtSub.text = request.SubMessage;
        }

        if (_imgPreview != null)
        {
            bool hasSprite = request.PreviewSprite != null;
            _imgPreview.gameObject.SetActive(hasSprite);
            if (hasSprite)
            {
                _imgPreview.sprite = request.PreviewSprite;
                _imgPreview.preserveAspect = true;
            }
        }

        if (_btnCancel != null)
        {
            _btnCancel.gameObject.SetActive(request.ShowCancel);
            _btnCancel.onClick.RemoveAllListeners();
            _btnCancel.onClick.AddListener(() =>
            {
                request.OnCancel?.Invoke();
                if (request.AutoCloseOnCancel)
                    closeSelf?.Invoke();
            });
        }

        ApplyPrimaryKind(request, closeSelf);
    }

    // Primary 버튼 종류(Confirm/StartTraining)에 따라 루트 토글 + 클릭 콜백 바인딩
    private void ApplyPrimaryKind(UIPopupRequest request, Action closeSelf)
    {
        if (_primaryConfirmRoot != null)
            _primaryConfirmRoot.SetActive(request.PrimaryKind == UIPopupRequest.PrimaryButtonKind.Confirm);

        if (_primaryStartTrainingRoot != null)
            _primaryStartTrainingRoot.SetActive(request.PrimaryKind == UIPopupRequest.PrimaryButtonKind.StartTraining);

        if (_btnConfirm != null)
        {
            _btnConfirm.interactable = request.PrimaryInteractable;
            _btnConfirm.onClick.RemoveAllListeners();
            _btnConfirm.onClick.AddListener(() =>
            {
                request.OnPrimary?.Invoke();
                if (request.AutoCloseOnPrimary)
                    closeSelf?.Invoke();
            });
        }

        if (_btnStartTraining != null)
        {
            _btnStartTraining.interactable = request.PrimaryInteractable;
            _btnStartTraining.onClick.RemoveAllListeners();
            _btnStartTraining.onClick.AddListener(() =>
            {
                request.OnPrimary?.Invoke();
                if (request.AutoCloseOnPrimary)
                    closeSelf?.Invoke();
            });
        }
    }
}