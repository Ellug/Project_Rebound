using System;


public abstract class GameEvent
{
    public string EventName { get; protected set; }
    public EventCondition Condition { get; protected set; }

    protected GameEvent(string eventName, EventCondition condition)
    {
        EventName = eventName;
        Condition = condition;
    }

    public bool CanExecute(int currentTurn, DateTime currentDate)
    {
        return Condition.IsSatisfied(currentTurn, currentDate);
    }

    public abstract void Execute();
}