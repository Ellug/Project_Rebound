using System;
using UnityEngine;

// 지금 재화 추가, 사용 있음
// 재화 초기값 변경
// 세이브 로드 나중에 추가
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

    // 명성치 추가
    public void AddReputation(int amount)
    {
        if (amount <= 0)
        {
            return;
        }

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
}
