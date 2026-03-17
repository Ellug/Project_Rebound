using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class SettingsPanel : UIBase
{
    [SerializeField] private Button _btnClose;            // 설정창 닫기 버튼
    [SerializeField] private Button _btnRestartTutorial;  // 튜토리얼 다시 시작하기 버튼
    [SerializeField] private Button _btnGoTitle;          // 타이틀 가는 버튼

    public override void Init()
    {
        base.Init();

        // 닫기 버튼
        if (_btnClose != null)
        {
            _btnClose.onClick.RemoveAllListeners(); // 중복 방지
            _btnClose.onClick.AddListener(() =>
            {
                UIManager.Instance.Close(this);
            });
        }

        // 튜토리얼 재시작 버튼
        if (_btnRestartTutorial != null)
        {
            _btnRestartTutorial.onClick.RemoveAllListeners(); // 중복 방지
            _btnRestartTutorial.onClick.AddListener(OnClickRestartTutorial);
        }

        // 타이틀 이동 버튼
        if (_btnGoTitle != null)
        {
            _btnGoTitle.onClick.RemoveAllListeners();
            _btnGoTitle.onClick.AddListener(OnClickGoTitle);
        }
    }

    // 확인 팝업
    private void OnClickRestartTutorial()
    {
        var buttons = new List<PopupButtonInfo>
        {
            new PopupButtonInfo(() => { }),
            new PopupButtonInfo(RestartTutorialConfirmed)
        };

        UIManager.Instance.ShowPopup(new PopupData(
            title: "튜토리얼 재시작",
            content: "튜토리얼 가이드를 다시 표시하시겠습니까?",
            buttons: buttons
        ));
    }

    private void RestartTutorialConfirmed()
    {
        if (UIManager.Instance == null)
        {
            Debug.LogWarning("[SettingsPanel] UIManager.Instance가 없습니다.");
            return;
        }

        TutorialGuideTableSO table = CachedSOData.Get<TutorialGuideTableSO>();
        if (table == null)
        {
            Debug.LogWarning("[SettingsPanel] CachedSOData.TutorialGuideTable이 null입니다.");
            return;
        }

        List<UIPopupRequest.GuidePage> pages = TutorialGuidePrefs.BuildPages(table);
        if (pages == null || pages.Count == 0)
        {
            Debug.LogWarning("[SettingsPanel] 튜토리얼 페이지가 비어있습니다.");
            return;
        }

        UIPopupRequest req = UIPopupRequest.Guide(
            title: "튜토리얼",
            pages: pages,
            onClose: () =>
            {
                TutorialGuidePrefs.SetDismissed(true);
            },
            onCancel: null
        );

        req.ShowCancel = false;
        req.AutoCloseOnPrimary = true;
        req.AutoCloseOnCancel = true;

        UIManager.Instance.ShowPopup(req);
    }

    private void OnClickGoTitle()
    {
        SceneManager.LoadScene("Title");
    }
}