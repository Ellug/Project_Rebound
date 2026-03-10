using System.Collections.Generic;
using UnityEngine;

// SO 데이터 테이블 → HeadCoachManager.InitFromTable() 연동
// StartManager의 Initializing 시점에 호출
public static class HeadCoachTableInitializer
{
    // CachedSOData에서 테이블을 읽어 HeadCoachManager를 초기화
    // 이미 초기화된 경우 복원만 수행 (Lobby 재진입 시)
    public static void Init()
    {
        if (HeadCoachManager.Instance.IsInitialized)
        {
            RestoreUnlockedNodes();
            return;
        }

        if (!TryGetRequiredTables(
                out CoachNodeMasterTableSO masterTable,
                out CoachNodeEffectDetailTableSO effectTable,
                out CoachNodePrerequisiteTableSO prerequisiteTable,
                out TierManageOpenConditionTableSO tierTable,
                out ContentUnlockFeatureTableSO contentTable))
        {
            Debug.LogError("[HeadCoachTableInitializer] 필수 테이블이 CachedSOData에 등록되지 않았습니다.");
            return;
        }

        foreach (CoachNodePrerequisiteRow row in prerequisiteTable.Rows)
            Debug.Log($"[Prerequisite] id={row.col1} nodeId={row.nodeId} targetId={row.targetPrerequisiteId}");

        HeadCoachManager.Instance.InitFromTable(
            ConvertMasterRows(masterTable),
            ConvertEffectRows(effectTable),
            ConvertPrerequisiteRows(prerequisiteTable),
            ConvertTierConfigRows(tierTable),
            ConvertContentUnlockRows(contentTable, effectTable));

        RestoreUnlockedNodes();
        Debug.Log("[HeadCoachTableInitializer] 초기화 완료");
    }

    private static bool TryGetRequiredTables(
        out CoachNodeMasterTableSO masterTable,
        out CoachNodeEffectDetailTableSO effectTable,
        out CoachNodePrerequisiteTableSO prerequisiteTable,
        out TierManageOpenConditionTableSO tierTable,
        out ContentUnlockFeatureTableSO contentTable)
    {
        bool isAllLoaded = true;
        isAllLoaded &= CachedSOData.TryGet(out masterTable);
        isAllLoaded &= CachedSOData.TryGet(out effectTable);
        isAllLoaded &= CachedSOData.TryGet(out prerequisiteTable);
        isAllLoaded &= CachedSOData.TryGet(out tierTable);
        isAllLoaded &= CachedSOData.TryGet(out contentTable);
        return isAllLoaded;
    }

    // CoachNodeMasterRow → HeadCoachNodeData
    private static IEnumerable<HeadCoachNodeData> ConvertMasterRows(CoachNodeMasterTableSO table)
    {
        var result = new List<HeadCoachNodeData>(table.Rows.Count);

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

    // CoachNodeEffectDetailRow → HeadCoachEffectData
    private static IEnumerable<HeadCoachEffectData> ConvertEffectRows(CoachNodeEffectDetailTableSO table)
    {
        var result = new List<HeadCoachEffectData>(table.Rows.Count);
        foreach (CoachNodeEffectDetailRow row in table.Rows)
        {
            result.Add(new HeadCoachEffectData
            {
                effectId = row.id,
                targetStat = row.targetStat,
                applyMethod = ParseApplyMethod(row.applyMethod),
                effectValue = row.effectValue,
                functionId = row.functionId,
            });
        }
        return result;
    }

    // CoachNodePrerequisiteRow → HeadCoachPrerequisiteData
    // targetPrerequisiteId가 0인 행은 선행 조건 없음으로 간주하여 스킵
    private static IEnumerable<HeadCoachPrerequisiteData> ConvertPrerequisiteRows(CoachNodePrerequisiteTableSO table)
    {
        var result = new List<HeadCoachPrerequisiteData>(table.Rows.Count);

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

    // TierManageOpenConditionRow → HeadCoachTierConfigData
    private static IEnumerable<HeadCoachTierConfigData> ConvertTierConfigRows(TierManageOpenConditionTableSO table)
    {
        var result = new List<HeadCoachTierConfigData>(table.Rows.Count);

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

    // ContentUnlockFeatureRow → HeadCoachContentUnlockData
    // 효과 테이블에서 참조되지 않는 콘텐츠는 등록하지 않음
    private static IEnumerable<HeadCoachContentUnlockData> ConvertContentUnlockRows(
        ContentUnlockFeatureTableSO contentTable,
        CoachNodeEffectDetailTableSO effectTable)
    {
        // 효과 테이블에서 functionId가 있는 항목만 수집
        var referencedFunctionIds = new HashSet<int>();

        foreach (CoachNodeEffectDetailRow row in effectTable.Rows)
        {
            if (row.functionId != 0)
                referencedFunctionIds.Add(row.functionId);
        }
        var result = new List<HeadCoachContentUnlockData>();

        foreach (ContentUnlockFeatureRow row in contentTable.Rows)
        {
            if (!referencedFunctionIds.Contains(row.id)) continue;

            result.Add(new HeadCoachContentUnlockData
            {
                functionId = row.id,
                functionKey = row.functionKey,
                contentName = row.name,
                category = row.category,
                description = row.description,
            });
        }
        return result;
    }

    // applyMethod 문자열 → ApplyMethod enum 변환
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
        {
            HeadCoachManager.Instance.RestoreUnlockedNodes(data.unlockedNodeIds);
        }
    }
}