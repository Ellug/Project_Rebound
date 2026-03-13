using System;
using System.Collections.Generic;
using System.Linq;

// 감독 노드 시스템 매니저
// NodeContainer를 소유하고 노드 해금/조회/저장복원을 담당
// 해금된 노드의 스탯 보너스를 StudentManager 등 외부에 제공
public class HeadCoachManager : Singleton<HeadCoachManager>
{
    public event Action OnTreeChanged;
    public event Action<string> OnContentUnlocked;

    private readonly HeadCoachNodeContainer _container = new();
    private readonly Dictionary<int, HeadCoachContentUnlockData> _contentUnlockMap = new();

    public bool IsInitialized { get; private set; }

    protected override void OnSingletonAwake()
    {
    }

    // 테이블 핫리로드 또는 에디터 재진입 시 호출해 컨테이너를 비우고 재초기화 허용
    public void ResetContainer()
    {
        _container.Clear();
        _contentUnlockMap.Clear();
        IsInitialized = false;
    }

    // StartManager의 Initializing 시점에 HeadCoachTableInitializer를 통해 호출
    public void InitFromTable(
        IEnumerable<HeadCoachNodeData> masterRows,
        IEnumerable<HeadCoachEffectData> effectRows,
        IEnumerable<HeadCoachPrerequisiteData> prerequisiteRows,
        IEnumerable<HeadCoachTierConfigData> tierConfigRows,
        IEnumerable<HeadCoachContentUnlockData> contentUnlockRows)
    {
        if (IsInitialized) return;
        IsInitialized = true;

        Dictionary<int, HeadCoachEffectData> effectMap = effectRows.ToDictionary(e => e.effectId);

        foreach (HeadCoachTierConfigData tierConfig in tierConfigRows)
            _container.RegisterTierConfig(tierConfig);

        foreach (HeadCoachContentUnlockData content in contentUnlockRows)
            _contentUnlockMap[content.functionId] = content;

        foreach (HeadCoachNodeData nodeData in masterRows)
        {
            effectMap.TryGetValue(nodeData.effectId, out HeadCoachEffectData effectData);

            // effectId 5000번대는 티어 승급 노드
            bool isTierGate = nodeData.effectId >= 5000 && nodeData.effectId < 6000;

            HeadCoachNode node = new()
            {
                nodeData = nodeData,
                effectData = effectData,
                nodeType = isTierGate ? NodeType.TierGate : NodeType.Normal,
            };

            _container.RegisterNode(node);
        }

        foreach (HeadCoachPrerequisiteData prerequisite in prerequisiteRows)
            _container.AddPrerequisite(prerequisite.nodeId, prerequisite.targetPrerequisiteId);
    }

    // 명성치 차감 후 노드 해금, 실패 시 false 반환
    public bool TryUnlockNode(int nodeId)
    {
        HeadCoachNode node = _container.GetNode(nodeId);
        if (node == null || node.IsUnlocked) return false;
        if (!node.ArePrerequisitesMet()) return false;
        if (!MoneyManager.Instance.TrySpendReputation(node.UnlockCost)) return false;

        node.SetUnlocked(true);

        HandleContentUnlock(node);
        CheckTierGateActivation(node.TierId);

        OnTreeChanged?.Invoke();
        return true;
    }

    // 현재 티어의 해금 수가 조건을 충족하면 티어 게이트 노드를 자동 활성화
    private void CheckTierGateActivation(int tierId)
    {
        if (!_container.TryGetTierConfig(tierId, out HeadCoachTierConfigData tierConfig)) return;

        int unlockedCount = _container.GetNodesByTierId(tierId).Count(n => n.IsUnlocked);
        if (unlockedCount < tierConfig.unlockConditionCount) return;

        HeadCoachNode gateNode = _container.GetTierGateNode(tierId);
        if (gateNode == null || gateNode.IsUnlocked) return;

        gateNode.SetUnlocked(true);
        OnTreeChanged?.Invoke();
    }

    private void HandleContentUnlock(HeadCoachNode node)
    {
        int functionId = node.effectData.functionId;
        if (functionId == 0) return;
        if (!_contentUnlockMap.TryGetValue(functionId, out HeadCoachContentUnlockData content)) return;

        OnContentUnlocked?.Invoke(content.functionKey);
    }

    public HeadCoachNode GetNode(int nodeId)
    {
        return _container.GetNode(nodeId);
    }

    public bool TryGetTierConfig(int tierId, out HeadCoachTierConfigData tierConfig)
    {
        return _container.TryGetTierConfig(tierId, out tierConfig);
    }

    // 특정 티어의 현재 해금된 일반 노드 수 반환
    public int GetUnlockedCountByTierId(int tierId)
    {
        return _container.GetNodesByTierId(tierId).Count(n => n.IsUnlocked);
    }

    public IEnumerable<HeadCoachNode> GetNodesByCategory(NodeCategory category)
    {
        return _container.GetNodesByCategory(category);
    }

    // 해금된 노드의 targetStat 기준으로 effectValue를 합산해 반환
    // StudentManager 등 실제 스탯 적용 주체가 이 값을 읽어 사용
    public Dictionary<string, float> GetActiveStatBonus()
    {
        Dictionary<string, float> result = new();

        foreach (HeadCoachNode node in _container.GetAllNodes().Where(n => n.IsUnlocked))
        {
            string key = node.effectData.targetStat;
            if (string.IsNullOrEmpty(key)) continue;

            if (!result.ContainsKey(key)) result[key] = 0f;
            result[key] += node.effectData.effectValue;
        }

        return result;
    }

    public void ApplyStatBonusTo(Student student)
    {
        if (student == null) return;

        foreach (KeyValuePair<string, float> pair in GetActiveStatBonus())
            ApplyStatBonus(student, pair.Key, (int)pair.Value);
    }

    public void ApplyStatBonusToAll()
    {
        if (StudentManager.Instance == null) return;

        foreach (Student student in StudentManager.Instance.Students)
            ApplyStatBonusTo(student);
    }

    private static void ApplyStatBonus(Student student, string targetStat, int value)
    {
        switch (targetStat)
        {
            case "Atk_Shoot":
            case "Adv_Atk_Shoot":
                student.shoot += value;
                break;
            case "Def_Jump":
            case "Adv_Def_Jump":
                student.jump += value;
                break;
            case "All_Stat":
                student.mental += value;
                student.shoot += value;
                student.speed += value;
                student.jump += value;
                student.stamina += value;
                break;
                // TODO: 효과 테이블 targetStat 항목 추가 시 case 확장
        }
    }

    // SaveManager.SaveCurrent() 호출 시 사용
    public List<int> GetUnlockedNodeIds()
    {
        List<int> result = new();

        foreach (HeadCoachNode node in _container.GetAllNodes())
            if (node.IsUnlocked) result.Add(node.NodeId);

        return result;
    }

    // SaveManager.LoadSlot() 이후 호출, InitFromTable() 완료 후에 사용해야 함
    public void RestoreUnlockedNodes(List<int> unlockedNodeIds)
    {
        if (unlockedNodeIds == null) return;

        foreach (int nodeId in unlockedNodeIds)
        {
            HeadCoachNode node = _container.GetNode(nodeId);
            if (node == null) continue;
            node.SetUnlocked(true);
        }

        OnTreeChanged?.Invoke();
    }
}