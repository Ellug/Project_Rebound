using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class SettingsPanel : UIBase
{
    [SerializeField] private Button _btnClose;            // 설정창 닫기 버튼
    [SerializeField] private Button _btnRestartTutorial;  // 튜토리얼 다시 시작하기 버튼
    [SerializeField] private Button _btnGoTitle;          // 타이틀 가는 버튼

    [Header("애니메이션")]
    [SerializeField] private PopupAnimator _animator;

    // 타이틀 이동 전 옵션 패널을 먼저 닫기 위해 참조
    [Header("연결")]
    [SerializeField] private OptionUI _optionUI;

    public override void Init()
    {
        base.Init();

        // 닫기 버튼 — PlayOut 완료 후 UIManager에 정리 위임
        if (_btnClose != null)
        {
            _btnClose.onClick.RemoveAllListeners(); // 중복 방지
            _btnClose.onClick.AddListener(OnClickClose);
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

    public override void Open()
    {
        if (_animator == null)
        {
            Debug.LogWarning("[SettingsPanel] _animator가 연결되지 않았습니다. 인스펙터에서 PopupAnimator를 연결해주세요.");
            base.Open();
            return;
        }

        // SetActive(true) 전에 Initialize로 위치/스케일 초기화 보장
        _animator.Initialize();

        base.Open();

        _animator.PlayIn();
    }

    public override void Close()
    {
        if (!gameObject.activeSelf) return;

        PlayPopupCloseSfx();

        if (_animator == null)
        {
            gameObject.SetActive(false);
            return;
        }

        _animator.PlayOut(() => gameObject.SetActive(false));
    }

    // 닫기 버튼 전용 — PlayOut 완료 후 UIManager에 정리 위임
    // UIManager.Close()는 Close() 호출 후 즉시 Destroy하므로 직접 처리
    private void OnClickClose()
    {
        if (_animator == null)
        {
            UIManager.Instance.Close(this);
            return;
        }

        PlayPopupCloseSfx();

        _animator.PlayOut(() =>
        {
            gameObject.SetActive(false);
            UIManager.Instance.Close(this);
        });
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
        // 씬 전환 전 옵션 패널이 열려있으면 애니메이션 없이 즉시 닫음
        // (SceneRoot 스케일링 중 팝업이 별도로 떠있는 문제 방지)
        if (_optionUI != null)
            _optionUI.CloseImmediate();

        SceneTransitionManager.Instance.LoadScene("Title");
    }
}