﻿using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class FriendlyMatchManager : Singleton<FriendlyMatchManager>
{
    public int MaxMonthlyCount = 3;

    private int _currentApplyCount = 0;
    private int _lastMonth = -1;

    public int LastMonth => _lastMonth; // 세이브/로드용으로 현재 월 정보 제공

    public int CurrentApplyCount
    {
        get
        {
            CheckMonthlyReset();
            return _currentApplyCount;
        }
    }

    private void CheckMonthlyReset()
    {
        TurnManager tm = FindFirstObjectByType<TurnManager>();
        if (tm != null && tm.DateManager != null)
        {
            int currentMonth = tm.DateManager.CurrentDate.Month;

            // 처음 체크할 때는 현재 달로 설정만 함
            if (_lastMonth == -1)
            {
                _lastMonth = currentMonth;
            }
            // 달이 바뀌었다면 즉시 0으로 리셋
            else if (_lastMonth != currentMonth)
            {
                _lastMonth = currentMonth;
                _currentApplyCount = 0;
                Debug.Log($"[FriendlyMatch] {currentMonth}월이 되어 친선경기 신청 횟수가 리셋되었습니다.");
            }
        }
    }

    public void RollbackApplyCount()
    {
        if (_currentApplyCount > 0)
        {
            _currentApplyCount--;
            Debug.Log("[FriendlyMatch] 신청 취소로 인해 횟수가 복구되었습니다.");
        }
    }

    public Dictionary<DateTime, string> GetBookedMatchSchedule(int currentYear)
    {
        Dictionary<DateTime, string> schedule = new Dictionary<DateTime, string>();
        if (MessengerManager.Instance == null) return schedule;

        foreach (var room in MessengerManager.Instance.ActiveRooms)
        {
            if (room.RoomId.StartsWith("friendly_"))
            {
                foreach (var msg in room.Messages)
                {
                    if (msg.EventType == MessageEventType.System && msg.Content.Contains("친선전 일정이 잡혔습니다"))
                    {
                        int index = msg.Content.IndexOf("에 친선전 일정이");
                        if (index > 0)
                        {
                            string dateStr = msg.Content.Substring(0, index).Trim();
                            string[] parts = dateStr.Split(new char[] { '월', '일' }, StringSplitOptions.RemoveEmptyEntries);
                            if (parts.Length >= 2 && int.TryParse(parts[0], out int m) && int.TryParse(parts[1], out int d))
                            {
                                try
                                {
                                    DateTime dt = new DateTime(currentYear, m, d).Date;
                                    if (!schedule.ContainsKey(dt))
                                    {
                                        schedule.Add(dt, room.RoomName);
                                    }
                                }
                                catch { }
                            }
                        }
                    }
                }
            }
        }
        return schedule;
    }

    public bool StartFriendlyMatch(string schoolId, string schoolName)
    {
        TurnManager tm = FindFirstObjectByType<TurnManager>();
        if (tm == null || tm.DateManager == null) return false;

        DateTime currentDate = tm.DateManager.CurrentDate;

        // 1. 횟수 제한 체크 (여기서 CurrentApplyCount를 부르며 자동으로 리셋 여부가 체크됨)
        if (CurrentApplyCount >= MaxMonthlyCount)
        {
            Debug.Log("이번 달 친선전 신청 횟수를 모두 소진했습니다.");
            return false;
        }

        // 2. 횟수 차감 
        _currentApplyCount++;

        // 3. 현재 인게임 날짜 기준으로 가장 가까운 토요일 3개
        List<DateTime> saturdays = new List<DateTime>();
        DateTime tempDate = currentDate.AddDays(1); // 내일부터 탐색 시작
        Dictionary<DateTime, string> schedule = GetBookedMatchSchedule(currentDate.Year);

        while (saturdays.Count < 3)
        {
            if (tempDate.DayOfWeek == DayOfWeek.Saturday)
            {
                bool isBookedInGameManager = GameManager.Instance != null && GameManager.Instance.IsFriendlyMatchConfirmed && GameManager.Instance.FriendlyMatchDate.Date == tempDate.Date;
                bool isBookedInChat = schedule.ContainsKey(tempDate.Date);

                if (!isBookedInGameManager && !isBookedInChat)
                {
                    saturdays.Add(tempDate);
                }
            }
            tempDate = tempDate.AddDays(1);
        }

        // 4. 치환할 텍스트(보간법) 데이터 준비
        Dictionary<string, string> textVars = new Dictionary<string, string>
        {
            { "{school_choice}", schoolName },
            { "{date1}", saturdays[0].ToString("M월 d") },
            { "{date2}", saturdays[1].ToString("M월 d") },
            { "{date3}", saturdays[2].ToString("M월 d") },
            { "{date_choice}", "" }
        };

        // 5. 친선전 전용 대화 러너 실행
        string roomId = $"friendly_{schoolId}";

        int msgStartIndex = 0;
        if (MessengerManager.Instance != null)
        {
            var room = MessengerManager.Instance.ActiveRooms.FirstOrDefault(r => r.RoomId == roomId);
            if (room != null) msgStartIndex = room.Messages.Count;
        }
        FriendlyMatchRunner.Instance.PlayDialogue(roomId, schoolName, "diag_schedule_001", "index_001", textVars, msgStartIndex);

        return true;
    }

    // 세이브 데이터에서 친선전 신청 횟수와 마지막 신청 월을 복원하는 메서드
    public void RestoreApplyCount(int count, int lastMonth)
    {
        _currentApplyCount = count;
        _lastMonth = lastMonth;
        Debug.Log($"[FriendlyMatch] 세이브 복원 | applyCount={count}, lastMonth={lastMonth}");
    }
}