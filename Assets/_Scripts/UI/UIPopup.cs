using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

public class UIPopup : UIBase
{
    [Header("UI Elements")]
    [SerializeField] private TMP_Text _txtTitle;    // 제목 텍스트
    [SerializeField] private TMP_Text _txtContent;  // 본문 텍스트 

    [Header("Dynamic Button Generation")]
    [SerializeField] private Transform _buttonGridRoot; // 버튼이 생성될 부모
    [SerializeField] private Button _buttonPrefab;      // 복제해서 사용할 버튼 프리팹

    [Header("Common Popup Elements")]
    [SerializeField] private Button _closeButton; // 우측 상단 X 버튼

    public override void Init()
    {
        base.Init();

        // X 버튼 이벤트 연결
        if (_closeButton != null)
        {
            _closeButton.onClick.RemoveAllListeners();
            _closeButton.onClick.AddListener(OnCloseButtonClicked);
        }
    }

    // 데이터를 받아서 UI를 갱신하는 메서드
    public void SetData(PopupData data)
    {
        // 1. 텍스트 설정
        if (_txtTitle) _txtTitle.text = data.Title;
        if (_txtContent) _txtContent.text = data.Content;

        // 2. 기존에 생성된 버튼이 있다면 모두 삭제
        foreach (Transform child in _buttonGridRoot)
        {
            Destroy(child.gameObject);
        }

        // 3. 데이터에 정의된 버튼만큼 동적 생성
        if (data.Buttons != null)
        {
            foreach (var btnInfo in data.Buttons)
            {
                CreateButton(btnInfo);
            }
        }
    }

    private void CreateButton(PopupButtonInfo info)
    {
        // 버튼 프리팹 생성
        Button newBtn = Instantiate(_buttonPrefab, _buttonGridRoot);

        // 버튼 텍스트 변경
        TMP_Text btnText = newBtn.GetComponentInChildren<TMP_Text>();
        if (btnText) btnText.text = info.Text;

        // 버튼 이벤트 연결
        newBtn.onClick.AddListener(() =>
        {
            // 기능 실행
            info.OnClick?.Invoke();

            // 팝업 닫기 옵션이 켜져있으면 닫기
            if (info.AutoClose)
            {
                UIManager.Instance.CloseTop();
            }
        });

        // 활성화 보장
        newBtn.gameObject.SetActive(true);
    }

    protected virtual void OnCloseButtonClicked()
    {
        UIManager.Instance.CloseTop();
    }
}