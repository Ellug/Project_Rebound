using System;
using UnityEngine;

[CreateAssetMenu(menuName = "Game/Event/EventData")]
public class GameEventData : ScriptableObject
{
    [SerializeField] private string _eventName;

    [Header("Trigger Date")]
    [SerializeField] private int _year;
    [SerializeField] private int _month;
    [SerializeField] private int _day;

    [SerializeField] private bool _isOneTimeEvent = true;
    [SerializeField] private EventExecutor _executor;

    private bool _hasExecuted;

    public void TryExecute(GameState gameState)
    {
        if (_hasExecuted && _isOneTimeEvent)
        {
            return;
        }

        DateTime triggerDate = new DateTime(_year, _month, _day);

        if (gameState.CurrentDate.Date != triggerDate.Date)
        {
            return;
        }

        Debug.Log($"[Event Triggered] {_eventName}");

        _executor?.Execute(gameState);
        _hasExecuted = true;
    }
}