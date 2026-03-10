using System.Collections.Generic;
using UnityEngine;

// UI 테스트용 더미 데이터 주입 스크립트
// 테이블 연동 완료 후 이 스크립트는 삭제 예정
public class HeadCoachTestInItializer : MonoBehaviour
{
    void Start()
    {
        // 이미 초기화된 경우 복원만 수행 (Lobby 재진입 시)
        if (HeadCoachManager.Instance.IsInitialized)
        {
            RestoreIfNeeded();
            return;
        }

        HeadCoachManager.Instance.InitFromTable(
            BuildMasterRows(),
            BuildEffectRows(),
            BuildPrerequisiteRows(),
            BuildTierConfigRows(),
            BuildContentUnlockRows());

        RestoreIfNeeded();

        Debug.Log("[HeadCoachTestInitializer] 초기화 완료");
    }

    // 감독 노드 마스터 테이블 더미
    // 기획서 데이터 기준: 1티어(tierId=6001), 2티어(tierId=6002)
    private static IEnumerable<HeadCoachNodeData> BuildMasterRows()
    {
        return new List<HeadCoachNodeData>
        {
            // 1티어 - 공격
            new() { nodeId = 101, tierId = 6001, name = "슈팅 강화",         unlockCost = 15, description = "팀 전체의 Shoot능력치를 1 상승시킵니다.",          effectId = 1001 },
            new() { nodeId = 102, tierId = 6001, name = "슈팅 드릴 숙련",    unlockCost = 15, description = "슈팅 드릴 훈련 시 컨디션 소모가 줄어듭니다.",      effectId = 1003 },
            // 1티어 - 수비
            new() { nodeId = 201, tierId = 6001, name = "수비 강화",         unlockCost = 15, description = "팀 전체의 Jump능력치를 1 상승시킵니다.",           effectId = 2001 },
            // 1티어 - 지원
            new() { nodeId = 301, tierId = 6001, name = "명성치 획득량 증가", unlockCost = 15, description = "경기 후 획득하는 명성치가 10% 증가합니다.",        effectId = 3002 },
            new() { nodeId = 302, tierId = 6001, name = "시설 업그레이드 비용 감소", unlockCost = 15, description = "시설 업그레이드하는 비용이 5% 감소합니다.", effectId = 3001 },
            // 1티어 - 티어 승급 노드
            new() { nodeId = 501, tierId = 6001, name = "티어 승급 노드",    unlockCost = 0,  description = "조건 달성 시 활성화. 모든 능력치 +1 및 2티어 개방", effectId = 5001 },
            // 2티어 - 공격
            new() { nodeId = 103, tierId = 6002, name = "정밀 슈팅",         unlockCost = 25, description = "팀 전체의 Shoot능력치를 2 상승시킵니다.",          effectId = 1002 },
            // 2티어 - 수비
            new() { nodeId = 202, tierId = 6002, name = "정밀 수비",         unlockCost = 25, description = "팀 전체의 Jump능력치를 2 상승시킵니다.",           effectId = 2002 },
            // 2티어 - 지원 (콘텐츠 해금)
            new() { nodeId = 401, tierId = 6002, name = "선물 기능 해금",    unlockCost = 25, description = "학생들에게 선물을 줄 수 있는 기능이 해금됩니다.",   effectId = 4001 },
            new() { nodeId = 402, tierId = 6002, name = "장학금 기능 해금",  unlockCost = 25, description = "학생들에게 장학금을 줄 수 있는 기능이 해금됩니다.", effectId = 4002 },
        };
    }

    // 노드 효과 상세 테이블 더미
    private static IEnumerable<HeadCoachEffectData> BuildEffectRows()
    {
        return new List<HeadCoachEffectData>
        {
            new() { effectId = 1001, targetStat = "Atk_Shoot",                    applyMethod = ApplyMethod.Add, effectValue =  1f, functionId = 0 },
            new() { effectId = 1002, targetStat = "Adv_Atk_Shoot",                applyMethod = ApplyMethod.Add, effectValue =  2f, functionId = 0 },
            new() { effectId = 1003, targetStat = "Condition_Drain_ShootingDrill", applyMethod = ApplyMethod.Add, effectValue = -5f, functionId = 0 },
            new() { effectId = 2001, targetStat = "Def_Jump",                     applyMethod = ApplyMethod.Add, effectValue =  1f, functionId = 0 },
            new() { effectId = 2002, targetStat = "Adv_Def_Jump",                 applyMethod = ApplyMethod.Add, effectValue =  3f, functionId = 0 },
            new() { effectId = 3001, targetStat = "Facility_Upgrade_Cost",        applyMethod = ApplyMethod.Add, effectValue = -5f, functionId = 0 },
            new() { effectId = 3002, targetStat = "Fame_Gain_Rate",               applyMethod = ApplyMethod.Add, effectValue = 10f, functionId = 0 },
            new() { effectId = 4001, targetStat = "Content_Unlock",               applyMethod = ApplyMethod.Add, effectValue =  1f, functionId = 401 },
            new() { effectId = 4002, targetStat = "Content_Unlock",               applyMethod = ApplyMethod.Add, effectValue =  1f, functionId = 402 },
            new() { effectId = 5001, targetStat = "All_Stat",                     applyMethod = ApplyMethod.Add, effectValue =  1f, functionId = 0 },
        };
    }

    // 노드 선행 조건 테이블 더미
    private static IEnumerable<HeadCoachPrerequisiteData> BuildPrerequisiteRows()
    {
        return new List<HeadCoachPrerequisiteData>
        {
            // 102(슈팅 드릴 숙련)를 찍으려면 101(슈팅 강화) 필요
            new() { nodeId = 102, targetPrerequisiteId = 101 },
            // 103(정밀 슈팅)을 찍으려면 101(슈팅 강화) + 501(티어 승급) 필요
            new() { nodeId = 103, targetPrerequisiteId = 101 },
            new() { nodeId = 103, targetPrerequisiteId = 501 },
            // 202(정밀 수비)를 찍으려면 201(수비 강화) + 501(티어 승급) 필요
            new() { nodeId = 202, targetPrerequisiteId = 201 },
            new() { nodeId = 202, targetPrerequisiteId = 501 },
        };
    }

    // 티어 관리 및 개방 조건 테이블 더미
    private static IEnumerable<HeadCoachTierConfigData> BuildTierConfigRows()
    {
        return new List<HeadCoachTierConfigData>
        {
            // 1티어: 전체 6개 노드 중 3개 해금 시 티어 승급 노드 활성화
            new() { tierId = 6001, tierLevel = 1, tierName = "1",  unlockConditionCount = 3, maxNodeCount = 6, tierBonusEffectId = 5001 },
            // 2티어: 사실상 제한 없음 (99)
            new() { tierId = 6002, tierLevel = 2, tierName = "2",  unlockConditionCount = 99, maxNodeCount = 4, tierBonusEffectId = 0 },
        };
    }

    // 콘텐츠/기능 해금 테이블 더미
    private static IEnumerable<HeadCoachContentUnlockData> BuildContentUnlockRows()
    {
        return new List<HeadCoachContentUnlockData>
        {
            new() { functionId = 401, functionKey = "Unlock_Student_Gift",       contentName = "학생 선물 시스템", category = "Student", description = "학생에게 선물을 주어 호감도를 높이는 기능 개방" },
            new() { functionId = 402, functionKey = "Unlock_Scholarship_System", contentName = "장학금 시스템",    category = "Student", description = "기획 중" },
        };
    }

    private static void RestoreIfNeeded()
    {
        PlayData data = SaveManager.Instance.CurrentData;
        if (data != null)
            HeadCoachManager.Instance.RestoreUnlockedNodes(data.unlockedNodeIds);
    }
}