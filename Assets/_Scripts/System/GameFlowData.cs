using System;
using System.Collections.Generic;

// 게임 진행 상태 데이터 (for 씬 전환 시 유지)
[Serializable]
public struct GameFlowData
{
    public bool HasFlowState;                  // 복원할 게임 상태가 있는지 여부
    public DateTime CurrentDate;               // 현재 날짜
    public int TurnIndex;                      // 현재 턴 번호
    public int DayIndex;                       // 게임 시작 이후 총 경과일
    public int CurrentYear;                    // 게임 내 연차 (n년차)
    public GamePhase Phase;                    // 현재 게임 페이즈
    public bool IsLeagueOpened;                // 리그 오픈 여부
    public bool IsLeagueHandled;               // 토너먼트 씬 진입 완료 여부
    public HashSet<string> ActiveEventIds;     // 현재 term 범위 내 활성 이벤트 id 집합

    // 초기값
    public static GameFlowData Default => new()
    {
        HasFlowState = false,
        CurrentDate = default,
        TurnIndex = 0,
        DayIndex = 0,
        CurrentYear = 1,
        Phase = GamePhase.Init,
        IsLeagueOpened = false,
        IsLeagueHandled = false,
        ActiveEventIds = new HashSet<string>(StringComparer.Ordinal)
    };

    public void Clear()
    {
        HasFlowState = false;
        CurrentDate = default;
        TurnIndex = 0;
        DayIndex = 0;
        CurrentYear = 1;
        Phase = GamePhase.Init;
        IsLeagueOpened = false;
        IsLeagueHandled = false;
        ActiveEventIds?.Clear();
    }

    // 동기화 : TurnManager 상태를 GameManager로 동기화 할 때 사용함
    public void Sync(DateTime currentDate, int turnIndex, int dayIndex, int currentYear, GamePhase phase, bool isLeagueOpened, bool isLeagueHandled)
    {
        CurrentDate = currentDate;
        TurnIndex = UnityEngine.Mathf.Max(0, turnIndex);
        DayIndex = UnityEngine.Mathf.Max(0, dayIndex);
        CurrentYear = UnityEngine.Mathf.Max(1, currentYear);
        Phase = phase;
        IsLeagueOpened = isLeagueOpened;
        IsLeagueHandled = isLeagueHandled;
        HasFlowState = true;
    }
}
