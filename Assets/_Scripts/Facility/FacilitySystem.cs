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

    public void SetLevel(string facility, int level)
    {
        _levels[facility] = Mathf.Max(1, level); // 세이브 로드 복원용
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

        if (facility == "school")
        {
            int schoolLv = GetLevel("school");
            int requiredLv = schoolLv * 2;

            int gymLv = GetLevel("gym");
            int cafeteriaLv = GetLevel("cafeteria");
            int counselingLv = GetLevel("counselingcenter");

            if (gymLv < requiredLv ||
                cafeteriaLv < requiredLv ||
                counselingLv < requiredLv)
            {
                Debug.Log("학교 업그레이드 조건 미충족");
                return false;
            }
        }
        var current = GetCurrentData(facility);
        if (!MoneyManager.Instance.TrySpendGold(current.upgradeCost))
        {
            Debug.Log("골드 부족");
            return false;
        }

        _levels[facility] = next.facilityLv;

        Debug.Log($"{facility} 업그레이드 Lv {next.facilityLv}");

        if (SaveManager.Instance != null)
        {
            Debug.Log($"[FacilitySystem] 업그레이드 저장 | facility={facility} | level={next.facilityLv}");
            SaveManager.Instance.SaveCurrent();
        }

        return true;
    }

    // 학교 업그레이드 조건 체크
    public bool CanUpgradeSchool()
    {
        int schoolLv = GetLevel("school");
        int requiredLv = schoolLv * 2;

        int gymLv = GetLevel("gym");
        int cafeteriaLv = GetLevel("cafeteria");
        int counselingLv = GetLevel("counselingcenter");

        return gymLv >= requiredLv &&
               cafeteriaLv >= requiredLv &&
               counselingLv >= requiredLv;
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