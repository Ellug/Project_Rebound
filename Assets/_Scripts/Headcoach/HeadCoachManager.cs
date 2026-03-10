using System;
using System.Collections.Generic;
using System.Linq;

// 감독 노드 시스템 매니저
// NodeContainer를 소유하고 외부 시스템(StudentManager 등)에 스탯 보너스를 제공
// 감독은 실존 객체가 없으므로 이 매니저가 그 역할을 대행
public class HeadCoachManager : Singleton<HeadCoachManager>
{
    public event Action OnTreeChanged;

    // 콘텐츠 해금 이벤트 (function_key 전달)
    public event Action<string> OnContentUnlocked;

    private readonly HeadCoachNodeContainer _container = new();

    // 콘텐츠/기능 해금 테이블 (functionId → ContentUnlockData)
    private readonly Dictionary<int, HeadCoachContentUnlockData> _contentUnlockMap = new();

    public bool IsInitialized { get; private set; } = false;

    protected override void OnSingletonAwake()
    {
        // TODO: 데이터 테이블 로드 완료 후 InitFromTable() 호출
    }

    // 초기화
    // 데이터 테이블로부터 노드/티어/효과/선행조건/콘텐츠 해금 데이터를 주입받아 트리를 구성
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

    // 해금
    // 명성치 차감 후 노드를 해금. 실패 시 false 반환
    public bool TryUnlockNode(int nodeId)
    {
        HeadCoachNode node = _container.GetNode(nodeId);
        if (node == null || node.IsUnlocked) return false;
        if (!node.ArePrerequisitesMet()) return false;
        if (!MoneyManager.Instance.TrySpendReputation(node.UnlockCost)) return false;

        node.SetUnlocked(true);

        HandleContentUnlock(node);
        CheckAndActivateTierGate(node.TierId);

        OnTreeChanged?.Invoke();
        return true;
    }

    // 티어 게이트
    // 현재 tierId의 일반 노드 해금 수가 승급 조건을 충족하면 티어 승급 노드를 자동 활성화
    private void CheckAndActivateTierGate(int tierId)
    {
        if (!_container.TryGetTierConfig(tierId, out HeadCoachTierConfigData tierConfig)) return;

        int unlockedCount = _container.GetNodesByTierId(tierId).Count(n => n.IsUnlocked);
        if (unlockedCount < tierConfig.unlockConditionCount) return;

        HeadCoachNode gateNode = _container.GetTierGateNode(tierId);
        if (gateNode == null || gateNode.IsUnlocked) return;

        gateNode.SetUnlocked(true);
        OnTreeChanged?.Invoke();
    }

    // 콘텐츠/기능 해금
    private void HandleContentUnlock(HeadCoachNode node)
    {
        int functionId = node.effectData.functionId;
        if (functionId == 0) return;
        if (!_contentUnlockMap.TryGetValue(functionId, out HeadCoachContentUnlockData content)) return;

        OnContentUnlocked?.Invoke(content.functionKey);
    }

    // 조회
    public HeadCoachNode GetNode(int nodeId) => _container.GetNode(nodeId);

    public IEnumerable<HeadCoachNode> GetNodesByCategory(NodeCategory category)
        => _container.GetNodesByCategory(category);

    // 해금된 노드의 targetStat 기준으로 effectValue를 합산하여 반환
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

    // 감독 노드 보너스를 특정 학생에게 즉시 적용
    // 노드 해금 시 또는 신입생 입학 시 호출
    public void ApplyStatBonusTo(Student student)
    {
        if (student == null) return;

        Dictionary<string, float> bonus = GetActiveStatBonus();

        foreach (KeyValuePair<string, float> pair in bonus)
        {
            int value = (int)pair.Value;
            ApplySingleBonus(student, pair.Key, value);
        }
    }

    // StudentManager의 전체 학생에게 일괄 적용
    public void ApplyStatBonusToAll()
    {
        if (StudentManager.Instance == null) return;
        foreach (Student student in StudentManager.Instance.Students)
            ApplyStatBonusTo(student);
    }

    private static void ApplySingleBonus(Student student, string targetStat, int value)
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

    // 저장 / 복원
    // SaveManager.SaveCurrent() 호출 시 사용 - 현재 해금된 노드 id 목록 반환
    public List<int> GetUnlockedNodeIds()
    {
        List<int> result = new();
        foreach (HeadCoachNode node in _container.GetAllNodes())
        {
            if (node.IsUnlocked)
                result.Add(node.NodeId);
        }

        return result;
    }

    // SaveManager.LoadSlot() 이후 호출 - 저장된 해금 목록을 트리에 복원
    // InitFromTable() 완료 후에 호출해야 함
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