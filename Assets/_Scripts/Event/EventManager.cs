using System;
using System.Collections.Generic;
using UnityEngine;

public class EventManager : MonoBehaviour
{
    private List<GameEvent> _registeredEvents = new List<GameEvent>();

    public void RegisterEvent(GameEvent gameEvent)
    {
        if (gameEvent == null)
        {
            return;
        }

        _registeredEvents.Add(gameEvent);
    }

    public void CheckAndExecuteEvents(int currentTurn, DateTime currentDate)
    {
        for (int i = _registeredEvents.Count - 1; i >= 0; i--)
        {
            GameEvent gameEvent = _registeredEvents[i];

            if (gameEvent.CanExecute(currentTurn, currentDate))
            {
                gameEvent.Execute();
                _registeredEvents.RemoveAt(i);
            }
        }
    }
}