using System;
using UnityEngine;

public class EventCondition
{
    public int RequiredTurn { get; private set; }
    public DateTime RequiredDate { get; private set; }

    public EventCondition(int requiredTurn)
    {
        RequiredTurn = requiredTurn;
    }

    public bool IsSatisfied(int currentTurn, DateTime currentDate)
    {
        if (currentTurn < RequiredTurn)
        {
            return false;
        }

        return true;
    }
}
