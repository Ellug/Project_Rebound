using UnityEngine;
using UnityEngine.UI;

// 테스트용 팝업 클래스
public class TestPopup : UIBase
{
    [SerializeField] private Text _titleText;
    [SerializeField] private Button _closeButton;

    private void Start()
    {
        // 닫기 버튼 클릭 시 최상단 팝업 닫기
        if (_closeButton != null)
        {
            _closeButton.onClick.AddListener(() =>
            {
                UIManager.Instance.CloseTop();
            });
        }
    }

    // 팝업 구분을 위한 제목 설정
    public void SetTitle(string title)
    {
        if (_titleText != null)
        {
            _titleText.text = title;
        }

        Debug.Log($"Popup Opened : {title}");
    }

    public override void Close()
    {
        base.Close();
        Debug.Log("Popup Closed");
    }
}