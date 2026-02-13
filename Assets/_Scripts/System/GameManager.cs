using UnityEngine.SceneManagement;

public class GameManager : Singleton<GameManager>
{
    protected override void OnSingletonAwake()
    {
        // GameManager 초기화 로직
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    void OnDestroy()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    // 씬 로드 이벤트 핸들러
    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        // Title 씬으로 돌아갈 때 StudentManager 정리
        if (scene.name == "Title")
            CleanupManagers();
    }

    // Title로 돌아갈 때 게임 데이터 정리
    private void CleanupManagers()
    {
        // StudentManager 정리
        if (StudentManager.Instance != null)
        {
            StudentManager.Instance.Cleanup();
            // 저장 로직도 여기서 한번에 하고, wantsToQuit 같은 곳에서 저장 처리하게 하는 방법도 있을듯?
            // 우선 저장은 세부 명세가 없으니 큰 그림만 고려해두기
        }

    }

    // 새 게임 시작
    public void StartNewGame()
    {
        // StudentFactory 초기화
        StudentFactory.ResetUsedNames();
        StudentFactory.ResetStudentIdCounter();

        // StudentManager 데이터 초기화
        if (StudentManager.Instance != null)
            StudentManager.Instance.ClearAllStudents();
    }
}
