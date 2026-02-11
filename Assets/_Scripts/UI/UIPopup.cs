using UnityEngine;
using UnityEngine.UI;
using TMPro; // TextMeshPro 사용

// 제목과 닫기 버튼이 있는 공통 팝업 클래스
public class UIPopup : UIBase
{
    [Header("Common Popup Elements")]
    [SerializeField] private TMP_Text _titleText;
    [SerializeField] private Button _closeButton;

    // 초기화 시 닫기 버튼 이벤트를 자동으로 연결
    public override void Init()
    {
        base.Init();

        if (_closeButton != null)
        {
            _closeButton.onClick.RemoveAllListeners();
            _closeButton.onClick.AddListener(OnCloseButtonClicked);
        }
    }

    // 팝업 제목 설정
    public void SetTitle(string title)
    {
        if (_titleText != null)
        {
            _titleText.text = title;
        }
    }

    // 닫기 버튼 클릭 시 동작
    protected virtual void OnCloseButtonClicked()
    {
        UIManager.Instance.CloseTop();
    }
}