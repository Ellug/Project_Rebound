using System;
using UnityEngine;

// 지금 재화 추가, 사용 있음
// 재화 초기값 변경
public class MoneyManager : Singleton<MoneyManager>
{
    private const int DEFAULT_GOLD = 2000;
    [SerializeField] private int _gold = DEFAULT_GOLD;
    [SerializeField] private int _reputation;

    public int Gold => _gold;
    public int Reputation => _reputation;

    public event Action<int> OnGoldChanged;
    public event Action<int> OnReputationChanged;

    // 자금 추가
    public void AddGold(int amount)
    {
        if (amount <= 0)
        {
            return;
        }

        _gold += amount;
        OnGoldChanged?.Invoke(_gold);
    }

    // 자금 사용
    public bool TrySpendGold(int amount)
    {
        if (amount <= 0)
        {
            return false;
        }

        if (_gold < amount)
        {
            return false;
        }

        _gold -= amount;
        OnGoldChanged?.Invoke(_gold);
        return true;
    }

    // 명성치 보너스 적용 후 최종 지급량 계산
    public int GetAdjustedReputationAmount(int amount)
    {
        if (amount <= 0)
        {
            return 0;
        }

        if (HeadCoachManager.Instance != null && HeadCoachManager.Instance.IsInitialized)
        {
            float bonusRate = HeadCoachManager.Instance.GetStatBonusValue("Fame_Gain_Rate");
            if (bonusRate != 0f)
            {
                amount = Mathf.RoundToInt(amount * (1f + bonusRate * 0.01f));
            }
        }

        return amount;
    }

    // 명성치 추가
    public void AddReputation(int amount)
    {
        if (amount <= 0)
        {
            return;
        }

        int originalAmount = amount;
        amount = GetAdjustedReputationAmount(amount);

        if (originalAmount != amount)
        {
            float appliedBonusRate = HeadCoachManager.Instance != null && HeadCoachManager.Instance.IsInitialized
                ? HeadCoachManager.Instance.GetStatBonusValue("Fame_Gain_Rate")
                : 0f;

            Debug.Log($"[MoneyManager] 명성치 보너스 적용: 기본 {originalAmount}, 보너스 {appliedBonusRate}%, 최종 {amount}");
        }
        Debug.Log($"[MoneyManager] 명성치 증가: 현재 {_reputation + amount}");

        _reputation += amount;
        OnReputationChanged?.Invoke(_reputation);
    }

    // 명성치 사용
    public bool TrySpendReputation(int amount)
    {
        if (amount <= 0)
        {
            return false;
        }

        if (_reputation < amount)
        {
            return false;
        }

        _reputation -= amount;
        OnReputationChanged?.Invoke(_reputation);
        return true;
    }

    // 자금 초기화
    public void ResetGold()
    {
        _gold = DEFAULT_GOLD;
        OnGoldChanged?.Invoke(_gold);
    }

    public void ApplySaveData(int gold, int reputation)
    {
        _gold = gold;
        _reputation = reputation;

        OnGoldChanged?.Invoke(_gold);
        OnReputationChanged?.Invoke(_reputation);
    }

    // UI 갱신용
    public void ForceNotify()
    {
        OnGoldChanged?.Invoke(_gold);
        OnReputationChanged?.Invoke(_reputation);
    }

    // 골드/명성치를 한 번에 지급
    public void ApplyReward(int money, int fame)
    {
        if (money > 0)
        {
            AddGold(money);
        }

        if (fame > 0)
        {
            AddReputation(fame);
        }
    }
}