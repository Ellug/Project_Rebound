using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

// - 로비에서 튜토리얼 엔트리(말풍선) 표시/숨김을 관리
// - X 버튼: "닫기 확인 팝업"을 띄우고, 확인 시 엔트리+오버레이를 같이 끈다.
public class LobbyTutorialGuideEntry : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private GameObject _overlayBlocker;   // 다른 버튼 입력 막는 검정 패널
    [SerializeField] private Button _btnOpenTutorial;      // "튜토리얼 열기" 버튼(말풍선 본문)
    [SerializeField] private Button _btnCloseX;            // 말풍선 우상단 X 버튼

    [Header("Options")]
    [SerializeField] private bool _openOnLobbyEnter = true;

    private void Awake()
    {
        BindButtons();

        // 로비 진입 시 자동 표시 (다시 보지 않기면 스킵)
        if (_openOnLobbyEnter && !TutorialGuidePrefs.IsDismissed())
        {
            ShowEntry(true);
        }
        else
        {
            ShowEntry(false);
        }
    }

    private void BindButtons()
    {
        if (_btnOpenTutorial != null)
        {
            _btnOpenTutorial.onClick.RemoveAllListeners();
            _btnOpenTutorial.onClick.AddListener(OpenTutorial);
        }

        if (_btnCloseX != null)
        {
            _btnCloseX.onClick.RemoveAllListeners();
            _btnCloseX.onClick.AddListener(OnClickCloseX);
        }
    }

    // 외부(세팅 패널 등)에서 엔트리만 다시 켜고 싶을 때
    public void ShowEntry(bool show)
    {
        if (_overlayBlocker != null)
            _overlayBlocker.SetActive(show);

        gameObject.SetActive(show);
    }

    // SettingsPanel에서 호출: 튜토리얼 다시보기(엔트리 다시 표시)
    public void RestartAndShowEntry()
    {
        TutorialGuidePrefs.ResetDismissed();
        ShowEntry(true);
    }

    private void OnClickCloseX()
    {
        if (UIManager.Instance == null)
        {
            // UIManager가 없으면 안전하게 그냥 끈다
            ShowEntry(false);
            return;
        }

        UIManager.Instance.ShowPopup(UIPopupRequest.Simple(
            title: "안내",
            message: "'튜토리얼 가이드는' 언제든지\n'환경설정'에서 다시 볼 수 있습니다.",
            onPrimary: () =>
            {
                // X 닫기는 재노출 방지 상태로 저장한다.
                TutorialGuidePrefs.SetDismissed(true);
                // 확인 시 엔트리 + 오버레이 같이 끄기
                ShowEntry(false);
            },
            onCancel: () =>
            {
            },
            showCancel: false,
            autoCloseOnPrimary: true,
            autoCloseOnCancel: true
        ));
    }
    public void OpenTutorial()
    {
        if (UIManager.Instance == null)
        {
            Debug.LogWarning("[LobbyTutorialGuideEntry] UIManager.Instance가 없습니다.");
            return;
        }

        TutorialGuideTableSO table = CachedSOData.Get<TutorialGuideTableSO>();
        if (table == null)
        {
            Debug.LogWarning("[LobbyTutorialGuideEntry] CachedSOData.TutorialGuideTable이 null입니다.");
            return;
        }

        List<UIPopupRequest.GuidePage> pages = TutorialGuidePrefs.BuildPages(table);
        if (pages == null || pages.Count == 0)
        {
            Debug.LogWarning("[LobbyTutorialGuideEntry] 튜토리얼 페이지가 비어있습니다.");
            return;
        }

        // 가이드 팝업을 열면, 엔트리(말풍선+검정패널)는 꺼둔다(원하면 유지로 바꿔도 됨)
        UIPopupRequest req = UIPopupRequest.Guide(
            title: "튜토리얼",
            pages: pages,
            onClose: () =>
            {
                // 마지막 닫기 버튼(Guide Close) 클릭 시 다시 안보기 처리
                TutorialGuidePrefs.SetDismissed(true);
                // 원하면 여기서 엔트리 완전 종료 유지(현재는 이미 꺼져있음)
            },
            onCancel: null
        );

        req.ShowCancel = false;
        req.AutoCloseOnPrimary = true;
        req.AutoCloseOnCancel = true;

        UIManager.Instance.ShowPopup(req);
    }
}
