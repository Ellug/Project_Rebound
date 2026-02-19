using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class TournamentUI : MonoBehaviour
{
    [Header("Round List")]
    [SerializeField] private ScrollRect _scrollRect;
    [SerializeField] private RectTransform _contentRoot;
    [SerializeField] private GameObject _matchupContainerPrefab;

    [Header("Match Panel")]
    [SerializeField] private GameObject _matchGamePanel;
    [SerializeField] private TMP_Text _leftSchoolText;
    [SerializeField] private TMP_Text _rightSchoolText;

    // 현재 라운드의 매치업 목록을 UI에 표시
    public void RenderRound(IReadOnlyList<TournamentMatchViewData> matchups)
    {
        if (_contentRoot == null || _matchupContainerPrefab == null)
        {
            Debug.LogWarning("[TournamentUI] 라운드 UI 참조가 비어 있습니다.");
            return;
        }

        ClearRoundItems();

        for (int i = 0; i < matchups.Count; i++)
        {
            TournamentMatchViewData matchup = matchups[i];
            GameObject matchupObject = Instantiate(_matchupContainerPrefab, _contentRoot);
            matchupObject.name = $"TournamentUI ({i + 1})";
            matchupObject.SetActive(true);

            MatchupContainerUI matchupUi = matchupObject.GetComponent<MatchupContainerUI>();
            if (matchupUi != null)
                matchupUi.SetData(matchup.UpTeam, matchup.DownTeam, matchup.IsHighlighted);
        }

        LayoutRebuilder.ForceRebuildLayoutImmediate(_contentRoot);
        if (_scrollRect != null)
            _scrollRect.verticalNormalizedPosition = 1f;
    }

    // 매치 게임 패널 표시 (학교명 설정)
    public void ShowMatchGamePanel(string leftSchoolName, string rightSchoolName)
    {
        if (_leftSchoolText != null)
            _leftSchoolText.text = leftSchoolName;

        if (_rightSchoolText != null)
            _rightSchoolText.text = rightSchoolName;

        if (_matchGamePanel != null)
            _matchGamePanel.SetActive(true);
    }

    // 매치 게임 패널 숨김
    public void HideMatchGamePanel()
    {
        if (_matchGamePanel != null)
            _matchGamePanel.SetActive(false);
    }

    // 기존 라운드 UI 아이템 전부 제거
    private void ClearRoundItems()
    {
        for (int i = _contentRoot.childCount - 1; i >= 0; i--)
        {
            GameObject childObject = _contentRoot.GetChild(i).gameObject;
            childObject.SetActive(false);
            Destroy(childObject);
        }
    }
}

public readonly struct TournamentMatchViewData
{
    public readonly string UpTeam;
    public readonly string DownTeam;
    public readonly bool IsHighlighted;

    public TournamentMatchViewData(string upTeam, string downTeam, bool isHighlighted)
    {
        UpTeam = upTeam;
        DownTeam = downTeam;
        IsHighlighted = isHighlighted;
    }
}
