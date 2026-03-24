using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class UIManager : Singleton<UIManager>
{
    private const string FixedContentRootName = "FixedContentRoot";

    // UI 관리용 스택
    private Stack<UIBase> _uiStack = new Stack<UIBase>();

    [Header("Settings")]
    [SerializeField] private Transform _canvasRoot;           // 팝업이 생성될 캔버스

    [Header("Popup Prefabs")]
    [SerializeField] private UIPopup _uiPopupPrefab;

    [Header("Popup Defaults")]
    [SerializeField] private string _defaultPopupPreviewImageId; // 기본 이미지 파일명 ID (Addressable)

    [Header("Student Select Prefab")]
    [SerializeField] private StudentSelectPopup _studentSelectPopupPrefab;

    [Header("Quit Poupup Prefab")]
    [SerializeField]private QuitPopup _quitPopupPrefab;

    private QuitPopup _quitPopupInstance;
    private InputSystem_Actions _input;

    // 타이틀 씬 등 외부에서 BackKey를 먼저 소비할 때 호출
    // HandleBackKey()가 같은 프레임에 실행되지 않도록 플래그로 차단
    private bool _backKeyConsumed;

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
    private void OnEnable()
    {
        _input?.Enable();
    }

    // 매니저가 비활성화될 때 인풋도 끄기
    private void OnDisable()
    {
        _input?.Disable();
    }

    private void OnDestroy()
    {
        SceneManager.sceneLoaded -= HandleSceneLoaded;
    }

    // 외부에서 캔버스 루트 참조 시 사용 (RecruitmentManager 등)
    public Transform GetCanvasRoot() => _canvasRoot;

    // 백스페이스 키 처리
    private void HandleBackKey()
    {
        if (_backKeyConsumed)
        {
            _backKeyConsumed = false;
            return;
        }

        if (_messengerStack.Count > 0)
        {
            _messengerStack.Peek().Close();
            return;
        }

        // QuitPopup이 열려있으면 닫기
        if (_quitPopupInstance != null
            && _quitPopupInstance.gameObject != null
            && _quitPopupInstance.gameObject.activeSelf)
        {
            _quitPopupInstance.Hide();
            return;
        }

        if (_uiStack.Count > 0)
        {
            while (_uiStack.Count > 0 && _uiStack.Peek() == null)
                _uiStack.Pop();

            if (_uiStack.Count == 0)
            {
                ShowQuitPopup();
                return;
            }

            if (_uiStack.Peek().DisableBackKey)
                return;

            // 안내팝업(UIPopup)이 열려있으면 QuitPopup 대신 닫기
            if (_uiStack.Peek() is UIPopup)
            {
                CloseTop();
                return;
            }

            _uiStack.Peek().OnBackKey();
            return;
        }

        ShowQuitPopup();
    }

    // 타이틀에서 게임 종료할 때 확인 팝업
    public void ShowQuitPopup()
    {
        if (_quitPopupPrefab == null) return;

        if (_quitPopupInstance == null || _quitPopupInstance.gameObject == null)
        {
            if (!EnsureCanvasRoot()) return;
            _quitPopupInstance = Instantiate(_quitPopupPrefab, _canvasRoot, false);
        }

        if (_quitPopupInstance.gameObject.activeSelf) return;

        _quitPopupInstance.transform.SetAsLastSibling();
        _quitPopupInstance.Show();
    }


    // UIPopupRequest 경로
    public UIPopup ShowPopup(UIPopupRequest request)
    {
        if (request == null) return null;

        if (_uiPopupPrefab == null)
        {
            Debug.LogError("[UIManager] Popup Prefab이 연결되지 않았습니다.");
            return null;
        }

        if (!EnsureCanvasRoot()) return null;

        ApplyPopupDefaults(request);

        // 1. 프리팹 생성
        UIPopup popupInstance = Instantiate(_uiPopupPrefab, _canvasRoot, false);
        popupInstance.transform.SetAsLastSibling(); // 항상 최상단에 표시

        // 2. 초기화 및 데이터 주입
        popupInstance.Init();
        popupInstance.Setup(request);
        popupInstance.Open();

        // 3. 스택에 추가
        _uiStack.Push(popupInstance);

        return popupInstance;
    }

    // 전역 기본 팝업 값 적용
    private void ApplyPopupDefaults(UIPopupRequest request)
    {
        if (request == null) return;
        if (request.Type != UIPopupRequest.PanelType.Default) return;
        if (!string.IsNullOrEmpty(request.PreviewImageId)) return;

        request.PreviewImageId = _defaultPopupPreviewImageId;
    }

    // PopupData 경로 (기존 호출 유지용 어댑터)
    public void ShowPopup(PopupData data)
    {
        ShowPopup(PopupRequestAdapter.FromPopupData(data));
    }


    public void OpenStudentSelect(int maxSelectCount, Action<List<Student>> onSelected, Action onCancelled, StudentCardPreviewDelta previewDelta = default)
    {
        if (_studentSelectPopupPrefab == null)
        {
            Debug.LogWarning("[UIManager] _studentSelectPopupPrefab이 null입니다.");
            onCancelled?.Invoke();
            return;
        }

        if (!EnsureCanvasRoot())
        {
            onCancelled?.Invoke();
            return;
        }

        StudentSelectPopup popup = Instantiate(_studentSelectPopupPrefab, _canvasRoot, false);
        popup.transform.SetAsLastSibling();

        popup.SetMaxSelectCount(maxSelectCount);
        popup.SetPreviewDelta(previewDelta);
        popup.Init();
        popup.Open();

        popup.OnSelectionConfirmed -= HandleSelected;
        popup.OnSelectionConfirmed += HandleSelected;

        popup.OnCancelled -= HandleCancelled;
        popup.OnCancelled += HandleCancelled;

        void HandleSelected(List<Student> students)
        {
            popup.OnSelectionConfirmed -= HandleSelected;
            popup.OnCancelled -= HandleCancelled;

            onSelected?.Invoke(students);

            popup.Close();
            Destroy(popup.gameObject);
        }

        void HandleCancelled()
        {
            popup.OnSelectionConfirmed -= HandleSelected;
            popup.OnCancelled -= HandleCancelled;

            onCancelled?.Invoke();

            popup.Close();
            Destroy(popup.gameObject);
        }
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

        UIBase top = _uiStack.Pop();
        top.Close();
        Destroy(top.gameObject);
    }

    // 특정 인스턴스를 닫기 (Top이 바뀌는 케이스 방지)
    public void Close(UIBase target)
    {
        if (target == null) return;

        //null이거나 스택에 없으면 그냥 파괴만 수행
        if (_uiStack.Count == 0)
        {
            target.Close();
            Destroy(target.gameObject);
            return;
        }

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

    // 캔버스 루트 관리
    private void HandleSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        RebindCanvasRoot();
        _quitPopupInstance = null;
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

        Transform activeSceneCanvasFallback = null;
        Transform anySceneFixedRootFallback = null;
        Transform anySceneCanvasFallback = null;

        for (int i = 0; i < canvases.Length; i++)
        {
            if (canvases[i] == null)
                continue;

            Transform canvasTransform = canvases[i].transform;
            Transform fixedContentRoot = FindFixedContentRoot(canvasTransform);

            if (anySceneFixedRootFallback == null && fixedContentRoot != null)
                anySceneFixedRootFallback = fixedContentRoot;

            if (anySceneCanvasFallback == null)
                anySceneCanvasFallback = canvasTransform;

            if (canvases[i].gameObject.scene != activeScene)
                continue;

            if (fixedContentRoot != null)
            {
                _canvasRoot = fixedContentRoot;
                return;
            }

            if (activeSceneCanvasFallback == null)
                activeSceneCanvasFallback = canvasTransform;
        }

        if (activeSceneCanvasFallback != null)
        {
            _canvasRoot = activeSceneCanvasFallback;
            return;
        }

        if (anySceneFixedRootFallback != null)
        {
            _canvasRoot = anySceneFixedRootFallback;
            return;
        }

        _canvasRoot = anySceneCanvasFallback;
    }

    private Transform FindFixedContentRoot(Transform canvasTransform)
    {
        if (canvasTransform == null)
            return null;

        Transform fixedRoot = canvasTransform.Find(FixedContentRootName);
        if (fixedRoot != null)
            return fixedRoot;

        Transform[] children = canvasTransform.GetComponentsInChildren<Transform>(true);
        for (int i = 0; i < children.Length; i++)
        {
            if (children[i] != null && children[i].name == FixedContentRootName)
                return children[i];
        }

        return null;
    }

    public void PushUI(UIBase ui)
    {
        if (ui != null && !_uiStack.Contains(ui))
        {
            _uiStack.Push(ui);
        }
    }

    public void ConsumeBackKey()
    {
        _backKeyConsumed = true;
    }
}
