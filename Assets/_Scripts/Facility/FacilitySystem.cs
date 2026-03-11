using System.Collections.Generic;
using UnityEngine;

public class FacilitySystem : Singleton<FacilitySystem>
{
    // 시설 레벨 저장
    private Dictionary<string, int> _levels = new();

    protected override void OnSingletonAwake()
    {
        // 초기 레벨
        _levels["school"] = 1;
        _levels["gym"] = 1;
        _levels["cafeteria"] = 1;
        _levels["counselingcenter"] = 1;
    }

    // 레벨 반환
    public int GetLevel(string facility)
    {
        return _levels.TryGetValue(facility, out int lv) ? lv : 1;
    }

    // 현재 데이터
    public FacilityUpgradeRow GetCurrentData(string facility)
    {
        int lv = GetLevel(facility);
        return CachedSOData.Get<FacilityUpgradeTableSO>().Get(facility, lv);
    }

    // 다음 레벨 데이터
    public FacilityUpgradeRow GetNextData(string facility)
    {
        int lv = GetLevel(facility);
        return CachedSOData.Get<FacilityUpgradeTableSO>().Get(facility, lv + 1);
    }

    // 업그레이드 시도
    public bool TryUpgrade(string facility)
    {
        var next = GetNextData(facility);

        if (next == null)
        {
            Debug.Log($"[{facility}] 이미 최대 레벨");
            return false;
        }

        if (!MoneyManager.Instance.TrySpendGold(next.upgradeCost))
        {
            Debug.Log("골드 부족");
            return false;
        }

        _levels[facility] = next.facilityLv;

        Debug.Log($"{facility} 업그레이드 Lv {next.facilityLv}");

        return true;
    }

    // 시설 요구조건 체크
    public bool CheckRequirement(string facility, int requiredLv)
    {
        return GetLevel(facility) >= requiredLv;
    }

    // 학교
    public int GetConditionDecayBonus()
    {
        return GetCurrentData("school").conditionDecayEfficiency;
    }

    // 체육관
    public int GetTrainingExpBonus()
    {
        return GetCurrentData("gym").trainingExpEfficiency;
    }

    // 상담
    public int GetMentalBonus()
    {
        return GetCurrentData("counselingcenter").trainingExpEfficiency;
    }

    // 식당
    public int GetCafeteriaBonus()
    {
        return GetCurrentData("cafeteria").trainingExpEfficiency;
    }
}
