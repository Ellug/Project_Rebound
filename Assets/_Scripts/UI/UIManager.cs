using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

public class UIManager : Singleton<UIManager>
{
    // UI 관리용 스택
    private Stack<UIBase> _uiStack = new Stack<UIBase>();

    [Header("Settings")]
    [SerializeField] private Transform _canvasRoot;           // 팝업이 생성될 캔버스
    [SerializeField] private UIPopup _simplePopupPrefab;      // Simple 타입 팝업 프리팹 (기존 masterPopupPrefab)
    [SerializeField] private ConfirmPopup _confirmPopupPrefab;

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

    // 외부에서 캔버스 루트 참조 시 사용 (RecruitmentManager 등)
    public Transform GetCanvasRoot() => _canvasRoot;

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

    // Simple 팝업 호출 메서드
    // PopupData를 ConfirmPopupRequest로 변환하여 UIPopup.Setup(Simple) 호출
    public void ShowPopup(PopupData data)
    {
        ShowPopupWithPrefab(data, _simplePopupPrefab);
    }

    // 특정 프리팹으로 팝업 호출 (null이면 기본 프리팹)
    public void ShowPopup(PopupData data, UIPopup popupPrefab)
    {
        ShowPopupWithPrefab(data, popupPrefab != null ? popupPrefab : _simplePopupPrefab);
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
        popupInstance.transform.SetAsLastSibling(); // 항상 최상단에 표시

        // 2. 초기화 및 데이터 주입
        // PopupData를 ConfirmPopupRequest로 변환하여 Simple 타입으로 설정
        popupInstance.Init();
        popupInstance.Setup(ConvertToRequest(data), PopupType.Simple);
        popupInstance.Open();

        // 3. 스택에 추가
        _uiStack.Push(popupInstance);
    }

    // PopupData → ConfirmPopupRequest 변환
    // 기존 PopupData의 버튼 목록을 확인/취소 버튼으로 매핑
    private static ConfirmPopupRequest ConvertToRequest(PopupData data)
    {
        string primaryLabel = "확인";
        System.Action primaryAction = null;
        string secondaryLabel = null;
        System.Action secondaryAction = null;

        if (data.Buttons != null && data.Buttons.Count > 0)
        {
            // 버튼이 1개면 확인 버튼
            PopupButtonInfo first = data.Buttons[0];
            primaryLabel = first.Text;
            primaryAction = () => first.OnClick?.Invoke();

            // 버튼이 2개면 두 번째를 취소 버튼으로
            if (data.Buttons.Count > 1)
            {
                PopupButtonInfo second = data.Buttons[1];
                secondaryLabel = second.Text;
                secondaryAction = () => second.OnClick?.Invoke();
            }
        }

        return new ConfirmPopupRequest(
            title: data.Title,
            message: data.Content,
            primaryLabel: primaryLabel,
            primaryAction: primaryAction,
            secondaryLabel: secondaryLabel,
            secondaryAction: secondaryAction,
            subMessage: data.SubContent
        );
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

    // Confirm 팝업 호출 메서드
    public void ShowConfirm(ConfirmPopupRequest request)
    {
        ShowConfirmWithPrefab(request, _confirmPopupPrefab);
    }

    public void ShowConfirm(ConfirmPopupRequest request, ConfirmPopup popupPrefab)
    {
        ShowConfirmWithPrefab(request, popupPrefab != null ? popupPrefab : _confirmPopupPrefab);
    }

    private void ShowConfirmWithPrefab(ConfirmPopupRequest request, ConfirmPopup popupPrefab)
    {
        if (popupPrefab == null)
        {
            Debug.LogError("[UIManager] ConfirmPopup Prefab이 연결되지 않았습니다.");
            return;
        }

        if (!EnsureCanvasRoot())
            return;

        ConfirmPopup popupInstance = Instantiate(popupPrefab, _canvasRoot, false);
        popupInstance.transform.SetAsLastSibling();

        popupInstance.Init();
        popupInstance.Setup(request);
        popupInstance.Open();

        _uiStack.Push(popupInstance);
    }

    // 범용 UI 표시
    public T ShowUI<T>(T uiPrefab) where T : UIBase
    {
        if (uiPrefab == null)
        {
            Debug.LogError("[UIManager] ShowUI 실패: prefab이 null입니다.");
            return null;
        }

        if (!EnsureCanvasRoot())
            return null;

        T instance = Instantiate(uiPrefab, _canvasRoot, false);
        instance.transform.SetAsLastSibling();

        instance.Init();
        instance.Open();

        _uiStack.Push(instance);
        return instance;
    }

    // 중복 생성 감지용
    public T ShowUIUnique<T>(T uiPrefab) where T : UIBase
    {
        foreach (var item in _uiStack)
        {
            if (item is T existing && existing != null)
            {
                existing.transform.SetAsLastSibling();
                return existing;
            }
        }

        return ShowUI(uiPrefab);
    }

    // 닫기
    // 스택 최상단 UI 닫기
    public void CloseTop()
    {
        if (_uiStack.Count == 0) return;

        UIBase topUI = _uiStack.Pop();
        topUI.Close();
        Destroy(topUI.gameObject);
    }

    // 특정 인스턴스를 닫기 (Top이 바뀌는 케이스 방지)
    public void Close(UIBase target)
    {
        if (target == null) return;
        if (_uiStack.Count == 0) return;

        // 일반 케이스: target이 Top
        if (_uiStack.Peek() == target)
        {
            CloseTop();
            return;
        }

        // 예외 케이스: 중간에 끼어있는 UI 제거
        Stack<UIBase> buffer = new Stack<UIBase>(_uiStack.Count);
        bool removed = false;

        while (_uiStack.Count > 0)
        {
            UIBase item = _uiStack.Pop();
            if (item == target)
            {
                item.Close();
                Destroy(item.gameObject);
                removed = true;
                break;
            }

            buffer.Push(item);
        }

        while (buffer.Count > 0)
            _uiStack.Push(buffer.Pop());

        if (!removed)
        {
            // 스택에 없으면 그냥 파괴만 수행 (직접 Instantiate된 팝업 등)
            target.Close();
            Destroy(target.gameObject);
        }
    }

    private void ShowExitPopup()
    {
        var buttons = new List<PopupButtonInfo>
        {
            new PopupButtonInfo("취소", null),
            new PopupButtonInfo("종료", () => Application.Quit())
        };
        ShowPopup(new PopupData("게임 종료", "게임을 종료하시겠습니까?", buttons: buttons));
    }

    // 메신저 스택
    private Stack<UIBase> _messengerStack = new Stack<UIBase>();

    // 창이 열릴 때 스택에 넣기
    public void PushMessenger(UIBase ui)
    {
        _messengerStack.Push(ui);
    }

    // 창이 닫힐 때 스택에서 빼기
    public void PopMessenger(UIBase ui)
    {
        if (_messengerStack.Count > 0 && _messengerStack.Peek() == ui)
        {
            _messengerStack.Pop();
        }
    }

    // 뒤로가기 키 입력 감지 
    private void Update()
    {
        if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
        {
            // 열려있는 메신저 창이 있다면, 가장 위에 있는 창 닫기
            if (_messengerStack.Count > 0)
            {
                _messengerStack.Peek().Close();
                return;
            }
        }
    }

    // 캔버스 루트 관리
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