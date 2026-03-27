using UnityEngine;

public class SuddenEventTester : MonoBehaviour
{
    [Header("테스트할 이벤트 ID")]
    public string eventIdToTest = "event_000001";

    // 유니티 인스펙터의 우클릭(점 3개) 메뉴에서도 실행 가능하게 만듭니다.
    [ContextMenu("이벤트 강제 실행!")]
    public void TriggerEvent()
    {
        if (SuddenEventManager.Instance != null)
        {
            // 입력된 ID를 기반으로 이벤트를 강제로 찔러 넣습니다.
            SuddenEventManager.Instance.ExecuteEventById(eventIdToTest);
            Debug.Log($"[EventTester] '{eventIdToTest}' 강제 실행 요청 완료!");
        }
        else
        {
            Debug.LogWarning("[EventTester] 씬에 SuddenEventManager가 없습니다!");
        }
    }

    // 인게임 화면 좌측 상단에 개발자 전용 작은 UI를 그립니다. (빌드 시 지우거나 꺼두시면 됩니다)
    private void OnGUI()
    {
        // 화면 좌측 상단에 250x100 픽셀 크기의 영역 지정
        GUILayout.BeginArea(new Rect(10, 10, 250, 100));
        
        // 배경 박스
        GUI.Box(new Rect(0, 0, 250, 100), "개발자 이벤트 테스터");
        GUILayout.Space(25);

        // 입력 필드 (이벤트 ID 입력용)
        GUILayout.BeginHorizontal();
        GUILayout.Label("이벤트 ID:", GUILayout.Width(70));
        eventIdToTest = GUILayout.TextField(eventIdToTest);
        GUILayout.EndHorizontal();

        GUILayout.Space(10);

        // 실행 버튼
        if (GUILayout.Button("이벤트 강제 발생", GUILayout.Height(30)))
        {
            TriggerEvent();
        }

        GUILayout.EndArea();
    }
}