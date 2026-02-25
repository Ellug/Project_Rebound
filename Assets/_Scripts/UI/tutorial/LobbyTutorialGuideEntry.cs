using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

// 로비 화면 내 튜토리얼 가이드 진입 버튼 관리
public class LobbyTutorialGuideEntry : MonoBehaviour
{
    [Header("Root")]
    [SerializeField] private GameObject _root; // 전체 표시 루트

    [Header("Buttons")]
    [SerializeField] private Button _btnOpenGuide; // 가이드 열기 버튼
    [SerializeField] private Button _btnCloseX;    // 닫기(X) 버튼

    [Header("Popup Prefab")]
    [SerializeField] private TutorialGuidePopup _tutorialGuidePopupPrefab; // 가이드 팝업 프리팹

    private bool _isInited; // 이벤트 중복 바인딩 방지용

    private void Start()
    {
        InitIfNeeded();
        RefreshVisible(); // 현재 저장 상태 기준으로 표시 여부 갱신
    }

    // 버튼 이벤트 1회 바인딩
    private void InitIfNeeded()
    {
        if (_isInited) return;
        _isInited = true;

        if (_btnOpenGuide != null)
        {
            _btnOpenGuide.onClick.RemoveAllListeners();
            _btnOpenGuide.onClick.AddListener(OpenGuidePopup);
        }

        if (_btnCloseX != null)
        {
            _btnCloseX.onClick.RemoveAllListeners();
            _btnCloseX.onClick.AddListener(OnClickCloseX);
        }
    }

    // PlayerPrefs 기준으로 표시 여부 결정
    private void RefreshVisible()
    {
        bool shouldShow = !TutorialGuidePrefs.IsDismissed;
        if (_root != null) _root.SetActive(shouldShow);
    }

    // 가이드 팝업 열기
    private void OpenGuidePopup()
    {
        if (_tutorialGuidePopupPrefab == null)
        {
            Debug.LogWarning("[LobbyTutorialGuideEntry] _tutorialGuidePopupPrefab이 null입니다.");
            return;
        }

        UIManager.Instance.ShowUI(_tutorialGuidePopupPrefab);
    }

    // X 버튼 클릭 시 안내 팝업 표시 후 비활성 처리
    private void OnClickCloseX()
    {
        var buttons = new List<PopupButtonInfo>
        {
            new PopupButtonInfo("확인", () =>
            {
                TutorialGuidePrefs.SetDismissed(true); // 다시 보지 않음 처리
                RefreshVisible();                      // UI 갱신
            })
        };

        UIManager.Instance.ShowPopup(new PopupData(
            title: "안내",
            content: "튜토리얼 가이드는 언제든지\n환경설정에서 다시 볼 수 있습니다.",
            buttons: buttons
        ));
    }
}