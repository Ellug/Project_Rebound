using UnityEngine;

[CreateAssetMenu(menuName = "Game/Event/Executor/LeagueStartExecutor")]
public class LeagueStartEventExecutor : EventExecutor
{
    [SerializeField] private GameEvent _leagueStartNotifyEvent;

    public override void Execute(GameState gameState)
    {
        if (gameState == null)
        {
            return;
        }

        gameState.OpenLeague();

        if (_leagueStartNotifyEvent != null)
        {
            _leagueStartNotifyEvent.Raise();
        }

        Debug.Log("[League] OpenLeague executed.");
    }
}