using System.Collections.Generic;
using UnityEngine;

public class FacilitySystem : Singleton<FacilitySystem>
{
    // 시설 레벨 저장
    private Dictionary<string, int> _levels = new();

    protected override void OnSingletonAwake()
    {
        ResetLevelsToDefault();
    }

    // 시설 레벨을 기본값으로 초기화
    public void ResetLevelsToDefault()
    {
        _levels.Clear();
        _levels["school"] = 1;
        _levels["gym"] = 1;
        _levels["cafeteria"] = 1;
        _levels["counselingcenter"] = 1;

        Debug.Log("[FacilitySystem] 시설 레벨 기본값으로 초기화");
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
        int cost = GetFinalUpgradeCost(facility);
        if (!MoneyManager.Instance.TrySpendGold(cost))
        {
            Debug.Log("골드 부족");
            return false;
        }

        _levels[facility] = next.facilityLv;

        Debug.Log($"{facility} 업그레이드 Lv {next.facilityLv}");

        // 업그레이드 직후 누적 보너스 확인 로그
        LogAccumulatedBonus(facility);

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
        return GetAccumulatedBonus("school", r => r.conditionDecayEfficiency);
    }

    // 체육관
    public int GetTrainingExpBonus()
    {
        return GetAccumulatedBonus("gym", r => r.trainingExpEfficiency);
    }

    // 상담
    public int GetMentalBonus()
    {
        return GetAccumulatedBonus("counselingcenter", r => r.trainingExpEfficiency);
    }

    // 식당
    public int GetCafeteriaBonus()
    {
        return GetAccumulatedBonus("cafeteria", r => r.trainingExpEfficiency);
    }

    public int GetFinalUpgradeCost(string facility)
    {
        var current = GetCurrentData(facility);
        if (current == null) return 0;

        int baseCost = current.upgradeCost;

        float discountPercent = 0f;
        if (HeadCoachManager.Instance != null)
        {
            discountPercent = HeadCoachManager.Instance.GetStatBonusValue("Facility_Upgrade_Cost");
        }

        // 테이블에 -5로 들어오면 5% 할인으로 처리
        float multiplier = 1f + (discountPercent / 100f);
        multiplier = Mathf.Max(0.01f, multiplier);

        int finalCost = Mathf.FloorToInt(baseCost * multiplier);

        Debug.Log($"[FacilitySystem] {facility} 업그레이드 비용 계산 | 기본:{baseCost} | 할인:{discountPercent}% | 배율:{multiplier} | 최종:{finalCost}");

        return Mathf.Max(1, finalCost);
    }

    // 공통 누적 합산 헬퍼
    // Lv1부터 현재 레벨까지 각 행의 값을 더해 총 보너스를 반환
    private int GetAccumulatedBonus(string facility, System.Func<FacilityUpgradeRow, int> selector)
    {
        int currentLv = GetLevel(facility);
        var table = CachedSOData.Get<FacilityUpgradeTableSO>();
        int total = 0;

        for (int lv = 1; lv <= currentLv; lv++)
        {
            var row = table.Get(facility, lv);
            if (row != null)
            {
                int value = selector(row);
                total += value;
            }
        }
        return total;
    }

    // 업그레이드 후 누적 보너스 확인용 로그 출력
    private void LogAccumulatedBonus(string facility)
    {
        switch (facility)
        {
            case "school":
                Debug.Log($"[FacilitySystem] school 컨디션 감소 누적 보너스 최종 | Lv{GetLevel("school")} | 합계:{GetConditionDecayBonus()}");
                break;
            case "gym":
                Debug.Log($"[FacilitySystem] gym 훈련 경험치 누적 보너스 최종 | Lv{GetLevel("gym")} | 합계:{GetTrainingExpBonus()}%");
                break;
            case "cafeteria":
                Debug.Log($"[FacilitySystem] cafeteria 식당 누적 보너스 최종 | Lv{GetLevel("cafeteria")} | 합계:{GetCafeteriaBonus()}%");
                break;
            case "counselingcenter":
                Debug.Log($"[FacilitySystem] counselingcenter 멘탈 누적 보너스 최종 | Lv{GetLevel("counselingcenter")} | 합계:{GetMentalBonus()}%");
                break;
        }
    }
}