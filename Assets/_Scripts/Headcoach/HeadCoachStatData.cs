using System;

// 노드 카테고리: 공격 / 수비 / 지원
public enum NodeCategory { Attack, Defense, Support }

// 노드 효과 적용 방식
public enum ApplyMethod { Add }

// 노드 타입: 일반 노드 / 티어 승급 노드
public enum NodeType { Normal, TierGate }

// 감독 노드 마스터 테이블 데이터 구조체(1행)
// id, tier_id, name, unlock_cost, description, effect_id
[Serializable]
public struct HeadCoachNodeData
{
    public int nodeId;                  // 마스터 테이블 id
    public int tierId;                  // 티어 관리 테이블 id (tier_id)
    public string name;
    public int unlockCost;              // 명성치 소모량
    public string description;
    public int effectId;                // 노드 효과 상세 테이블 id
}

// 노드 효과 상세 테이블 데이터 구조체(1행)
// id, target_stat, apply_method, effect_value, function_id
[Serializable]
public struct HeadCoachEffectData
{
    public int effectId;
    public string targetStat;           // 적용 대상 스탯 키
    public ApplyMethod applyMethod;
    public float effectValue;
    public int functionId;              // 0이면 콘텐츠 해금 없음
}

// 노드 선행 조건 테이블 데이터 구조체(1행)
// id, node_id, target_prerequisite_id
[Serializable]
public struct HeadCoachPrerequisiteData
{
    public int id;
    public int nodeId;                  // 해금 대상 노드
    public int targetPrerequisiteId;    // 필요 선행 노드
}

// 콘텐츠/기능 해금 테이블 데이터 구조체(1행)
// id, function_key, name, category, description
[Serializable]
public struct HeadCoachContentUnlockData
{
    public int functionId;
    public string functionKey;
    public string contentName;
    public string category;
    public string description;
}

// 티어 관리 및 개방 조건 테이블 1행 데이터 구조체
// id, tier_level, tier_name, unlock_condition_count, max_node_count, tier_bonus_effect_id
[Serializable]
public struct HeadCoachTierConfigData
{
    public int tierId;
    public int tierLevel;
    public string tierName;
    public int unlockConditionCount;     // 다음 티어 진입에 필요한 현재 티어 해금 수
    public int maxNodeCount;
    public int tierBonusEffectId;        // 티어 승급 보너스 효과 id (없으면 0)
}