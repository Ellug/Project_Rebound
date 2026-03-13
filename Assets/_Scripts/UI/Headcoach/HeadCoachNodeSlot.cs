using System;
using UnityEngine;
using UnityEngine.UI;

// 감독 트리에서 일반 노드 하나를 표현하는 슬롯
// 티어 게이트 노드는 HeadCoachTierGateSlot 사용
public class HeadCoachNodeSlot : MonoBehaviour
{
    [Header("버튼")]
    [SerializeField] private Button _btnNode;

    // 슬롯 본체 배경 이미지: 해금됨(별) / 해금 가능(검정) / 해금조건 미충족(어두운 테두리) 스프라이트 교체
    [Header("슬롯 본체 Image")]
    [SerializeField] private Image _slotImage;

    [Header("슬롯 스프라이트 (공격)")]
    [SerializeField] private Sprite _sprAttackUnlocked;
    [SerializeField] private Sprite _sprAttackUnlockable;
    [SerializeField] private Sprite _sprAttackLocked;

    [Header("슬롯 스프라이트 (수비)")]
    [SerializeField] private Sprite _sprDefenseUnlocked;
    [SerializeField] private Sprite _sprDefenseUnlockable;
    [SerializeField] private Sprite _sprDefenseLocked;

    [Header("슬롯 스프라이트 (지원)")]
    [SerializeField] private Sprite _sprSupportUnlocked;
    [SerializeField] private Sprite _sprSupportUnlockable;
    [SerializeField] private Sprite _sprSupportLocked;

    [Header("하이라이트")]
    [SerializeField] private GameObject _highlight;

    [Header("노드 ID")]
    [SerializeField] private int _nodeId;

    private HeadCoachNode _node;
    private Action<int> _onNodeSelected;
    private bool _isHighlighted;

    public int NodeId => _nodeId;

    private void Awake()
    {
        ApplyHighlight();
    }

    public void Setup(HeadCoachNode node, Action<int> onNodeSelected)
    {
        _node = node;
        _onNodeSelected = onNodeSelected;

        if (_btnNode != null)
        {
            _btnNode.onClick.RemoveAllListeners();
            _btnNode.onClick.AddListener(() => _onNodeSelected?.Invoke(_node.NodeId));
        }

        RefreshSlot();
    }

    public void RefreshSlot()
    {
        if (_node == null)
            return;

        bool isUnlocked = _node.IsUnlocked;
        bool prereqMet = _node.ArePrerequisitesMet();
        NodeCategory category = _node.Category;

        SetSprite(_slotImage, GetSlotSprite(category, isUnlocked, prereqMet));
        ApplyHighlight();
    }

    private Sprite GetSlotSprite(NodeCategory category, bool isUnlocked, bool prereqMet)
    {
        return category switch
        {
            NodeCategory.Attack => isUnlocked ? _sprAttackUnlocked : prereqMet ? _sprAttackUnlockable : _sprAttackLocked,
            NodeCategory.Defense => isUnlocked ? _sprDefenseUnlocked : prereqMet ? _sprDefenseUnlockable : _sprDefenseLocked,
            NodeCategory.Support => isUnlocked ? _sprSupportUnlocked : prereqMet ? _sprSupportUnlockable : _sprSupportLocked,
            _ => null,
        };
    }

    // 노드 정보창 열림/닫힘에 따라 외부에서 호출
    public void SetHighlight(bool active)
    {
        _isHighlighted = active;
        ApplyHighlight();
    }

    private void ApplyHighlight()
    {
        if (_highlight != null)
            _highlight.SetActive(_isHighlighted);
    }

    private static void SetSprite(Image image, Sprite sprite)
    {
        if (image != null)
            image.sprite = sprite;
    }
}