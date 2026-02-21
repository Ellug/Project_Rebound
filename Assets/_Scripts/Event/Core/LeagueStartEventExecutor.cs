using UnityEngine;

// 리그 시작 이벤트 실행기 (GameManager의 리그 상태를 오픈)
[CreateAssetMenu(menuName = "Game/Event/Executor/LeagueStartExecutor")]
public class LeagueStartEventExecutor : EventExecutor
{
    [SerializeField] private GameEvent _leagueStartNotifyEvent;

    public override void Execute(GameState gameState)
    {
        if (GameManager.Instance == null)
        {
            Debug.LogWarning("[LeagueStartExecutor] GameManager가 없습니다.");
            return;
        }

        GameManager.Instance.OpenLeague();

        if (_leagueStartNotifyEvent != null)
        {
            _leagueStartNotifyEvent.Raise();
        }
        else
        {
            Debug.LogWarning("[LeagueStartExecutor] Notify event is NULL");
        }
    }
}