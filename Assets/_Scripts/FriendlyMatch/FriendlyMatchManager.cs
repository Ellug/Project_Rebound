﻿using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class FriendlyMatchManager : Singleton<FriendlyMatchManager>
{
    public int MaxMonthlyCount = 3;

    private int _currentApplyCount = 0;
    private int _lastMonth = -1;

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
            // 달이 바뀌었다면 즉시 0으로 리셋!
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

        while (saturdays.Count < 3)
        {
            if (tempDate.DayOfWeek == DayOfWeek.Saturday)
            {
                if (GameManager.Instance == null || !GameManager.Instance.IsFriendlyMatchConfirmed || GameManager.Instance.FriendlyMatchDate.Date != tempDate.Date)
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
}