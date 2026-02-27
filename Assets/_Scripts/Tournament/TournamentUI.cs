using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// 라운드 대진표를 표시하는 View: 결승전은 집중 패널, 그 외는 스크롤 리스트로 렌더링
public class TournamentUI : MonoBehaviour
{
    [Header("Tournament Panel")]
    [SerializeField] private GameObject _roundListPanel;
    [SerializeField] private GameObject _focusedMatchPanel;
    [SerializeField] private TMP_Text _focusedMySchoolText;
    [SerializeField] private TMP_Text _focusedOpponentSchoolText;
    [SerializeField] private string _unknownTeamText = "-";

    [Header("Round Title Images")]
    [SerializeField] private Image _roundTitleImage;
    [Tooltip("팀 수 기준 내림차순으로 배치: [0]=32강, [1]=16강, [2]=8강, [3]=4강, [4]=결승")]
    [SerializeField] private Sprite[] _roundTitleSprites = new Sprite[0];

    [Header("Round List")]
    [SerializeField] private ScrollRect _scrollRect;
    [SerializeField] private RectTransform _contentRoot;
    [SerializeField] private GameObject _matchupContainerPrefab;

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

        _roundListPanel.SetActive(!isFinalRound);
        _focusedMatchPanel.SetActive(isFinalRound);

        if (isFinalRound)
        {
            UpdateFocusedMatchPanel(matchups, mySchoolName);
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
            matchupUi.SetData(matchup.UpTeam, matchup.DownTeam, matchup.IsHighlighted);
        }

        LayoutRebuilder.ForceRebuildLayoutImmediate(_contentRoot);
        _scrollRect.verticalNormalizedPosition = 1f;
    }

    // 팀 수(32→16→8→4→2)를 스프라이트 인덱스(0→1→2→3→4)로 변환해 이미지 교체
    private void UpdateRoundTitle(int roundTeamCount)
    {
        if (roundTeamCount <= 0 || _roundTitleSprites.Length == 0 || _roundTitleImage == null)
            return;

        // 지원 팀 수: 32, 16, 8, 4, 2 (인덱스 0~4)
        int[] teamCounts = { 32, 16, 8, 4, 2 };
        int targetIndex = -1;
        for (int i = 0; i < teamCounts.Length; i++)
        {
            if (teamCounts[i] == roundTeamCount)
            {
                targetIndex = i;
                break;
            }
        }

        if (targetIndex >= 0 && targetIndex < _roundTitleSprites.Length)
            _roundTitleImage.sprite = _roundTitleSprites[targetIndex];
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

        _focusedMySchoolText.text = myTeam;
        _focusedOpponentSchoolText.text = opponentTeam;
    }

    // contentRoot의 자식 오브젝트를 역순으로 비활성화 후 Destroy (라운드 전환 시 호출)
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
