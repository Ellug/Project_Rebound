using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// 감독 노드 하나를 표현하는 슬롯
public class HeadCoachNodeSlot : MonoBehaviour
{
    [SerializeField] private TMP_Text _txtName;
    [SerializeField] private TMP_Text _txtCost;
    [SerializeField] private Button _btnNode;
    [SerializeField] private GameObject _lockOverlay;
    [SerializeField] private GameObject _unlockedBadge;

    [Header("카테고리 색상")]
    [SerializeField] private Image _nodeBackground;                           // 색상을 입힐 배경 Image
    [SerializeField] private Color _attackColor = new(0.85f, 0.25f, 0.25f);   // 빨강
    [SerializeField] private Color _defenseColor = new(0.25f, 0.50f, 0.85f);  // 파랑
    [SerializeField] private Color _supportColor = new(0.90f, 0.75f, 0.20f);  // 노랑

    private HeadCoachNode _node;
    private Action<int> _onNodeSelected;

    public void Setup(HeadCoachNode node, Action<int> onNodeSelected)
    {
        _node = node;
        _onNodeSelected = onNodeSelected;

        if (_btnNode != null)
        {
            _btnNode.onClick.RemoveAllListeners();
            _btnNode.onClick.AddListener(() => _onNodeSelected?.Invoke(_node.NodeId));
        }

        ApplyCategoryColor(node.Category);
        Refresh();
    }

    public void Refresh()
    {
        if (_node == null) return;

        SetText(_txtName, _node.Name);
        SetText(_txtCost, _node.IsUnlocked ? string.Empty : $"{_node.UnlockCost}");

        SafeSetActive(_unlockedBadge, _node.IsUnlocked);
        SafeSetActive(_lockOverlay, !_node.IsUnlocked && !_node.ArePrerequisitesMet());
    }

    private void ApplyCategoryColor(NodeCategory category)
    {
        if (_nodeBackground == null) return;

        _nodeBackground.color = category switch
        {
            NodeCategory.Attack => _attackColor,
            NodeCategory.Defense => _defenseColor,
            NodeCategory.Support => _supportColor,
            _ => Color.white,
        };
    }

    private static void SetText(TMP_Text t, string v) { if (t != null) t.text = v; }
    private static void SafeSetActive(GameObject g, bool a) { if (g != null) g.SetActive(a); }
}