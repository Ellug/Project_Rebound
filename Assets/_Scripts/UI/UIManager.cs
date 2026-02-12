using UnityEngine;
using System.Collections.Generic;
using UnityEngine.InputSystem;

// 전체 UI의 스택 관리 및 뒤로가기 입력을 제어하는 매니저
public class UIManager : Singleton<UIManager>
{
    // UI 관리용 스택
    private Stack<UIBase> _uiStack = new Stack<UIBase>();

    // 팝업들이 생성될 부모 캔버스 (인스펙터에서 할당)
    [SerializeField] private Transform _canvasRoot;

    // 싱글톤 초기화 시 호출
    protected override void OnSingletonAwake()
    {
        // 씬 전환 시 스택 초기화가 필요하다면 이벤트 연결 등 처리
    }

    private void Update()
    {
        // 안드로이드 백버튼 입력 감지
        if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
        {
            if (_uiStack.Count > 0)
            {
                // 스택 최상단 UI의 뒤로가기 로직 수행
                _uiStack.Peek().OnBackKey();
            }
            else
            {
                // 스택에 UI가 없을 때 -> 종료 팝업 로직
                HandleExitInput();
            }
        }
    }

    // UI 프리팹을 받아서 띄우고 스택에 추가
    public T Show<T>(T uiPrefab) where T : UIBase
    {
        // 인스턴스화
        T uiInstance = Instantiate(uiPrefab, _canvasRoot);

        uiInstance.Init();
        uiInstance.Open();

        // 스택에 푸시
        _uiStack.Push(uiInstance);

        return uiInstance;
    }

    // 스택 최상단 UI 닫기
    public void CloseTop()
    {
        if (_uiStack.Count == 0) return;

        // 스택에서 제거 및 닫기 처리
        UIBase topUI = _uiStack.Pop();
        topUI.Close();

        // 생성된 객체 파괴
        Destroy(topUI.gameObject);
    }

    // 앱 종료 처리 또는 종료 확인 팝업
    private void HandleExitInput()
    {
        Debug.Log("종료 팝업 호출 필요");
    }
}