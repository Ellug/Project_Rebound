using System.Collections;
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
    // 진입 시 스크롤 초점을 최하단(1티어 시작점)에 맞춤 (기획서 4-1. 중단 영역)
    [SerializeField] private ScrollRect _laneScrollRect;

    [Header("중단 영역 - 카테고리별 레인")]
    [SerializeField] private HeadCoachNodeSlot _nodeSlotPrefab;
    [SerializeField] private Transform _attackLaneRoot;
    [SerializeField] private Transform _defenseLaneRoot;
    [SerializeField] private Transform _supportLaneRoot;
    [SerializeField] private float _slotHeight = 120f;  // 슬롯 하나의 높이
    [SerializeField] private float _slotSpacing = 8f;   // 슬롯 간 간격

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

        // 기획서 4-1: 진입 시 스크롤 초점을 최하단(1티어 시작점)에 맞춤
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

        foreach (Transform child in laneRoot)
            Destroy(child.gameObject);

        var nodes = new System.Collections.Generic.List<HeadCoachNode>(
            HeadCoachManager.Instance.GetNodesByCategory(category));

        int count = nodes.Count;
        float step = _slotHeight + _slotSpacing;

        // 1티어가 최하단에 오도록 아래(y=0)에서 위로 쌓음
        // i=0이 가장 아래, i=count-1이 가장 위
        for (int i = 0; i < count; i++)
        {
            HeadCoachNodeSlot slot = Instantiate(_nodeSlotPrefab, laneRoot);
            slot.Setup(nodes[i], OnNodeSelected);

            RectTransform rt = slot.GetComponent<RectTransform>();
            if (rt == null) continue;

            rt.anchorMin = new Vector2(0f, 0f);
            rt.anchorMax = new Vector2(1f, 0f);
            rt.pivot = new Vector2(0.5f, 0f);
            rt.sizeDelta = new Vector2(0f, _slotHeight);
            rt.anchoredPosition = new Vector2(0f, i * step);
        }

        // Lane RectTransform 높이를 슬롯 전체 높이에 맞게 조정
        if (laneRoot is RectTransform laneRt)
            laneRt.sizeDelta = new Vector2(laneRt.sizeDelta.x, count > 0 ? count * step - _slotSpacing : 0f);
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