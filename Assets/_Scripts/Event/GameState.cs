using System;

// 이벤트 시스템용 게임 상태 (날짜/턴 정보만 관리)
[System.Serializable]
public class GameState
{
    public DateTime CurrentDate { get; private set; }
    public int CurrentTurn { get; private set; }
    
    // 게임 페이즈 에서 처리와 중복이라서 제거

    public GameState(DateTime startDate)
    {
        CurrentDate = startDate;
        CurrentTurn = 0;
    }

    // TurnManager 기준 상태를 Event 시스템에 동기화
    public void SyncState(DateTime currentDate, int currentTurn)
    {
        CurrentDate = currentDate;
        CurrentTurn = currentTurn < 0 ? 0 : currentTurn;
    }
}
