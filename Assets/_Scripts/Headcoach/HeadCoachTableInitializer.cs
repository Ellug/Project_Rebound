using System.Collections.Generic;
using UnityEngine;

// SO 데이터 테이블을 읽어 HeadCoachManager를 초기화하는 정적 클래스
// StartManager의 Initializing 시점에 Init() 호출
public static class HeadCoachTableInitializer
{
    public static void Init()
    {
        // 테이블 업데이트 후 노드 미출력 방지: 항상 컨테이너를 초기화하고 재구성
        HeadCoachManager.Instance.ResetContainer();

        if (!TryGetRequiredTables(
                out CoachNodeMasterTableSO masterTable,
                out CoachNodeEffectDetailTableSO effectTable,
                out CoachNodePrerequisiteTableSO prerequisiteTable,
                out TierManageOpenConditionTableSO tierTable))
        {
            Debug.LogError("[HeadCoachTableInitializer] 필수 테이블이 CachedSOData에 등록되지 않았습니다.");
            return;
        }

        foreach (CoachNodePrerequisiteRow row in prerequisiteTable.Rows)

        HeadCoachManager.Instance.InitFromTable(
            ConvertMasterRows(masterTable),
            ConvertEffectRows(effectTable),
            ConvertPrerequisiteRows(prerequisiteTable),
            ConvertTierConfigRows(tierTable));

        RestoreUnlockedNodes();
        Debug.Log("[HeadCoachTableInitializer] 초기화 완료");
    }

    private static bool TryGetRequiredTables(
        out CoachNodeMasterTableSO masterTable,
        out CoachNodeEffectDetailTableSO effectTable,
        out CoachNodePrerequisiteTableSO prerequisiteTable,
        out TierManageOpenConditionTableSO tierTable)
    {
        bool isAllLoaded = true;
        isAllLoaded &= CachedSOData.TryGet(out masterTable);
        isAllLoaded &= CachedSOData.TryGet(out effectTable);
        isAllLoaded &= CachedSOData.TryGet(out prerequisiteTable);
        isAllLoaded &= CachedSOData.TryGet(out tierTable);
        return isAllLoaded;
    }

    // TODO: 노드 102의 effect_id가 테이블에 1003으로 기재되어 있으나 실제로는 1002 → 테이블 수정 필요
    private static IEnumerable<HeadCoachNodeData> ConvertMasterRows(CoachNodeMasterTableSO table)
    {
        List<HeadCoachNodeData> result = new(table.Rows.Count);
        foreach (CoachNodeMasterRow row in table.Rows)
        {
            result.Add(new HeadCoachNodeData
            {
                nodeId = row.id,
                tierId = row.tierId,
                name = row.name,
                unlockCost = row.unlockCost,
                description = row.description,
                effectId = row.effectId,
            });
        }
        return result;
    }

    private static IEnumerable<HeadCoachEffectData> ConvertEffectRows(CoachNodeEffectDetailTableSO table)
    {
        List<HeadCoachEffectData> result = new(table.Rows.Count);
        foreach (CoachNodeEffectDetailRow row in table.Rows)
        {
            result.Add(new HeadCoachEffectData
            {
                effectId = row.id,
                targetStat = row.targetStat,
                applyMethod = ParseApplyMethod(row.applyMethod),
                effectValue = row.effectValue,
            });
        }
        return result;
    }

    // targetPrerequisiteId가 0인 행은 선행 조건 없음으로 간주해 스킵
    private static IEnumerable<HeadCoachPrerequisiteData> ConvertPrerequisiteRows(CoachNodePrerequisiteTableSO table)
    {
        List<HeadCoachPrerequisiteData> result = new(table.Rows.Count);
        foreach (CoachNodePrerequisiteRow row in table.Rows)
        {
            if (row.targetPrerequisiteId == 0) continue;

            result.Add(new HeadCoachPrerequisiteData
            {
                id = row.col1,
                nodeId = row.nodeId,
                targetPrerequisiteId = row.targetPrerequisiteId,
            });
        }
        return result;
    }

    private static IEnumerable<HeadCoachTierConfigData> ConvertTierConfigRows(TierManageOpenConditionTableSO table)
    {
        List<HeadCoachTierConfigData> result = new(table.Rows.Count);
        foreach (TierManageOpenConditionRow row in table.Rows)
        {
            result.Add(new HeadCoachTierConfigData
            {
                tierId = row.id,
                tierLevel = row.tierLevel,
                tierName = row.tierName,
                unlockConditionCount = row.unlockConditionCount,
                maxNodeCount = row.maxNodeCount,
                tierBonusEffectId = row.tierBonusEffectId,
            });
        }
        return result;
    }

    private static ApplyMethod ParseApplyMethod(string raw)
    {
        if (System.Enum.TryParse(raw, ignoreCase: true, out ApplyMethod method))
            return method;

        Debug.LogWarning($"[HeadCoachTableInitializer] 알 수 없는 applyMethod: '{raw}' → Add로 대체");
        return ApplyMethod.Add;
    }

    private static void RestoreUnlockedNodes()
    {
        PlayData data = SaveManager.Instance.CurrentData;
        if (data != null)
            HeadCoachManager.Instance.RestoreUnlockedNodes(data.unlockedNodeIds);
    }
}