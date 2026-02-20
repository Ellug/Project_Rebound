using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class TournamentUI : MonoBehaviour
{
    [Header("Tournament Panel")]
    [SerializeField] private TMP_Text _roundTitleText;
    [SerializeField] private GameObject _roundListPanel;
    [SerializeField] private GameObject _focusedMatchPanel;
    [SerializeField] private TMP_Text _focusedMySchoolText;
    [SerializeField] private TMP_Text _focusedVsText;
    [SerializeField] private TMP_Text _focusedOpponentSchoolText;
    [SerializeField] private string _roundTitleFormat = "{ROUND} 토너먼트 표";
    [SerializeField] private string _finalRoundTitle = "결승";
    [SerializeField] private string _unknownTeamText = "-";

    [Header("Round List")]
    [SerializeField] private ScrollRect _scrollRect;
    [SerializeField] private RectTransform _contentRoot;
    [SerializeField] private GameObject _matchupContainerPrefab;

    [Header("Match Panel")]
    [SerializeField] private GameObject _matchGamePanel;
    [SerializeField] private TMP_Text _leftSchoolText;
    [SerializeField] private TMP_Text _rightSchoolText;

    [Header("Match Result Panel")]
    [SerializeField] private GameObject _matchResultPanel;
    [SerializeField] private TMP_Text _matchResultText;

    // 현재 라운드의 매치업 목록을 UI에 표시
    public void RenderRound(IReadOnlyList<TournamentMatchViewData> matchups)
    {
        RenderRound(matchups, null);
    }

    public void RenderRound(IReadOnlyList<TournamentMatchViewData> matchups, string mySchoolName)
    {
        int roundTeamCount = GetRoundTeamCount(matchups);
        bool isFinalRound = IsFinalRound(roundTeamCount);

        UpdateRoundTitle(roundTeamCount);

        if (_roundListPanel != null)
            _roundListPanel.SetActive(!isFinalRound);

        if (_focusedMatchPanel != null)
            _focusedMatchPanel.SetActive(isFinalRound);

        if (isFinalRound)
        {
            UpdateFocusedMatchPanel(matchups, mySchoolName);
            return;
        }

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

    private void UpdateRoundTitle(int roundTeamCount)
    {
        if (_roundTitleText == null)
            return;

        if (roundTeamCount <= 0)
            return;

        string roundText = IsFinalRound(roundTeamCount)
            ? _finalRoundTitle
            : $"{roundTeamCount}강";

        if (string.IsNullOrEmpty(_roundTitleFormat))
            _roundTitleText.text = roundText;
        else if (_roundTitleFormat.Contains("{ROUND}"))
            _roundTitleText.text = _roundTitleFormat.Replace("{ROUND}", roundText);
        else
            _roundTitleText.text = roundText;
    }

    private static int GetRoundTeamCount(IReadOnlyList<TournamentMatchViewData> matchups)
    {
        return matchups != null ? matchups.Count * 2 : 0;
    }

    private static bool IsFinalRound(int roundTeamCount)
    {
        return roundTeamCount == 2;
    }

    private void UpdateFocusedMatchPanel(IReadOnlyList<TournamentMatchViewData> matchups, string mySchoolName)
    {
        string myTeam = string.IsNullOrWhiteSpace(mySchoolName) ? _unknownTeamText : mySchoolName;
        string opponentTeam = _unknownTeamText;

        if (matchups != null)
        {
            for (int i = 0; i < matchups.Count; i++)
            {
                TournamentMatchViewData matchup = matchups[i];
                if (!matchup.IsHighlighted)
                    continue;

                if (!string.IsNullOrWhiteSpace(mySchoolName))
                {
                    if (matchup.UpTeam == mySchoolName)
                    {
                        myTeam = matchup.UpTeam;
                        opponentTeam = matchup.DownTeam;
                    }
                    else if (matchup.DownTeam == mySchoolName)
                    {
                        myTeam = matchup.DownTeam;
                        opponentTeam = matchup.UpTeam;
                    }
                    else
                    {
                        myTeam = matchup.UpTeam;
                        opponentTeam = matchup.DownTeam;
                    }
                }
                else
                {
                    myTeam = matchup.UpTeam;
                    opponentTeam = matchup.DownTeam;
                }

                break;
            }
        }

        if (_focusedMySchoolText != null)
            _focusedMySchoolText.text = myTeam;

        if (_focusedVsText != null)
            _focusedVsText.text = "VS";

        if (_focusedOpponentSchoolText != null)
            _focusedOpponentSchoolText.text = opponentTeam;
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

    // 경기 결과 패널 표시
    public void ShowMatchResultPanel(string resultText)
    {
        if (_matchResultText != null)
            _matchResultText.text = resultText;

        if (_matchResultPanel != null)
            _matchResultPanel.SetActive(true);
    }

    // 경기 결과 패널 숨김
    public void HideMatchResultPanel()
    {
        if (_matchResultPanel != null)
            _matchResultPanel.SetActive(false);
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
