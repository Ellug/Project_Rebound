using UnityEngine;
using System.Collections.Generic;
using UnityEngine.InputSystem; // New Input System
using UnityEngine.SceneManagement;

public class UIManager : Singleton<UIManager>
{
    // UI 관리용 스택
    private Stack<UIBase> _uiStack = new Stack<UIBase>();

    [Header("Settings")]
    [SerializeField] private Transform _canvasRoot;      // 팝업이 생성될 캔버스
    [SerializeField] private UIPopup _masterPopupPrefab; // 기본 팝업 프리팹

    // Input Action Asset으로 생성된 C# 클래스
    private InputSystem_Actions _input;

    protected override void OnSingletonAwake()
    {
        // 1. 인풋 클래스 생성
        _input = new InputSystem_Actions();

        // 2. 이벤트 바인딩
        // UI맵의 Cancel액션이 발동되면 HandleBackKey 함수 실행
        _input.UI.Cancel.performed += ctx => HandleBackKey();

        SceneManager.sceneLoaded += HandleSceneLoaded;
        RebindCanvasRoot();
    }

    // 매니저가 활성화될 때 인풋도 켜기
    void OnEnable()
    {
        _input?.Enable();
    }

    // 매니저가 비활성화될 때 인풋도 끄기
    void OnDisable()
    {
        _input?.Disable();
    }

    void OnDestroy()
    {
        SceneManager.sceneLoaded -= HandleSceneLoaded;
    }

    private void HandleBackKey()
    {
        if (_uiStack.Count > 0)
        {
            // 스택 최상단 팝업의 뒤로가기 로직 수행
            _uiStack.Peek().OnBackKey();
        }
        else
        {
            // 스택에 팝업이 없으면 종료 팝업 띄우기
            ShowExitPopup();
        }
    }

    // 팝업 호출 메서드
    public void ShowPopup(PopupData data)
    {
        ShowPopupWithPrefab(data, _masterPopupPrefab);
    }

    // 특정 프리팹으로 팝업 호출 (null이면 기본 프리팹)
    public void ShowPopup(PopupData data, UIPopup popupPrefab)
    {
        ShowPopupWithPrefab(data, popupPrefab != null ? popupPrefab : _masterPopupPrefab);
    }

    private void ShowPopupWithPrefab(PopupData data, UIPopup popupPrefab)
    {
        if (popupPrefab == null)
        {
            Debug.LogError("[UIManager] Popup Prefab이 연결되지 않았습니다.");
            return;
        }

        if (!EnsureCanvasRoot())
            return;

        // 1. 프리팹 생성
        UIPopup popupInstance = Instantiate(popupPrefab, _canvasRoot, false);
        popupInstance.transform.SetAsLastSibling();

        // 2. 초기화 및 데이터 주입
        popupInstance.Init();
        popupInstance.SetData(data); // 데이터 밀어넣기
        popupInstance.Open();

        // 3. 스택에 추가
        _uiStack.Push(popupInstance);
    }

    // 편의용 오버로딩
    public void ShowPopup(string title, string content, string confirmText = "확인", System.Action onConfirm = null)
    {
        var buttons = new List<PopupButtonInfo>
        {
            new PopupButtonInfo(confirmText, onConfirm)
        };
        ShowPopup(new PopupData(title, content, buttons: buttons));
    }

    // 스택 최상단 UI 닫기
    public void CloseTop()
    {
        if (_uiStack.Count == 0) return;

        UIBase topUI = _uiStack.Pop();
        topUI.Close();
        Destroy(topUI.gameObject);
    }

    // 종료 팝업 예시
    private void ShowExitPopup()
    {
        var buttons = new List<PopupButtonInfo>
        {
            new PopupButtonInfo("취소", null),
            new PopupButtonInfo("종료", () => Application.Quit())
        };
        ShowPopup(new PopupData("게임 종료", "게임을 종료하시겠습니까?", buttons: buttons));
    }

    private void HandleSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        RebindCanvasRoot();
    }

    private bool EnsureCanvasRoot()
    {
        if (IsCanvasRootValid())
            return true;

        RebindCanvasRoot();
        if (IsCanvasRootValid())
            return true;

        Debug.LogError("[UIManager] Canvas Root를 찾지 못해 팝업을 표시할 수 없습니다.");
        return false;
    }

    private bool IsCanvasRootValid()
    {
        return _canvasRoot != null
            && _canvasRoot.gameObject.scene.IsValid()
            && _canvasRoot.gameObject.scene.isLoaded;
    }

    private void RebindCanvasRoot()
    {
        Scene activeScene = SceneManager.GetActiveScene();
        Canvas[] canvases = FindObjectsByType<Canvas>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);

        for (int i = 0; i < canvases.Length; i++)
        {
            if (canvases[i] == null)
                continue;

            if (canvases[i].gameObject.scene == activeScene)
            {
                _canvasRoot = canvases[i].transform;
                return;
            }
        }

        if (canvases.Length > 0 && canvases[0] != null)
            _canvasRoot = canvases[0].transform;
    }
}
