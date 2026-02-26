using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class SettingsPanel : UIBase
{
    [SerializeField] private Button _btnClose;  // 설정창 닫기 버튼
    [SerializeField] private Button _btnRestartTutorial;    // 튜토리얼 다시 시작하기 버튼

    public override void Init()
    {
        base.Init();

        // 닫기 버튼
        if (_btnClose != null)
        {
            _btnClose.onClick.AddListener(() =>
            {
                UIManager.Instance.Close(this);
            });
        }

        // 튜토리얼 재시작 버튼
        if (_btnRestartTutorial != null)
        {
            _btnRestartTutorial.onClick.AddListener(OnClickRestartTutorial);
        }
    }

    // 확인 팝업
    private void OnClickRestartTutorial()
    {
        var buttons = new List<PopupButtonInfo>
    {
        new PopupButtonInfo("취소"),
        new PopupButtonInfo("재시작", RestartTutorialConfirmed)
    };

        UIManager.Instance.ShowPopup(new PopupData(
            title: "튜토리얼 재시작",
            content: "튜토리얼 가이드를 다시 표시하시겠습니까?",
            buttons: buttons
        ));
    }

    private void RestartTutorialConfirmed()
    {
        TutorialGuidePrefs.ResetDismissed();

        var entry = FindFirstObjectByType<LobbyTutorialGuideEntry>(FindObjectsInactive.Include);
        entry?.RestartAndOpen();
    }
}