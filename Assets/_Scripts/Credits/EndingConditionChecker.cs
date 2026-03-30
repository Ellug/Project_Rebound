using System;
using UnityEngine;

// 4년차 겨울 토너먼트 종료 여부 판정 유틸리티
public static class EndingConditionChecker
{
    private const int FinalYear = 4; // 1년차 시작 기준 4년차

    // GameManager에서 현재 연차와 날짜를 받아 판정
    public static bool IsEndingReached(GameManager gameManager)
    {
        if (gameManager == null)
        {
            Debug.LogWarning("[EndingConditionChecker] GameManager가 null입니다.");
            return false;
        }

        return IsEndingReached(gameManager.CurrentYear, gameManager.CurrentDate);
    }

    // 연차와 날짜를 직접 받아 판정 (유닛 테스트용)
    public static bool IsEndingReached(int currentYear, DateTime currentDate)
    {
        if (currentYear < FinalYear)
            return false;

        if (!AlwaysEventDateUtil.TryGetFirstWinterVacationTerm(out DateTime winterStart, out DateTime winterEnd))
        {
            Debug.LogWarning("[EndingConditionChecker] 겨울 방학 일정을 찾을 수 없어 연차만으로 판정합니다.");
            return true;
        }

        DateTime today = currentDate.Date;
        return today >= winterStart.Date && today <= winterEnd.Date;
    }
}