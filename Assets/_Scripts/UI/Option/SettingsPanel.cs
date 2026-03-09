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
        // 1) 다시 보지 않기 해제
        TutorialGuidePrefs.ResetDismissed();

        // 2) 안내 팝업에서 "확인"을 누르는 순간 Entry를 다시 켠다
        UIManager.Instance.ShowPopup(new PopupData(
            title: "안내",
            content: "튜토리얼은 로비에서 다시 표시됩니다.",
            buttons: new List<PopupButtonInfo>
            {
            new PopupButtonInfo(() =>
            {
                // 비활성 포함 검색
                var entry = FindFirstObjectByType<LobbyTutorialGuideEntry>(FindObjectsInactive.Include);
                if (entry != null)
                {
                    
                    // entry.ShowEntry(true);          // 엔트리+검정패널만 다시 띄움
                    entry.RestartAndShowEntry();       // Prefs 리셋 포함 버전이면 이걸 권장
                }
                else
                {
                    Debug.LogWarning("[SettingsPanel] LobbyTutorialGuideEntry를 찾지 못했습니다.");
                }
            })
            }
        ));
    }

    private void OnClickGoTitle()
    {
        SceneManager.LoadScene("Title");
    }
}