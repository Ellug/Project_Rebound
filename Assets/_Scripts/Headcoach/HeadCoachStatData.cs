using System;

// 노드 카테고리: 공격 / 수비 / 지원
public enum NodeCategory { Attack, Defense, Support }

// 노드 티어: 1티어(기초) → 2티어(심화) 순차 개방
public enum NodeTier { Tier1 = 1, Tier2 = 2 }


// 스탯 하나의 순수 데이터 구조체 (값만 보유)
[Serializable]
public struct HeadCoachStatData
{
    public string nodeId;
    public string displayName;
    public string description;
    public NodeCategory category;
    public NodeTier tier;
    public string targetStatKey; // 적용 대상 스탯 키
    public float effectValue;    // 적용 수치
}