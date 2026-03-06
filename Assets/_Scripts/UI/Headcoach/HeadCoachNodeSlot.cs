using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// 감독 노드 하나를 표현하는 슬롯
public class HeadCoachNodeSlot : MonoBehaviour
{
    [SerializeField] private TMP_Text _txtName;
    [SerializeField] private TMP_Text _txtDesc;
    [SerializeField] private Button _btnUnlock;
    [SerializeField] private GameObject _lockOverlay;   // 잠금 상태
    [SerializeField] private GameObject _unlockedBadge; // 해금 완료 상태

    private HeadCoachNode _node;
    private Action<string> _onUnlockRequested;

    public void Setup(HeadCoachNode node, Action<string> onUnlockRequested)
    {
        _node = node;
        _onUnlockRequested = onUnlockRequested;
        Refresh();
    }

    private void Refresh()
    {
        if (_node == null) return;

        SetText(_txtName, _node.stat.displayName);
        SetText(_txtDesc, _node.stat.description);

        bool unlocked = _node.isUnlocked;
        SafeSetActive(_unlockedBadge, unlocked);
        SafeSetActive(_lockOverlay, !unlocked);
        SafeSetActive(_btnUnlock?.gameObject, !unlocked);

        if (_btnUnlock != null)
        {
            _btnUnlock.onClick.RemoveAllListeners();
            _btnUnlock.onClick.AddListener(() => _onUnlockRequested?.Invoke(_node.stat.nodeId));
        }
    }
    private static void SetText(TMP_Text t, string v) { if (t != null) t.text = v; }
    private static void SafeSetActive(GameObject g, bool a) { if (g != null) g.SetActive(a); }
}