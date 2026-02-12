using System;
using UnityEngine;

[CreateAssetMenu(menuName = "Game/Event/EventData")]
public class GameEventData : ScriptableObject
{
    public enum TriggerType
    {
        Turn = 0,
        Date = 1
    }

    [Header("Common")]
    [SerializeField] private string _eventName;
    [SerializeField] private bool _isOneTimeEvent = true;
    [SerializeField] private EventExecutor _executor;

    [Header("Trigger")]
    [SerializeField] private TriggerType _triggerType = TriggerType.Turn;
    [SerializeField] private int _triggerTurn = 1;
    [SerializeField] private int _year = 2026;
    [SerializeField] private int _month = 1;
    [SerializeField] private int _day = 1;

    [NonSerialized] private bool _hasExecuted; // 명시적으로 런타임 전용

    public string EventName => _eventName;
    public TriggerType Type => _triggerType;

    public void ResetRuntimeState()
    {
        _hasExecuted = false;
    }

    public void TryExecute(GameState gameState)
    {
        if (gameState == null)
        {
            return;
        }

        if (_isOneTimeEvent && _hasExecuted)
        {
            return;
        }

        if (!IsTriggered(gameState))
        {
            return;
        }

        

        _executor?.Execute(gameState);
        _hasExecuted = true;
    }

    private bool IsTriggered(GameState gameState)
    {
        if (_triggerType == TriggerType.Turn)
        {
            return IsTriggeredByTurn(gameState);
        }

        return IsTriggeredByDate(gameState);
    }

    private bool IsTriggeredByTurn(GameState gameState)
    {
        if (_triggerTurn <= 0)
        {
            return false;
        }

        return gameState.CurrentTurn >= _triggerTurn;
    }

    private bool IsTriggeredByDate(GameState gameState)
    {
        if (!TryGetTriggerDate(out DateTime triggerDate))
        {
            Debug.LogWarning($"[EventData] Invalid trigger date: {_eventName} ({_year}-{_month}-{_day})");
            return false;
        }

        return gameState.CurrentDate.Date >= triggerDate.Date;
    }

    private bool TryGetTriggerDate(out DateTime triggerDate)
    {
        triggerDate = default;

        if (_year <= 0 || _month <= 0 || _day <= 0)
        {
            return false;
        }

        try
        {
            triggerDate = new DateTime(_year, _month, _day);
            return true;
        }
        catch
        {
            return false;
        }
    }
}