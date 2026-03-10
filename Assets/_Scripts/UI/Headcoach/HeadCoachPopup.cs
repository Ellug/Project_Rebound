using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

//감독 트리 팝업 전체를 관리하는 클래스
public class HeadCoachPopup : UIBase
{
    [Header("Node List")]
    [SerializeField] private HeadCoachNodeSlot _nodeSlotPrefab;
    [SerializeField] private Transform _nodeListRoot; // 전체 노드 부모 (기획 확정 후 카테고리별 루트로 분리 가능)

    [Header("Close")]
    [SerializeField] private Button _btnClose;

    private bool _inited = false;

    public override void Init()
    {
        if (_inited) return;
        _inited = true;
        base.Init();

        _btnClose?.onClick.AddListener(() => Close());
    }

    public override void Open()
    {
        base.Open();
        HeadCoachManager.Instance.OnTreeChanged -= RefreshNodeList;
        HeadCoachManager.Instance.OnTreeChanged += RefreshNodeList;
        RefreshNodeList();
    }

    public override void Close()
    {
        HeadCoachManager.Instance.OnTreeChanged -= RefreshNodeList;
        base.Close();
    }

    // 전체 노드를 카테고리 순서로 표시
    private void RefreshNodeList()
    {
        foreach (Transform child in _nodeListRoot)
            Destroy(child.gameObject);

        // Attack → Defense → Support 순으로 전체 출력
        foreach (NodeCategory category in new[] { NodeCategory.Attack, NodeCategory.Defense, NodeCategory.Support })
        {
            IEnumerable<HeadCoachNode> nodes = HeadCoachManager.Instance.GetNodesByCategory(category);
            foreach (HeadCoachNode node in nodes)
            {
                HeadCoachNodeSlot slot = Instantiate(_nodeSlotPrefab, _nodeListRoot);
                slot.Setup(node, OnUnlockRequested);
            }
        }
    }
    private void OnUnlockRequested(string nodeId)
    {
        HeadCoachManager.Instance.SetUnlocked(nodeId, true);
    }
}
