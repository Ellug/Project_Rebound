using System;
using System.Collections.Generic;
using System.Linq;

// 감독 노드 시스템 매니저
// NodeTree를 소유하고 외부 시스템(StudentManager 등)에 스탯 보너스를 제공
// 감독은 실존 객체가 없으므로 이 매니저가 그 역할을 대행
public class HeadCoachManager : Singleton<HeadCoachManager>
{
    public event Action OnTreeChanged;

    private HeadCoachNodeContainer _tree = new();

    protected override void OnSingletonAwake()
    {
        _tree = new HeadCoachNodeContainer();
    }

    // 노드 등록

    public void RegisterNode(HeadCoachNode node) => _tree.RegisterNode(node);

    public void LinkParent(string childId, string parentId) => _tree.LinkParent(childId, parentId);

    // 해금

    public void SetUnlocked(string nodeId, bool unlocked)
    {
        HeadCoachNode node = _tree.GetNode(nodeId);
        if (node == null) return;

        node.isUnlocked = unlocked;
        OnTreeChanged?.Invoke();
    }

    // 조회

    public IEnumerable<HeadCoachNode> GetNodesByCategory(NodeCategory category)
        => _tree.GetNodesByCategory(category);

    // 스탯 합산 (스탯 적용 주체에 제공)

    // 해금된 노드의 effectValue를 targetStatKey 기준으로 합산해 반환
    // StudentManager 등 실제 스탯 적용 주체가 이 값을 읽어 사용
    public Dictionary<string, float> GetActiveStatBonus()
    {
        var result = new Dictionary<string, float>();

        foreach (HeadCoachNode node in _tree.GetAllNodes().Where(n => n.isUnlocked))
        {
            string key = node.stat.targetStatKey;
            if (string.IsNullOrEmpty(key)) continue;

            if (!result.ContainsKey(key)) result[key] = 0f;
            result[key] += node.stat.effectValue;
        }

        return result;
    }
}