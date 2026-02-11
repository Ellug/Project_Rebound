using System;
using UnityEngine;

public class MoneyManager : Singleton<MoneyManager>
{
    private const int DEFAULT_GOLD = 2000;
    [SerializeField] private int _gold = DEFAULT_GOLD;
    [SerializeField] private int _reputation;

    public int Gold => _gold;
    public int Reputation => _reputation;

    public event Action<int> OnGoldChanged;
    public event Action<int> OnReputationChanged;

    public void AddGold(int amount)
    {
        if (amount <= 0)
        {
            return;
        }

        _gold += amount;
        OnGoldChanged?.Invoke(_gold);
    }

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

    public void AddReputation(int amount)
    {
        if (amount <= 0)
        {
            return;
        }

        _reputation += amount;
        OnReputationChanged?.Invoke(_reputation);
    }

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
}
