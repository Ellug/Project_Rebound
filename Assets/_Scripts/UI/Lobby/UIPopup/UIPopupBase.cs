using UnityEngine;
using UnityEngine.UI;

// 팝업/패널 계열 공통 베이스
// - UIManager 스택/BackKey 처리 대상
// - "통합 팝업 Host(UIPopup)"가 아니라, StudentSelectPopup 같은 일반 UI도 상속 가능
public class UIPopupBase : UIBase
{
    [Header("Base Close (Optional)")]
    [SerializeField] private Button _btnClose; // X 버튼이 있는 UI만 연결

    protected virtual void Awake()
    {
        BindCloseButton();
    }

    public override void Init()
    {
        base.Init();
        BindCloseButton();
    }

    private void BindCloseButton()
    {
        if (_btnClose == null)
            return;

        _btnClose.onClick.RemoveAllListeners();
        _btnClose.onClick.AddListener(OnCloseButtonClicked);
    }

    // X 버튼/BackKey 시의 기본 동작
    // 파생 클래스에서 필요한 경우 override
    protected virtual void OnCloseButtonClicked()
    {
        CloseSelfByManager();
    }

    public override void OnBackKey()
    {
        // 기본: X 버튼과 동일 취급
        OnCloseButtonClicked();
    }

    protected void CloseSelfByManager()
    {
        if (UIManager.Instance != null)
        {
            UIManager.Instance.Close(this);
            return;
        }

        Close();
        Destroy(gameObject);
    }
}