using UnityEngine;

[CreateAssetMenu(menuName = "Game/Event/Executor/NotifyExecutor")]
public class NotifyEventExecutor : EventExecutor
{
    [SerializeField] private GameEvent _notifyEvent;

    public override void Execute(GameState gameState)
    {
        if (_notifyEvent == null)
        {
            return;
        }

        _notifyEvent.Raise();
    }
}
//팝업 출력 로그등에 사용