using System;

[System.Serializable]
public class GameState
{
    public DateTime CurrentDate { get; private set; }
    public int CurrentTurn { get; private set; }

    public GameState(DateTime startDate)
    {
        CurrentDate = startDate;
        CurrentTurn = 0;
    }

    public void AdvanceTurn()
    {
        CurrentTurn++;
        CurrentDate = CurrentDate.AddDays(1);
    }

    //CurrentDate -> 오늘 날짜

    //CurrentTurn -> 몇 턴째인지

    //AdvanceTurn() -> 하루 증가
}