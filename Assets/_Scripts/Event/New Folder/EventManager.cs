using System.Collections.Generic;
using UnityEngine;

public class EventManager : MonoBehaviour
{
    [SerializeField] private List<GameEventData> _eventList;

    private GameState _gameState;

    public void Initialize(GameState gameState)
    {
        _gameState = gameState;

        foreach (GameEventData eventData in _eventList)
        {
            if (eventData == null)
            {
                continue;
            }

            eventData.ResetRuntimeState();
        }
    }

    public void CheckEvents()
    {
        

        foreach (GameEventData eventData in _eventList)
        {
            
            eventData.TryExecute(_gameState);
        }
    }
}