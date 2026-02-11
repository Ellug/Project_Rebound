using UnityEngine;

public abstract class EventExecutor : ScriptableObject
{
    public abstract void Execute(GameState gameState);
}