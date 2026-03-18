using System;
using System.Collections.Generic;
using UnityEngine;

public class FriendlyMatchManager : Singleton<FriendlyMatchManager>
{
    public int MaxMonthlyCount = 3;
    public int CurrentApplyCount { get; private set; } = 0;

    private int _lastMonth = -1;
    private bool _isEventHooked = false;

    private void Start()
    {
        HookDateEvent();
    }

    // DateManager의 날짜 변경 이벤트를 구독하여 달이 바뀌면 알아서 초기화
    private void HookDateEvent()
    {
        if (_isEventHooked) return;

        TurnManager tm = FindFirstObjectByType<TurnManager>();
        if (tm != null && tm.DateManager != null)
        {
            _lastMonth = tm.DateManager.CurrentDate.Month;
            tm.DateManager.OnDateAdvanced += HandleDateAdvanced;
            _isEventHooked = true;
        }
    }

    // 인게임 날짜가 하루 지날 때마다 자동으로 실행되는 함수
    private void HandleDateAdvanced(DateTime newDate, int dayIndex)
    {
        if (_lastMonth != newDate.Month)
        {
            _lastMonth = newDate.Month;
            CurrentApplyCount = 0; // 달이 바뀌면 신청 횟수 0으로 자동 리셋
            Debug.Log($"[FriendlyMatch] {newDate.Month}월이 되어 친선경기 신청 횟수가 리셋되었습니다.");
        }
    }

    public void StartFriendlyMatch(string schoolId, string schoolName)
    {
        TurnManager tm = FindFirstObjectByType<TurnManager>();
        if (tm == null || tm.DateManager == null)
        {
            Debug.LogError("DateManager를 찾을 수 없습니다!");
            return;
        }

        HookDateEvent(); // 혹시 Start에서 연결 안 됐을 경우를 대비한 안전장치
        DateTime currentDate = tm.DateManager.CurrentDate;

        // 1. 횟수 제한 체크
        if (CurrentApplyCount >= MaxMonthlyCount)
        {
            Debug.Log("이번 달 친선전 신청 횟수를 모두 소진했습니다.");
            return;
        }

        // 2. 횟수 차감 
        CurrentApplyCount++;

        // 3. 현재 인게임 날짜 기준으로 가장 가까운 토요일 3개
        List<DateTime> saturdays = new List<DateTime>();
        DateTime tempDate = currentDate.AddDays(1); // 내일부터 탐색 시작

        while (saturdays.Count < 3)
        {
            if (tempDate.DayOfWeek == DayOfWeek.Saturday)
            {
                saturdays.Add(tempDate);
            }
            tempDate = tempDate.AddDays(1);
        }

        // 4. 치환할 텍스트(보간법) 데이터 준비
        Dictionary<string, string> textVars = new Dictionary<string, string>
        {
            { "{school_choice}", schoolName },
            { "{date1}", saturdays[0].ToString("M월 d일") },
            { "{date2}", saturdays[1].ToString("M월 d일") },
            { "{date3}", saturdays[2].ToString("M월 d일") },
            { "{date_choice}", "" } // 유저가 누른 선택지 날짜가 들어갈 공간
        };

        // 5. 친선전 전용 대화 러너 실행
        string roomId = $"friendly_{schoolId}";
        FriendlyMatchRunner.Instance.PlayDialogue(roomId, schoolName, "diag_schedule_001", "index_001", textVars);
    }
}