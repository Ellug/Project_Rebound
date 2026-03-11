using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// 감독 트리 팝업 전체를 관리하는 클래스
// 상단: 보유 명성치 + 카테고리별 해금 현황
// 중단: 공격(좌) / 수비(중) / 지원(우) 3레인 스크롤
// 하단: 노드 선택 시 슬라이드업 상세 패널
public class HeadCoachPopup : UIBase
{
    [Header("상단 영역")]
    [SerializeField] private TMP_Text _txtFame;
    [SerializeField] private TMP_Text _txtAttackUnlockCount;
    [SerializeField] private TMP_Text _txtDefenseUnlockCount;
    [SerializeField] private TMP_Text _txtSupportUnlockCount;

    [Header("중단 영역 - 스크롤")]
    // ScrollRect: 3개 레인을 감싸는 스크롤 뷰
    // 진입 시 스크롤 초점을 최하단(1티어 시작점)에 맞춤
    [SerializeField] private ScrollRect _laneScrollRect;

    [Header("중단 영역 - 카테고리별 레인")]
    [SerializeField] private Transform _attackLaneRoot;
    [SerializeField] private Transform _defenseLaneRoot;
    [SerializeField] private Transform _supportLaneRoot;

    [Header("티어별 해금 현황")]
    // 티어별 해금 수 / 전체 노드 수 표시 (예: 2/5)
    // 티어 수만큼 Inspector에서 연결
    [SerializeField] private List<TMP_Text> _txtTierUnlockCounts;

    [Header("하단 영역 - 노드 상세")]
    [SerializeField] private HeadCoachNodeInfoPopup _nodeInfoPopup;

    [Header("공통")]
    [SerializeField] private Button _btnClose;

    private bool _inited = false;

    public override void Init()
    {
        if (_inited) return;
        _inited = true;
        base.Init();

        _btnClose?.onClick.AddListener(() => Close());
        _nodeInfoPopup?.Hide();
    }

    public override void Open()
    {
        base.Open();
        HeadCoachManager.Instance.OnTreeChanged -= RefreshAll;
        HeadCoachManager.Instance.OnTreeChanged += RefreshAll;
        MoneyManager.Instance.OnReputationChanged -= RefreshFameArea;
        MoneyManager.Instance.OnReputationChanged += RefreshFameArea;
        RefreshAll();

        // 진입 시 스크롤 초점을 최하단(1티어 시작점)에 맞춤
        StartCoroutine(ScrollToBottomNextFrame());
    }

    public override void Close()
    {
        HeadCoachManager.Instance.OnTreeChanged -= RefreshAll;
        MoneyManager.Instance.OnReputationChanged -= RefreshFameArea;
        _nodeInfoPopup?.Hide();
        base.Close();
    }

    // 전체 UI 갱신
    private void RefreshAll()
    {
        RefreshFameArea();
        RefreshNodeLanes();
        RefreshTierUnlockCounts();
    }

    // 상단 명성치 및 카테고리별 해금 현황 갱신
    private void RefreshFameArea(int _ = 0)
    {
        SetText(_txtFame, $"{MoneyManager.Instance.Reputation}");

        int attackCount = CountUnlocked(NodeCategory.Attack);
        int defenseCount = CountUnlocked(NodeCategory.Defense);
        int supportCount = CountUnlocked(NodeCategory.Support);

        SetText(_txtAttackUnlockCount, $"{attackCount}");
        SetText(_txtDefenseUnlockCount, $"{defenseCount}");
        SetText(_txtSupportUnlockCount, $"{supportCount}");
    }

    // 티어별 해금 수 / 전체 노드 수 갱신 (예: 2/5)
    private void RefreshTierUnlockCounts()
    {
        if (_txtTierUnlockCounts == null || _txtTierUnlockCounts.Count == 0) return;

        var tierGroups = new Dictionary<int, (int unlocked, int total)>();

        foreach (HeadCoachNode node in HeadCoachManager.Instance.GetNodesByCategory(NodeCategory.Attack))
            AddToTierGroup(tierGroups, node);
        foreach (HeadCoachNode node in HeadCoachManager.Instance.GetNodesByCategory(NodeCategory.Defense))
            AddToTierGroup(tierGroups, node);
        foreach (HeadCoachNode node in HeadCoachManager.Instance.GetNodesByCategory(NodeCategory.Support))
            AddToTierGroup(tierGroups, node);

        // tierId 오름차순으로 정렬 후 텍스트에 반영
        var sortedTiers = new List<int>(tierGroups.Keys);
        sortedTiers.Sort();

        for (int i = 0; i < _txtTierUnlockCounts.Count; i++)
        {
            if (i >= sortedTiers.Count)
            {
                SetText(_txtTierUnlockCounts[i], string.Empty);
                continue;
            }

            int tierId = sortedTiers[i];
            var (unlocked, total) = tierGroups[tierId];
            SetText(_txtTierUnlockCounts[i], $"{unlocked}/{total}");
        }
    }

    private static void AddToTierGroup(Dictionary<int, (int, int)> groups, HeadCoachNode node)
    {
        if (!groups.TryGetValue(node.TierId, out var counts))
            counts = (0, 0);

        groups[node.TierId] = (counts.Item1 + (node.IsUnlocked ? 1 : 0), counts.Item2 + 1);
    }

    // 카테고리별 레인 노드 슬롯 갱신
    private void RefreshNodeLanes()
    {
        RebuildLane(_attackLaneRoot, NodeCategory.Attack);
        RebuildLane(_defenseLaneRoot, NodeCategory.Defense);
        RebuildLane(_supportLaneRoot, NodeCategory.Support);
    }

    private void RebuildLane(Transform laneRoot, NodeCategory category)
    {
        if (laneRoot == null) return;

        var nodes = new List<HeadCoachNode>(
            HeadCoachManager.Instance.GetNodesByCategory(category));

        // 미리 배치된 슬롯을 순서대로 수집
        var slots = new List<HeadCoachNodeSlot>();
        foreach (Transform child in laneRoot)
        {
            HeadCoachNodeSlot slot = child.GetComponent<HeadCoachNodeSlot>();
            if (slot != null)
                slots.Add(slot);
        }

        int count = Mathf.Min(nodes.Count, slots.Count);
        for (int i = 0; i < count; i++)
        {
            slots[i].gameObject.SetActive(true);
            slots[i].Setup(nodes[i], OnNodeSelected);
        }

        // 슬롯 수보다 노드가 적으면 남는 슬롯 비활성화
        for (int i = count; i < slots.Count; i++)
            slots[i].gameObject.SetActive(false);

        RefreshConnectors(laneRoot);
    }

    // 레인 내 미리 배치된 커넥터 해금 상태에 따라 색상 갱신
    private static void RefreshConnectors(Transform laneRoot)
    {
        foreach (Transform child in laneRoot)
        {
            HeadCoachNodeConnector connector = child.GetComponent<HeadCoachNodeConnector>();
            if (connector == null) continue;

            HeadCoachNode fromNode = HeadCoachManager.Instance.GetNode(connector.fromNodeId);
            HeadCoachNode toNode = HeadCoachManager.Instance.GetNode(connector.toNodeId);

            if (fromNode == null || toNode == null) continue;

            connector.Refresh(fromNode.IsUnlocked && toNode.IsUnlocked);
        }
    }

    // 노드 선택 → 하단 상세 패널 표시
    private void OnNodeSelected(int nodeId)
    {
        HeadCoachNode node = HeadCoachManager.Instance.GetNode(nodeId);
        if (node == null) return;

        _nodeInfoPopup?.Show(node, OnUnlockRequested);
    }

    // 해금 시도
    private void OnUnlockRequested(int nodeId)
    {
        bool success = HeadCoachManager.Instance.TryUnlockNode(nodeId);
        if (success)
        {
            // 해금 성공 시 하단 상세 패널 슬라이드 아웃
            _nodeInfoPopup?.Hide();
        }
        if (!success)
        {
            // TODO: 명성치 부족 / 조건 미달 피드백 UI 연출
        }
    }

    // 노드 슬롯 생성 후 레이아웃 반영까지 1프레임 대기 후 최하단으로 이동
    private IEnumerator ScrollToBottomNextFrame()
    {
        if (_laneScrollRect == null) yield break;

        yield return null;

        Canvas.ForceUpdateCanvases();
        _laneScrollRect.verticalNormalizedPosition = 0f;
    }

    private int CountUnlocked(NodeCategory category)
    {
        int count = 0;
        foreach (HeadCoachNode node in HeadCoachManager.Instance.GetNodesByCategory(category))
        {
            if (node.IsUnlocked) count++;
        }

        return count;
    }

    private static void SetText(TMP_Text t, string v) { if (t != null) t.text = v; }
}