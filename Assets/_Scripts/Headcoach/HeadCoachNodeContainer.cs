using System.Collections.Generic;
using System.Linq;

// 노드를 논리 트리 구조로 보관하는 컨테이너
// 노드 등록 / 트리 구성 / 티어 게이트 조회만 담당
public class HeadCoachNodeContainer
{
    private readonly Dictionary<int, HeadCoachNode> _nodeMap = new();
    private readonly Dictionary<int, HeadCoachTierConfigData> _tierConfigMap = new();

    // 노드 등록
    public void RegisterNode(HeadCoachNode node)
    {
        _nodeMap[node.NodeId] = node;
    }

    public void RegisterTierConfig(HeadCoachTierConfigData tierConfig)
    {
        _tierConfigMap[tierConfig.tierId] = tierConfig;
    }

    // 트리 연결
    public void LinkParent(int childId, int parentId)
    {
        HeadCoachNode child = GetNode(childId);
        HeadCoachNode parent = GetNode(parentId);
        if (child == null || parent == null) return;

        child.parent = parent;
        parent.children.Add(child);
    }

    public void AddPrerequisite(int nodeId, int prerequisiteNodeId)
    {
        HeadCoachNode node = GetNode(nodeId);
        HeadCoachNode prerequisite = GetNode(prerequisiteNodeId);
        if (node == null || prerequisite == null) return;

        node.prerequisites.Add(prerequisite);
    }

    // 조회
    public HeadCoachNode GetNode(int nodeId)
    {
        _nodeMap.TryGetValue(nodeId, out HeadCoachNode node);
        return node;
    }

    public bool TryGetTierConfig(int tierId, out HeadCoachTierConfigData tierConfig)
    {
        return _tierConfigMap.TryGetValue(tierId, out tierConfig);
    }

    // 티어 오름차순 반환 → UI에서 Tier1 → Tier2 순서 보장
    public IEnumerable<HeadCoachNode> GetNodesByCategory(NodeCategory category)
    {
        return _nodeMap.Values
            .Where(n => n.Category == category && n.nodeType == NodeType.Normal)
            .OrderBy(n => n.TierId);
    }

    // 특정 tierId에 속한 일반 노드 목록
    public IEnumerable<HeadCoachNode> GetNodesByTierId(int tierId)
    {
        return _nodeMap.Values
            .Where(n => n.TierId == tierId && n.nodeType == NodeType.Normal);
    }

    // tierId에 속한 티어 승급 노드
    public HeadCoachNode GetTierGateNode(int tierId)
    {
        return _nodeMap.Values
            .FirstOrDefault(n => n.TierId == tierId && n.nodeType == NodeType.TierGate);
    }

    public IEnumerable<HeadCoachNode> GetAllNodes() => _nodeMap.Values;
}