using System;
using System.Collections.Generic;

// 감독 노드 하나를 표현하는 런타임 객체
// HeadCoachNodeData(마스터 테이블)를 기반으로 생성되며 해금 상태와 트리 연결을 관리
[Serializable]
public class HeadCoachNode
{
    public HeadCoachNodeData nodeData;
    public HeadCoachEffectData effectData;
    public NodeType nodeType;

    public bool IsUnlocked { get; private set; } = false;

    // 런타임 전용 트리 연결 (직렬화 제외)
    [NonSerialized] public HeadCoachNode parent;
    [NonSerialized] public List<HeadCoachNode> children = new();
    [NonSerialized] public List<HeadCoachNode> prerequisites = new();   // 선행 조건 노드 목록

    public int NodeId => nodeData.nodeId;
    public int TierId => nodeData.tierId;
    public string Name => nodeData.name;
    public int UnlockCost => nodeData.unlockCost;

    // 카테고리는 effect_id 범위로 판단
    // 1000번대: 공격, 2000번대: 수비, 3000~4000번대: 지원, 5000번대: 티어 승급
    public NodeCategory Category
    {
        get
        {
            int id = nodeData.effectId;
            if (id >= 1000 && id < 2000) return NodeCategory.Attack;
            if (id >= 2000 && id < 3000) return NodeCategory.Defense;
            return NodeCategory.Support;
        }
    }

    public void SetUnlocked(bool unlocked)
    {
        IsUnlocked = unlocked;
    }

    // 모든 선행 조건 노드가 해금되어 있는지 확인
    public bool ArePrerequisitesMet()
    {
        foreach (HeadCoachNode prerequisite in prerequisites)
        {
            if (!prerequisite.IsUnlocked) return false;
        }
        return true;
    }
}