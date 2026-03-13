using System.Collections;
using TMPro;
using UnityEngine;

// 티어 게이트 슬롯 (자물쇠 아이콘 + 현재 해금 수 / 조건 수 표시)
public class HeadCoachTierGateSlot : MonoBehaviour
{
    [Header("자물쇠 오브젝트")]
    [SerializeField] private GameObject _goLocked;
    [SerializeField] private GameObject _goUnlocked;

    [Header("진행도 텍스트")]
    [SerializeField] private TMP_Text _txtCurrent;
    [SerializeField] private TMP_Text _txtSlash;
    [SerializeField] private TMP_Text _txtTarget;

    [Header("텍스트 색상")]
    [SerializeField] private Color _colorWhite = Color.white;
    [SerializeField] private Color _colorBlack = Color.black;

    [Header("노드 ID")]
    [SerializeField] private int _nodeId;

    public int NodeId => _nodeId;

    private void Awake()
    {
        ApplyLockedState();
        SetProgressText(0, 0, false);
    }

    private void OnEnable()
    {
        StartCoroutine(CoRefreshWhenReady());
    }

    private IEnumerator CoRefreshWhenReady()
    {
        yield return null;

        int safety = 10;
        while (safety-- > 0)
        {
            if (HeadCoachManager.Instance != null)
            {
                HeadCoachNode gateNode = HeadCoachManager.Instance.GetNode(_nodeId);
                if (gateNode != null)
                {
                    RefreshSlot();
                    yield break;
                }
            }

            yield return null;
        }

        Debug.LogWarning($"[HeadCoachTierGateSlot] 초기 Refresh 실패, nodeId={_nodeId}");
    }

    public void RefreshSlot()
    {
        if (HeadCoachManager.Instance == null)
        {
            ApplyLockedState();
            return;
        }

        HeadCoachNode gateNode = HeadCoachManager.Instance.GetNode(_nodeId);
        if (gateNode == null)
        {
            ApplyLockedState();
            return;
        }

        if (!HeadCoachManager.Instance.TryGetTierConfig(gateNode.TierId, out HeadCoachTierConfigData tierConfig))
        {
            ApplyLockedState();
            return;
        }

        int unlockedCount = HeadCoachManager.Instance.GetUnlockedCountByTierId(gateNode.TierId);
        int targetCount = tierConfig.unlockConditionCount;

        // 목표 수 초과 표시 방지
        unlockedCount = Mathf.Min(unlockedCount, targetCount);

        // 티어 게이트 슬롯은 "현재 티어 해금 수가 목표 수를 달성했는가"로 판정
        bool isGateUnlocked = unlockedCount >= targetCount;

        if (isGateUnlocked)
            ApplyUnlockedState();
        else
            ApplyLockedState();

        SetProgressText(unlockedCount, targetCount, isGateUnlocked);

        Debug.Log(
            $"[HeadCoachTierGateSlot] nodeId={_nodeId}, tierId={gateNode.TierId}, " +
            $"unlockedCount={unlockedCount}, targetCount={targetCount}, " +
            $"isUnlocked={gateNode.IsUnlocked}, isGateUnlocked={isGateUnlocked}");
    }

    private void ApplyLockedState()
    {
        if (_goUnlocked != null)
            _goUnlocked.SetActive(false);

        if (_goLocked != null)
            _goLocked.SetActive(true);
    }

    private void ApplyUnlockedState()
    {
        if (_goUnlocked != null)
            _goUnlocked.SetActive(true);

        if (_goLocked != null)
            _goLocked.SetActive(false);
    }

    private void SetProgressText(int current, int target, bool isGateUnlocked)
    {
        if (_txtCurrent != null)
        {
            _txtCurrent.text = current.ToString();
            _txtCurrent.color = isGateUnlocked ? _colorBlack : _colorWhite;
        }

        if (_txtTarget != null)
        {
            _txtTarget.text = target.ToString();
            _txtTarget.color = isGateUnlocked ? _colorWhite : _colorBlack;
        }

        if (_txtSlash != null)
        {
            _txtSlash.text = "/";
            _txtSlash.color = isGateUnlocked ? _colorWhite : _colorBlack;
        }
    }
}