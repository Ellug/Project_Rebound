using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

public class UIPopup : UIBase
{
    [Header("Layout")]
    [SerializeField] private RectTransform _windowRect; // 크기 재조정을 위한 배경 RectTransform
    [SerializeField] private VerticalLayoutGroup _windowLayoutGroup; // 패딩 조절을 위해 필요

    [Header("Padding Settings")]
    [SerializeField] private int _paddingTopWithImage = 100;    // 이미지가 있을 때 상단 패딩
    [SerializeField] private int _paddingTopNoImage = 40;     // 이미지가 없을 때 상단 패딩

    [Header("Content")]
    [SerializeField] private Image _contentImage;       // 상단 이미지 
    [SerializeField] private TMP_Text _txtTitle;        // 제목
    [SerializeField] private TMP_Text _txtSubContent;   // 부가 설명
    [SerializeField] private TMP_Text _txtContent;      // 본문

    [Header("Buttons")]
    [SerializeField] private Transform _buttonGridRoot; // 버튼 부모
    [SerializeField] private Button _buttonPrefab;      // 버튼 프리팹
    [SerializeField] private Button _closeButton;       // X 버튼

    public override void Init()
    {
        base.Init();
        if (_closeButton != null)
        {
            _closeButton.onClick.RemoveAllListeners();
            _closeButton.onClick.AddListener(OnCloseButtonClicked);
        }
    }

    public void SetData(PopupData data)
    {
        // 1. 이미지 처리
        bool hasImage = data.Image != null;

        if (data.Image != null && _contentImage != null)
        {
            _contentImage.gameObject.SetActive(true);
            _contentImage.sprite = data.Image;
            _contentImage.preserveAspect = true;
        }
        else if (_contentImage != null)
        {
            _contentImage.gameObject.SetActive(false);
        }

        if (_windowLayoutGroup != null)
        {
            // 구조체(RectOffset)는 직접 수정이 안 돼서 복사해서 넣어야 함
            RectOffset newPadding = _windowLayoutGroup.padding;
            newPadding.top = hasImage ? _paddingTopWithImage : _paddingTopNoImage;
            _windowLayoutGroup.padding = newPadding;
        }

        // 2. 텍스트 처리
        if (_txtTitle) _txtTitle.text = data.Title;
        if (_txtContent) _txtContent.text = data.Content;

        // 3. 서브 텍스트 처리
        if (!string.IsNullOrEmpty(data.SubContent) && _txtSubContent != null)
        {
            _txtSubContent.gameObject.SetActive(true);
            _txtSubContent.text = data.SubContent;
        }
        else if (_txtSubContent != null)
        {
            _txtSubContent.gameObject.SetActive(false);
        }

        // 4. 버튼 생성
        SetButtons(data.Buttons);

        // 5. 내용물에 맞춰 창 크기 즉시 갱신
        if (_windowRect != null)
        {
            LayoutRebuilder.ForceRebuildLayoutImmediate(_windowRect);
        }
    }

    private void SetButtons(List<PopupButtonInfo> buttons)
    {
        foreach (Transform child in _buttonGridRoot) Destroy(child.gameObject);

        if (buttons != null)
        {
            foreach (var btnInfo in buttons)
            {
                Button newBtn = Instantiate(_buttonPrefab, _buttonGridRoot);
                TMP_Text btnText = newBtn.GetComponentInChildren<TMP_Text>();
                if (btnText) btnText.text = btnInfo.Text;

                newBtn.onClick.AddListener(() =>
                {
                    btnInfo.OnClick?.Invoke();
                    if (btnInfo.AutoClose) UIManager.Instance.CloseTop();
                });
                newBtn.gameObject.SetActive(true);
            }
        }
    }

    protected virtual void OnCloseButtonClicked()
    {
        UIManager.Instance.CloseTop();
    }
}