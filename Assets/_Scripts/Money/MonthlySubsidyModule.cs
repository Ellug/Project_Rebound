// MonthlySubsidyModule.cs
using System;
using UnityEngine;

public class MonthlySubsidyModule : ITurnModule
{
    private const int SubsidyAmount = 100;

    public string ModuleName => "MonthlySubsidy";
    public int Priority => 50; // 우선순위는 프로젝트 기준에 맞게 조정

    private int _lastPaidMonth = -1;

    public float GetPermanentBonusRate() => _permanentBonusRate; // 영구 보너스 배율 반환

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

        // 보너스 반영된 실제 지급액 계산
        int actualAmount = GetCurrentSubsidyAmount();
        MoneyManager.Instance.AddGold(actualAmount);

        Debug.Log($"[MonthlySubsidyModule] {tomorrow:yyyy-MM} 월 지원금 {actualAmount} 지급 완료");

        UIManager.Instance?.ShowPopup(UIPopupRequest.Simple(
            title: "지원금 도착",
            message: $"학교 지원금 {actualAmount}골드가 지급되었습니다! 팀 운영에 유용하게 사용하세요.",
            onPrimary: null,
            onCancel: null,
            showCancel: false
        ));
    }

    public void Reset()
    {
        _lastPaidMonth = -1;
    }

    // 영구 보너스 배율 누적 필드
    // 세이브/로드 대응은 추후 구현
    private float _permanentBonusRate = 0f;

    // 영구 지원금 보너스 배율 누적
    public void AddPermanentBonusRate(float rate)
    {
        _permanentBonusRate += rate;
        Debug.Log($"[MonthlySubsidyModule] 영구 지원금 보너스 누적: {_permanentBonusRate * 100f}%");
    }

    // 현재 지원금 금액 반환 (영구 보너스 반영)
    public int GetCurrentSubsidyAmount()
    {
        return Mathf.RoundToInt(SubsidyAmount * (1f + _permanentBonusRate));
    }

    // 영구 보너스 배율 초기화 (예: 페널티 적용 시)
    public void SetPermanentBonusRate(float rate)
    {
        _permanentBonusRate = Mathf.Max(0f, rate);
    }
}