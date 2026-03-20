using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// 감독 트리 팝업 전체를 관리하는 클래스
// 상단: 보유 명성치
// 중단: 공격/지원·운영/수비 3레인 스크롤
// 하단: 노드 선택 시 슬라이드업 상세 패널
public class HeadCoachPopup : UIBase
{
    [Header("상단 영역")]
    [SerializeField] private TMP_Text _txtFame;

    [Header("스크롤")]
    [SerializeField] private ScrollRect _laneScrollRect;

    [Header("카테고리별 레인")]
    [SerializeField] private Transform _attackLaneRoot;
    [SerializeField] private Transform _defenseLaneRoot;
    [SerializeField] private Transform _supportLaneRoot;
    [SerializeField] private Transform _contentRoot; // HeadCoachTierGateSlot 탐색용

    [Header("노드 상세 팝업")]
    [SerializeField] private HeadCoachNodeInfoPopup _nodeInfoPopup;

    [Header("닫기 버튼")]
    [SerializeField] private Button _btnClose;

    private bool _isInited;
    private HeadCoachNodeSlot _selectedSlot;
    private int _selectedNodeId = -1;

    public override void Init()
    {
        if (_isInited) return;
        _isInited = true;
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
        StartCoroutine(ScrollToBottomNextFrame());
    }

    public override void Close()
    {
        HeadCoachManager.Instance.OnTreeChanged -= RefreshAll;
        MoneyManager.Instance.OnReputationChanged -= RefreshFameArea;

        ClearSelection();
        _nodeInfoPopup?.Hide();

        base.Close();
    }

    private void RefreshAll()
    {
        RefreshFameArea();
        RefreshNodeLanes();
    }

    private void RefreshFameArea(int _ = 0)
    {
        if (_txtFame != null)
            _txtFame.text = $"{MoneyManager.Instance.Reputation}";
    }

    private void RefreshNodeLanes()
    {
        RebuildLane(_attackLaneRoot);
        RebuildLane(_defenseLaneRoot);
        RebuildLane(_supportLaneRoot);
        RefreshTierGateSlots();
        RestoreHighlight();
    }

    private void RefreshTierGateSlots()
    {
        if (_contentRoot == null) return;

        foreach (HeadCoachTierGateSlot gateSlot in _contentRoot.GetComponentsInChildren<HeadCoachTierGateSlot>(true))
            gateSlot.RefreshSlot();
    }

    private void RebuildLane(Transform laneRoot)
    {
        if (laneRoot == null) return;

        foreach (Transform child in laneRoot)
        {
            HeadCoachNodeSlot slot = child.GetComponent<HeadCoachNodeSlot>();
            if (slot == null) continue;

            HeadCoachNode node = HeadCoachManager.Instance.GetNode(slot.NodeId);
            if (node == null)
            {
                slot.gameObject.SetActive(false);
                continue;
            }

            slot.gameObject.SetActive(true);
            slot.Setup(node, OnNodeSelected);
        }

        RefreshConnectors(laneRoot);
    }

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

    private void OnNodeSelected(int nodeId)
    {
        HeadCoachNode node = HeadCoachManager.Instance.GetNode(nodeId);
        if (node == null) return;

        ClearHighlight();

        _selectedNodeId = nodeId;
        _selectedSlot = FindSlot(nodeId);
        _selectedSlot?.SetHighlight(true);

        _nodeInfoPopup?.Show(node, OnUnlockRequested, ClearHighlight);
    }

    private void OnUnlockRequested(int nodeId)
    {
        bool success = HeadCoachManager.Instance.TryUnlockNode(nodeId);

        if (success)
        {
            SoundManager.Instance.PlayEffect(212);

            SaveManager.Instance.SaveCurrent();

            RestoreHighlight();
            _nodeInfoPopup?.RefreshPopup();
        }
    }

    private HeadCoachNodeSlot FindSlot(int nodeId)
    {
        foreach (Transform laneRoot in new[] { _attackLaneRoot, _defenseLaneRoot, _supportLaneRoot })
        {
            if (laneRoot == null) continue;

            foreach (Transform child in laneRoot)
            {
                HeadCoachNodeSlot slot = child.GetComponent<HeadCoachNodeSlot>();
                if (slot != null && slot.NodeId == nodeId)
                    return slot;
            }
        }

        return null;
    }

    // 하이라이트만 해제, 선택 정보는 유지
    private void ClearHighlight()
    {
        _selectedSlot?.SetHighlight(false);
    }

    // 하이라이트 + 선택 정보까지 완전 초기화
    private void ClearSelection()
    {
        _selectedSlot?.SetHighlight(false);
        _selectedSlot = null;
        _selectedNodeId = -1;
    }

    private void RestoreHighlight()
    {
        if (_selectedNodeId < 0)
            return;

        _selectedSlot?.SetHighlight(false);
        _selectedSlot = FindSlot(_selectedNodeId);
        _selectedSlot?.SetHighlight(true);
    }

    private IEnumerator ScrollToBottomNextFrame()
    {
        if (_laneScrollRect == null)
            yield break;

        yield return null;
        Canvas.ForceUpdateCanvases();
        _laneScrollRect.verticalNormalizedPosition = 0f;
    }
}
