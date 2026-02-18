using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class EventManager : MonoBehaviour
{
    [SerializeField] private List<GameEventData> _eventList;

    private GameState _gameState;

    public void Initialize(GameState gameState, bool resetRuntimeState = true)
    {
        _gameState = gameState;

        if (!resetRuntimeState || _eventList == null)
            return;

        foreach (GameEventData eventData in _eventList)
        {
            if (eventData == null)
                continue;

            eventData.ResetRuntimeState();
        }
    }

    public void CheckEvents()
    {
        if (_gameState == null || _eventList == null)
            return;

        foreach (GameEventData eventData in _eventList)
        {
            if (eventData == null)
                continue;

            eventData.TryExecute(_gameState);
        }
    }

    // GameManager가 다음 리그 날짜를 계산하기 위해 호출
    public bool TryGetNextLeagueDate(DateTime currentDate, out DateTime nextLeagueDate)
    {
        nextLeagueDate = default;

        if (_eventList == null || _eventList.Count == 0)
            return false;

        List<DateTime> candidateDates = new List<DateTime>();

        foreach (var eventData in _eventList)
        {
            if (eventData == null || eventData.Type != GameEventData.TriggerType.Date)
                continue;

            // 리그 시작 실행기를 쓰는 이벤트만 리그 일정으로 취급한다.
            if (!(eventData.Executor is LeagueStartEventExecutor))
                continue;

            // 올해 날짜
            if (TryGetEventDate(eventData, currentDate.Year, out DateTime thisYearDate))
            {
                if (thisYearDate.Date >= currentDate.Date)
                    candidateDates.Add(thisYearDate);
            }

            // 내년 날짜 (올해 리그가 모두 끝난 경우를 위해)
            if (TryGetEventDate(eventData, currentDate.Year + 1, out DateTime nextYearDate))
            {
                candidateDates.Add(nextYearDate);
            }
        }

        if (candidateDates.Count == 0)
            return false;

        nextLeagueDate = candidateDates.OrderBy(d => d).First();
        return true;
    }

    private bool TryGetEventDate(GameEventData eventData, int year, out DateTime eventDate)
    {
        eventDate = default;

        if (eventData == null)
            return false;

        int month = Mathf.Clamp(eventData.Month, 1, 12);
        int day = Mathf.Clamp(eventData.Day, 1, DateTime.DaysInMonth(year, month));

        try
        {
            eventDate = new DateTime(year, month, day);
            return true;
        }
        catch
        {
            return false;
        }
    }
}
