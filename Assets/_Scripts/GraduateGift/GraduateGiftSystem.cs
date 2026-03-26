using System;
using System.Collections.Generic;
using UnityEngine;

// 졸업 선물 보상 추첨 및 팝업 표시
// 구현 범위: 등급 판정 → 보상 추첨 → 결과 저장 → 랜덤 날짜에 효과 적용 및 팝업 표시
// 장비 업그레이드(itemeffect_03)는 기획 확정 후 별도 구현
public static class GraduateGiftSystem
{
    private static readonly System.Random _random = new();

    // 졸업생 목록을 받아 등급 판정 → 보상 추첨 → PendingGraduateGift 저장
    // 실제 효과 적용과 팝업은 랜덤 날짜에 ProcessPendingGifts()에서 처리
    public static void ProcessGraduates(
        List<Student> graduates,
        int semiFinalCount,
        System.Action<List<(Student student, string rewardType)>> onAllDone = null)
    {
        if (graduates == null || graduates.Count == 0)
        {
            onAllDone?.Invoke(new List<(Student, string)>());
            return;
        }

        if (SaveManager.Instance?.CurrentData == null)
        {
            onAllDone?.Invoke(new List<(Student, string)>());
            return;
        }

        var pending = SaveManager.Instance.CurrentData.pendingGraduateGifts;
        if (pending == null)
        {
            SaveManager.Instance.CurrentData.pendingGraduateGifts = new List<PendingGraduateGift>();
            pending = SaveManager.Instance.CurrentData.pendingGraduateGifts;
        }

        var rewardResults = new List<(Student student, string rewardType)>();

        DateTime graduationDate = GameManager.Instance != null
            ? GameManager.Instance.CurrentDate
            : DateTime.Now;

        foreach (var student in graduates)
        {
            if (student == null)
                continue;

            var evalResult = GraduateGradeEvaluator.Evaluate(student, semiFinalCount);
            string rewardId = DrawReward(evalResult.GradeIndex);

            rewardResults.Add((student, rewardId ?? string.Empty));

            if (string.IsNullOrEmpty(rewardId))
                continue;

            int daysOffset = _random.Next(1, 366);
            DateTime triggerDate = graduationDate.AddDays(daysOffset);

            pending.Add(new PendingGraduateGift
            {
                studentId = student.id.ToString(),
                studentName = student.studentName,
                gradeLabel = evalResult.GradeLabel,
                rewardId = rewardId,
                rewardName = FindRewardName(rewardId),
                triggerDate = triggerDate.ToString("yyyy-MM-dd")
            });

            // 고등급(1·2등급) 추가 보상 예약
            // 1등급 → 3등급 확률표 기준 추첨, 2등급 → 4등급 확률표 기준 추첨
            // 졸업일로부터 100일 이내 랜덤 날짜에 1회 추가 지급
            int bonusGradeIndex = evalResult.GradeIndex switch
            {
                1 => 3,
                2 => 4,
                _ => -1
            };

            if (bonusGradeIndex < 0)
                continue;

            string bonusRewardId = DrawReward(bonusGradeIndex);
            if (string.IsNullOrEmpty(bonusRewardId))
                continue;

            int bonusDaysOffset = _random.Next(1, 101); // 1~100일
            DateTime bonusTriggerDate = graduationDate.AddDays(bonusDaysOffset);

            pending.Add(new PendingGraduateGift
            {
                studentId = student.id.ToString(),
                studentName = student.studentName,
                gradeLabel = evalResult.GradeLabel,
                rewardId = bonusRewardId,
                rewardName = FindRewardName(bonusRewardId),
                triggerDate = bonusTriggerDate.ToString("yyyy-MM-dd")
            });
        }

        SaveManager.Instance.SaveCurrent();
        onAllDone?.Invoke(rewardResults);
    }

    // 매 턴 시작 시 호출 — 오늘 날짜에 해당하는 대기 선물을 효과 적용 후 팝업 표시
    // TurnManager의 OnTurnBegin 또는 GameManager.HandleTurnCompleted에서 호출
    public static void ProcessPendingGifts(DateTime currentDate)
    {
        if (SaveManager.Instance?.CurrentData == null) return;

        var pending = SaveManager.Instance.CurrentData.pendingGraduateGifts;
        if (pending == null || pending.Count == 0) return;

        // 오늘 날짜에 해당하는 항목 수집
        var toProcess = new List<PendingGraduateGift>();
        foreach (var gift in pending)
        {
            if (!DateTime.TryParse(gift.triggerDate, out DateTime triggerDate)) continue;
            if (triggerDate.Date <= currentDate.Date)
                toProcess.Add(gift);
        }

        if (toProcess.Count == 0) return;

        // 처리된 항목 제거
        foreach (var gift in toProcess)
            pending.Remove(gift);

        // 순차 팝업 표시
        ShowNextPendingGiftPopup(toProcess, 0);
    }

    // 대기 선물 팝업을 순차적으로 표시 (재귀 콜백 방식)
    private static void ShowNextPendingGiftPopup(List<PendingGraduateGift> gifts, int index)
    {
        if (index >= gifts.Count)
        {
            SaveManager.Instance?.SaveCurrent();
            return;
        }

        var gift = gifts[index];
        Action onNext = () => ShowNextPendingGiftPopup(gifts, index + 1);

        ExecuteReward(gift.rewardId);

        GraduateGiftPopupRow popupRow = FindPopupRow(gift.rewardId);
        if (popupRow == null || UIManager.Instance == null)
        {
            onNext?.Invoke();
            return;
        }

        UIManager.Instance.ShowPopup(UIPopupRequest.Default(
            title: popupRow.rewardHeader,
            message: popupRow.rewardBody,
            onPrimary: onNext,
            onCancel: null,
            showCancel: false,
            primaryKind: UIPopupRequest.PrimaryButtonKind.Confirm
        ));
    }

    // 등급 기반 보상 추첨
    // 각 행의 확률을 독립적으로 체크 후 당첨된 항목 중 1개 랜덤 선택
    // 당첨 항목 없으면 null 반환
    private static string DrawReward(int gradeIndex)
    {
        var table = CachedSOData.Get<GraduateGiftTierRewardTableSO>();
        if (table == null || table.Rows == null || table.Rows.Count == 0)
        {
            Debug.LogError("[GraduateGiftSystem] GraduateGiftTierRewardTableSO를 찾을 수 없습니다.");
            return string.Empty;
        }

        List<GraduateGiftTierRewardRow> candidates = new List<GraduateGiftTierRewardRow>();
        int totalWeight = 0;

        foreach (var row in table.Rows)
        {
            if (row == null)
                continue;

            int weight = 0;
            switch (gradeIndex)
            {
                case 1: weight = row.grade1; break;
                case 2: weight = row.grade2; break;
                case 3: weight = row.grade3; break;
                case 4: weight = row.grade4; break;
            }

            if (weight <= 0)
                continue;

            candidates.Add(row);
            totalWeight += weight;
        }

        if (candidates.Count == 0 || totalWeight <= 0)
        {
            Debug.LogWarning($"[GraduateGiftSystem] 등급 {gradeIndex}에 해당하는 보상 후보가 없습니다.");
            return string.Empty;
        }

        int roll = UnityEngine.Random.Range(0, totalWeight);
        int accumulated = 0;

        foreach (var row in candidates)
        {
            int weight = 0;
            switch (gradeIndex)
            {
                case 1: weight = row.grade1; break;
                case 2: weight = row.grade2; break;
                case 3: weight = row.grade3; break;
                case 4: weight = row.grade4; break;
            }

            accumulated += weight;
            if (roll < accumulated)
            {
                // 중요: 실행/저장용은 rewardType이 아니라 id
                return row.id;
            }
        }

        Debug.LogWarning("[GraduateGiftSystem] DrawReward 결과가 비정상입니다.");
        return string.Empty;
    }

    // rewardType별 보상 효과 적용
    private static void ExecuteReward(string rewardId)
    {
        switch (rewardId)
        {
            case "itemeffect_01":
                ExecuteSubsidyPermBonus();
                break;

            case "itemeffect_02":
                ExecuteFacilityUpgrade();
                break;

            case "itemeffect_03":
                ExecuteEquipmentUpgrade();
                break;

            case "itemeffect_04":
                ExecuteInstantSubsidyBonus();
                break;

            case "itemeffect_05":
                ExecuteConditionRecoveryAll();
                break;

            case "itemeffect_06":
                ExecuteTemporaryTrainingBoost();
                break;

            case "itemeffect_07":
                ExecuteTrainingEfficiencyPermBonus();
                break;

            default:
                Debug.LogWarning($"[GraduateGiftSystem] 알 수 없는 rewardId: {rewardId}");
                break;
        }
    }

    // itemeffect_01: 지원금 영구 10% 증가
    private static void ExecuteSubsidyPermBonus()
    {
        if (GameManager.Instance == null)
        {
            Debug.LogWarning("[GraduateGiftSystem] GameManager가 없어 지원금 보너스를 적용할 수 없습니다.");
            return;
        }
        GameManager.Instance.AddSubsidyPermBonus(0.1f);
        Debug.Log("[GraduateGiftSystem] 지원금 영구 10% 증가 적용");
    }

    // itemeffect_02: 업그레이드 가능한 시설 중 랜덤 1개를 비용 없이 업그레이드
    private static void ExecuteFacilityUpgrade()
    {
        var facilitySystem = FacilitySystem.Instance;
        if (facilitySystem == null)
        {
            Debug.LogWarning("[GraduateGiftSystem] FacilitySystem이 없어 시설 업그레이드를 적용할 수 없습니다.");
            return;
        }

        string[] facilities = { "school", "gym", "cafeteria", "counselingcenter" };
        var upgradable = new List<string>();

        foreach (string f in facilities)
        {
            if (facilitySystem.GetNextData(f) != null)
                upgradable.Add(f);
        }

        if (upgradable.Count == 0)
        {
            Debug.Log("[GraduateGiftSystem] 업그레이드 가능한 시설이 없습니다.");
            return;
        }

        string target = upgradable[UnityEngine.Random.Range(0, upgradable.Count)];
        facilitySystem.ForceUpgrade(target);
        Debug.Log($"[GraduateGiftSystem] 시설 무료 업그레이드: {target}");
    }

    // itemeffect_03: 랜덤 장비 1개를 1단계 업그레이드
    private static void ExecuteEquipmentUpgrade()
    {
        if (EquipmentSystem.Instance == null)
        {
            Debug.LogWarning("[GraduateGiftSystem] EquipmentSystem이 없어 장비 업그레이드를 적용할 수 없습니다.");
            return;
        }

        EquipmentSystem.Instance.UpgradeRandom();
        Debug.Log("[GraduateGiftSystem] 랜덤 장비 1단계 업그레이드 적용");
    }

    // itemeffect_04: 현재 지원금의 20%를 즉시 골드로 지급
    private static void ExecuteInstantSubsidyBonus()
    {
        if (GameManager.Instance == null)
        {
            Debug.LogWarning("[GraduateGiftSystem] GameManager가 없어 즉시 지원금을 적용할 수 없습니다.");
            return;
        }

        int subsidyAmount = GameManager.Instance.GetCurrentSubsidyAmount();
        int bonus = Mathf.FloorToInt(subsidyAmount * 0.2f);

        if (MoneyManager.Instance == null) return;

        MoneyManager.Instance.AddGold(bonus);
        Debug.Log($"[GraduateGiftSystem] 즉시 후원금 지급: {bonus}골드");
    }

    // itemeffect_05: 전체 학생 컨디션 40 회복
    private static void ExecuteConditionRecoveryAll()
    {
        if (StudentManager.Instance == null)
        {
            Debug.LogWarning("[GraduateGiftSystem] StudentManager가 없어 컨디션 회복을 적용할 수 없습니다.");
            return;
        }

        foreach (Student student in StudentManager.Instance.Students)
        {
            if (student == null) continue;
            student.condition = Student.ClampCondition(student.condition + 40);
            StudentManager.Instance.NotifyStudentModified(student);
        }

        Debug.Log("[GraduateGiftSystem] 전체 학생 컨디션 40 회복 적용");
    }

    // itemeffect_06: 30일 동안 랜덤 스탯 경험치 1.3배 증가
    private static void ExecuteTemporaryTrainingBoost()
    {
        if (GameManager.Instance == null)
        {
            Debug.LogWarning("[GraduateGiftSystem] GameManager가 없어 일시적 훈련 효과를 적용할 수 없습니다.");
            return;
        }

        StudentCoreStat[] statPool =
        {
            StudentCoreStat.Mental,
            StudentCoreStat.Shoot,
            StudentCoreStat.Speed,
            StudentCoreStat.Jump,
            StudentCoreStat.Stamina
        };
        StudentCoreStat targetStat = statPool[UnityEngine.Random.Range(0, statPool.Length)];
        DateTime expireDate = GameManager.Instance.CurrentDate.AddDays(30);

        GameManager.Instance.SetTemporaryTrainingBoost(targetStat, expireDate);
        Debug.Log($"[GraduateGiftSystem] 일시적 훈련 효과 적용: {targetStat} 경험치 1.3배 | 만료일: {expireDate:yyyy-MM-dd}");
    }

    // itemeffect_07: 영구적으로 전체 훈련 스탯 효율 3% 상승
    private static void ExecuteTrainingEfficiencyPermBonus()
    {
        if (GameManager.Instance == null)
        {
            Debug.LogWarning("[GraduateGiftSystem] GameManager가 없어 훈련 효율 영구 보너스를 적용할 수 없습니다.");
            return;
        }

        GameManager.Instance.AddTrainingEfficiencyPermBonus(0.03f);
        Debug.Log("[GraduateGiftSystem] 전역 훈련 스탯 효율 영구 보너스 3% 적용");
    }

    // 팝업 테이블에서 rewardType으로 행 조회
    private static GraduateGiftPopupRow FindPopupRow(string rewardId)
    {
        var table = CachedSOData.Get<GraduateGiftPopupTableSO>();
        if (table == null || table.Rows == null || table.Rows.Count == 0)
        {
            Debug.LogWarning("[GraduateGiftSystem] GraduateGiftPopupTableSO를 찾을 수 없습니다.");
            return null;
        }

        foreach (var row in table.Rows)
        {
            if (row == null)
                continue;

            if (row.id == rewardId)
                return row;
        }

        Debug.LogWarning($"[GraduateGiftSystem] rewardId에 해당하는 팝업 데이터를 찾지 못했습니다: {rewardId}");
        return null;
    }

    // 보상 ID로 보상 이름 조회 (팝업 표시용)
    private static string FindRewardName(string rewardId)
    {
        var table = CachedSOData.Get<GraduateGiftTierRewardTableSO>();
        if (table == null || table.Rows == null || table.Rows.Count == 0)
            return rewardId;

        foreach (var row in table.Rows)
        {
            if (row == null)
                continue;

            if (row.id == rewardId)
                return string.IsNullOrEmpty(row.rewardType) ? rewardId : row.rewardType;
        }

        return rewardId;
    }
}