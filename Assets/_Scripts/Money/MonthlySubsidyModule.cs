// MonthlySubsidyModule.cs
using System;
using UnityEngine;

public class MonthlySubsidyModule : ITurnModule
{
    private const int SubsidyAmount = 100;

    public string ModuleName => "MonthlySubsidy";
    public int Priority => 50; // 우선순위는 프로젝트 기준에 맞게 조정

    private int _lastPaidMonth = -1;

    public void OnTurnBegin(TurnContext context) { }

    public void OnTurnExecute(TurnContext context) { }

    public void OnTurnEnd(TurnContext context)
    {
        // OnTurnEnd 시점은 AdvanceDay() 이전이므로 내일 날짜 기준으로 체크
        DateTime tomorrow = context.CurrentDate.AddDays(1);

        if (tomorrow.Day != 1)
            return;

        if (_lastPaidMonth == tomorrow.Month)
            return;

        if (MoneyManager.Instance == null)
            return;

        _lastPaidMonth = tomorrow.Month;
        MoneyManager.Instance.AddGold(SubsidyAmount);

        Debug.Log($"[MonthlySubsidyModule] {tomorrow:yyyy-MM} 월 지원금 {SubsidyAmount} 지급 완료");

        UIManager.Instance?.ShowPopup(UIPopupRequest.Simple(
            title: "지원금 도착",
            message: "학교 지원금 100골드가 지급되었습니다! 팀 운영에 유용하게 사용하세요.",
            onPrimary: null,
            onCancel: null,
            showCancel: false
        ));
    }

    public void Reset()
    {
        _lastPaidMonth = -1;
    }
}