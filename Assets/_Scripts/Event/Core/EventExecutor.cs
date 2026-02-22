using UnityEngine;

public abstract class EventExecutor : ScriptableObject
{
    public abstract void Execute(GameState gameState);
}
//모든 이벤트 실행 모듈은 이걸 상속