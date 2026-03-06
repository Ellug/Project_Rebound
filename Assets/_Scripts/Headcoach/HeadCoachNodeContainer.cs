using System.Collections.Generic;
using System.Linq;

// 노드를 논리 트리 구조로 보관하는 컨테이너
// 노드 등록 / 트리 구성 / 조회만 담당
public class HeadCoachNodeContainer
{
    private readonly Dictionary<string, HeadCoachNode> _nodeMap = new();

    public void RegisterNode(HeadCoachNode node)
    {
        _nodeMap[node.stat.nodeId] = node;
    }

    public void LinkParent(string childId, string parentId)
    {
        HeadCoachNode child = GetNode(childId);
        HeadCoachNode parent = GetNode(parentId);
        if (child == null || parent == null) return;

        child.parent = parent;
        parent.children.Add(child);
    }

    // 없으면 null 반환
    public HeadCoachNode GetNode(string nodeId)
    {
        _nodeMap.TryGetValue(nodeId, out HeadCoachNode node);
        return node;
    }

    // 티어 오름차순 반환 → UI에서 Tier1 → Tier2 순서 보장
    public IEnumerable<HeadCoachNode> GetNodesByCategory(NodeCategory category)
    {
        return _nodeMap.Values
            .Where(n => n.stat.category == category)
            .OrderBy(n => (int)n.stat.tier);
    }

    // 전체 순회용
    public IEnumerable<HeadCoachNode> GetAllNodes() => _nodeMap.Values;
}