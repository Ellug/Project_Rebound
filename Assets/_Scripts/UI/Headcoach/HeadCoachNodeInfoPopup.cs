using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// 노드 선택 시 하단에서 슬라이드업으로 표시되는 상세 정보 팝업
public class HeadCoachNodeInfoPopup : MonoBehaviour
{
    [Header("이름 영역")]
    [SerializeField] private TMP_Text _txtName;
    [SerializeField] private Image _nameBackPanel;

    // 백패널은 색상이 아닌 스프라이트 교체 방식으로 처리
    [Header("이름 백패널 스프라이트")]
    [SerializeField] private Sprite _sprBackPanelAttack;
    [SerializeField] private Sprite _sprBackPanelDefense;
    [SerializeField] private Sprite _sprBackPanelSupport;
    [SerializeField] private Sprite _sprBackPanelTierGate;

    [Header("설명 영역")]
    [SerializeField] private TMP_Text _txtDescription;
    // 선행 조건 미충족 시 표시, 설명과의 간격은 _blockReasonTopPadding으로 제어
    [SerializeField] private TMP_Text _txtBlockReason;
    [SerializeField] private float _blockReasonTopPadding = 16f;

    [Header("해금 버튼")]
    [SerializeField] private Button _btnUnlock;
    [SerializeField] private Image _btnUnlockImage;
    [SerializeField] private TMP_Text _txtBtnCost;  // 해금 가능/명성치 부족 시 비용 표시

    // 버튼 스프라이트: 상태 × 계열 조합
    // 해금 버튼 = 해금 가능 상태 / 재화 부족 버튼 = 명성치 부족 상태 / 최대치 버튼 = 해금 완료 상태
    [Header("해금 버튼 스프라이트 (해금 가능)")]
    [SerializeField] private Sprite _sprBtnUnlockAttack;
    [SerializeField] private Sprite _sprBtnUnlockDefense;
    [SerializeField] private Sprite _sprBtnUnlockSupport;

    [Header("재화 부족 버튼 스프라이트 (해금 불가)")]
    [SerializeField] private Sprite _sprBtnShortageAttack;
    [SerializeField] private Sprite _sprBtnShortageDefense;
    [SerializeField] private Sprite _sprBtnShortageSupport;

    [Header("최대치 버튼 스프라이트 (해금 완료)")]
    [SerializeField] private Sprite _sprBtnMaxAttack;
    [SerializeField] private Sprite _sprBtnMaxDefense;
    [SerializeField] private Sprite _sprBtnMaxSupport;

    [Header("닫기 버튼")]
    [SerializeField] private Button _btnClose;

    [Header("텍스트 색상")]
    // 설명: 해금 전 검정 / 해금 후 흰색
    [SerializeField] private Color _descColorLocked = Color.black;
    [SerializeField] private Color _descColorUnlocked = Color.white;
    [SerializeField] private Color _blockReasonColor = new(1f, 0.45f, 0f, 1f);

    [Header("애니메이션")]
    [SerializeField] private PopupAnimator _animator;

    private HeadCoachNode _selectedNode;
    private Action<int> _onUnlockRequested;
    private Action _onHide; // 닫힐 때 호출 (하이라이트 해제 등)
    private bool _isInited;

    void Awake()
    {
        Init();
    }

    private void Init()
    {
        if (_isInited) return;
        _isInited = true;

        _btnClose?.onClick.AddListener(Hide);
    }

    public void Show(HeadCoachNode node, Action<int> onUnlockRequested, Action onHide = null)
    {
        Init();
        _selectedNode = node;
        _onUnlockRequested = onUnlockRequested;
        _onHide = onHide;

        _animator.Initialize();

        gameObject.SetActive(true);
        transform.SetAsLastSibling();
        RefreshPopup();
        _animator.PlayIn();
    }

    public void Hide()
    {
        if (!gameObject.activeSelf) return;

        _animator.PlayOut(() =>
        {
            gameObject.SetActive(false);
            _onHide?.Invoke();
            _selectedNode = null;
        });
    }

    // 해금 성공 후 팝업을 닫지 않고 현재 노드 상태로 갱신할 때 호출
    public void RefreshPopup()
    {
        if (_selectedNode == null) return;

        bool isUnlocked = _selectedNode.IsUnlocked;
        bool prereqMet = _selectedNode.ArePrerequisitesMet();
        bool prereqBlock = !isUnlocked && !prereqMet;

        RefreshNameArea();
        RefreshDescriptionArea(isUnlocked, prereqBlock);
        RefreshButton(isUnlocked, prereqBlock);
    }

    private void RefreshNameArea()
    {
        if (_txtName != null)
            _txtName.text = _selectedNode.Name;

        if (_nameBackPanel != null)
            _nameBackPanel.sprite = GetBackPanelSprite(_selectedNode);
    }

    private void RefreshDescriptionArea(bool isUnlocked, bool prereqBlock)
    {
        if (_txtDescription != null)
        {
            _txtDescription.text = _selectedNode.nodeData.description;
            _txtDescription.color = _descColorLocked;
        }

        SetActive(_txtBlockReason?.gameObject, prereqBlock);
        if (prereqBlock && _txtBlockReason != null)
        {
            _txtBlockReason.text = "이전 노드를 해금하여야 합니다.";
            _txtBlockReason.color = _blockReasonColor;

            // LayoutElement의 minHeight로 텍스트 높이 + 상단 여백 확보
            LayoutElement layoutElement = _txtBlockReason.GetComponent<LayoutElement>();
            if (layoutElement == null)
                layoutElement = _txtBlockReason.gameObject.AddComponent<LayoutElement>();

            _txtBlockReason.ForceMeshUpdate();
            layoutElement.minHeight = _txtBlockReason.preferredHeight + _blockReasonTopPadding;
        }
    }

    private void RefreshButton(bool isUnlocked, bool prereqBlock)
    {
        bool isFameShort = !isUnlocked && !prereqBlock
            && MoneyManager.Instance.Reputation < _selectedNode.UnlockCost;

        if (_btnUnlockImage != null)
            _btnUnlockImage.sprite = GetButtonSprite(isUnlocked, prereqBlock, isFameShort);

        // 해금 가능 또는 명성치 부족 모두 cost 텍스트 표시
        SetActive(_txtBtnCost?.gameObject, !isUnlocked && !prereqBlock);

        if (!isUnlocked && !prereqBlock && _txtBtnCost != null)
            _txtBtnCost.text = $"{_selectedNode.UnlockCost}";

        if (_btnUnlock != null)
        {
            _btnUnlock.interactable = true;
            _btnUnlock.onClick.RemoveAllListeners();

            if (isUnlocked || prereqBlock)
            {
                // 해금 완료 / 선행 미충족 → 아무것도 하지 않음
            }
            else if (isFameShort)
            {
                // 명성치 부족 → 안내 팝업
                _btnUnlock.onClick.AddListener(ShowShortageModal);
            }
            else
            {
                // 해금 가능 → 해금 실행
                _btnUnlock.onClick.AddListener(() => _onUnlockRequested?.Invoke(_selectedNode.NodeId));
            }
        }
    }

    private void OnUnlockClicked()
    {
        // RefreshButton에서 onClick을 직접 바인딩하므로 사용하지 않음
    }

    private void ShowShortageModal()
    {
        UIManager.Instance.ShowPopup(UIPopupRequest.Simple(
            title: "재화 부족",
            message: "노드를 해금하기엔 아직 이름이 \n알려지지 않았습니다. \n\n경기를 통해 명성을 더 쌓으세요.",
            onPrimary: null,
            onCancel: null,
            showCancel: false
        ));
    }

    private Sprite GetButtonSprite(bool isUnlocked, bool prereqBlock, bool isFameShort)
    {
        NodeCategory category = _selectedNode.Category;

        if (isUnlocked)
        {
            return category switch
            {
                NodeCategory.Attack => _sprBtnMaxAttack,
                NodeCategory.Defense => _sprBtnMaxDefense,
                NodeCategory.Support => _sprBtnMaxSupport,
                _ => null,
            };
        }

        if (prereqBlock)
        {
            return category switch
            {
                NodeCategory.Attack => _sprBtnShortageAttack,
                NodeCategory.Defense => _sprBtnShortageDefense,
                NodeCategory.Support => _sprBtnShortageSupport,
                _ => null,
            };
        }

        // 해금 가능 / 명성치 부족 모두 unlock 스프라이트 사용
        return category switch
        {
            NodeCategory.Attack => _sprBtnUnlockAttack,
            NodeCategory.Defense => _sprBtnUnlockDefense,
            NodeCategory.Support => _sprBtnUnlockSupport,
            _ => null,
        };
    }

    private Sprite GetBackPanelSprite(HeadCoachNode node)
    {
        if (node.nodeType == NodeType.TierGate) return _sprBackPanelTierGate;
        return node.Category switch
        {
            NodeCategory.Attack => _sprBackPanelAttack,
            NodeCategory.Defense => _sprBackPanelDefense,
            NodeCategory.Support => _sprBackPanelSupport,
            _ => null,
        };
    }

    private static void SetActive(GameObject target, bool active)
    {
        if (target != null) target.SetActive(active);
    }
}