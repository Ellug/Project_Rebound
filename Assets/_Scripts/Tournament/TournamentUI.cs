using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

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
    [Tooltip("팀 수 기준 내림차순: [0]=32강, [1]=16강, [2]=8강, [3]=4강, [4]=결승")]
    [SerializeField] private Sprite[] _roundTitleSprites = new Sprite[0];

    [Header("Round List")]
    [SerializeField] private ScrollRect _scrollRect;
    [SerializeField] private RectTransform _contentRoot;
    [SerializeField] private GameObject _matchupContainerPrefab;
    [SerializeField] private RectTransform _connectorPrefab;

    [Header("Bracket Layout")]
    [SerializeField] private float _layoutLeftPadding = 24f;
    [SerializeField] private float _layoutTopPadding = 24f;
    [SerializeField] private float _layoutRightPadding = 24f;
    [SerializeField] private float _layoutBottomPadding = 24f;
    [SerializeField] private float _firstRoundMatchSpacing = 82f;
    [SerializeField] private float _roundColumnSpacing = 160f;
    [SerializeField] private float _connectorXOffset = 64f;

    // 단일 라운드 입력을 다중 라운드 렌더러 포맷으로 감싸 전달
    public void RenderRound(IReadOnlyList<TournamentMatchViewData> matchups, string mySchoolName)
    {
        List<List<TournamentMatchViewData>> rounds = new(1) { new(matchups) };

        RenderRounds(rounds, 0, mySchoolName);
    }

    // 현재 라운드 상태에 맞춰 브래킷 패널 또는 결승 집중 패널을 갱신
    public void RenderRounds(IReadOnlyList<List<TournamentMatchViewData>> allRounds, int currentRoundIndex, string mySchoolName)
    {
        IReadOnlyList<TournamentMatchViewData> currentRound = allRounds[currentRoundIndex];
        int roundTeamCount = currentRound.Count * 2;
        bool isFinalRound = roundTeamCount == 2;

        UpdateRoundTitle(roundTeamCount);

        _roundListPanel.SetActive(!isFinalRound);
        _focusedMatchPanel.SetActive(isFinalRound);

        if (isFinalRound)
        {
            UpdateFocusedMatchPanel(currentRound, mySchoolName);
            return;
        }

        ClearRoundItems(_contentRoot);

        Vector2 matchupSize = ((RectTransform)_matchupContainerPrefab.transform).sizeDelta;
        RenderBracket(allRounds, currentRoundIndex, matchupSize.x, matchupSize.y);

        LayoutRebuilder.ForceRebuildLayoutImmediate(_contentRoot);
        _scrollRect.horizontalNormalizedPosition = 1f;
        _scrollRect.verticalNormalizedPosition = 1f;
    }

    // 누적된 라운드 데이터를 기준으로 매치업과 연결선을 배치
    private void RenderBracket(
        IReadOnlyList<List<TournamentMatchViewData>> allRounds,
        int currentRoundIndex,
        float matchupWidth,
        float matchupHeight)
    {
        int roundCount = currentRoundIndex + 1;
        List<float[]> roundCenters = BuildRoundCenters(allRounds, roundCount, matchupHeight);

        float maxCenterY = 0f;

        for (int roundIndex = 0; roundIndex < roundCount; roundIndex++)
        {
            IReadOnlyList<TournamentMatchViewData> roundMatchups = allRounds[roundIndex];
            float[] centers = roundCenters[roundIndex];
            float columnX = _layoutLeftPadding + roundIndex * (matchupWidth + _roundColumnSpacing);

            for (int matchupIndex = 0; matchupIndex < roundMatchups.Count; matchupIndex++)
            {
                TournamentMatchViewData matchup = roundMatchups[matchupIndex];
                float centerY = centers[matchupIndex];
                maxCenterY = Mathf.Max(maxCenterY, centerY);

                float topY = centerY - matchupHeight * 0.5f;
                CreateMatchupItem(matchup, roundIndex + 1, matchupIndex + 1, columnX, topY);
            }
        }

        for (int roundIndex = 0; roundIndex < roundCount - 1; roundIndex++)
        {
            float[] currentRoundCenters = roundCenters[roundIndex];
            float[] nextRoundCenters = roundCenters[roundIndex + 1];
            float columnX = _layoutLeftPadding + roundIndex * (matchupWidth + _roundColumnSpacing);
            float connectorOffset = Mathf.Clamp(_connectorXOffset, 0f, _roundColumnSpacing);
            float connectorX = columnX + matchupWidth + connectorOffset;

            for (int pairIndex = 0; pairIndex < nextRoundCenters.Length; pairIndex++)
            {
                int topIndex = pairIndex * 2;
                int bottomIndex = topIndex + 1;

                float topCenterY = currentRoundCenters[topIndex];
                float bottomCenterY = currentRoundCenters[bottomIndex];
                float connectorCenterY = (topCenterY + bottomCenterY) * 0.5f;
                float connectorHeight = bottomCenterY - topCenterY;
                maxCenterY = Mathf.Max(maxCenterY, bottomCenterY);

                CreateConnectorItem(roundIndex + 1, pairIndex + 1, connectorX, connectorCenterY, connectorHeight);
            }
        }

        float totalWidth = _layoutLeftPadding
            + roundCount * matchupWidth
            + Mathf.Max(roundCount - 1, 0) * _roundColumnSpacing
            + _layoutRightPadding;
        float totalHeight = Mathf.Max(
            _layoutTopPadding + matchupHeight + _layoutBottomPadding,
            maxCenterY + matchupHeight * 0.5f + _layoutBottomPadding);

        _contentRoot.sizeDelta = new Vector2(totalWidth, totalHeight);
    }

    // 각 라운드 매치업의 수직 중심 좌표를 계산해 브래킷 정렬 기준으로 사용
    private List<float[]> BuildRoundCenters(IReadOnlyList<List<TournamentMatchViewData>> allRounds, int roundCount, float matchupHeight)
    {
        List<float[]> centers = new(roundCount);

        int firstRoundMatchCount = allRounds[0].Count;
        float[] firstRoundCenters = new float[firstRoundMatchCount];
        for (int i = 0; i < firstRoundMatchCount; i++)
        {
            firstRoundCenters[i] = _layoutTopPadding + matchupHeight * 0.5f + i * (matchupHeight + _firstRoundMatchSpacing);
        }
        centers.Add(firstRoundCenters);

        for (int roundIndex = 1; roundIndex < roundCount; roundIndex++)
        {
            int currentMatchCount = allRounds[roundIndex].Count;
            float[] currentCenters = new float[currentMatchCount];
            float[] previousCenters = centers[roundIndex - 1];

            for (int i = 0; i < currentMatchCount; i++)
            {
                int upperIndex = i * 2;
                int lowerIndex = upperIndex + 1;
                currentCenters[i] = (previousCenters[upperIndex] + previousCenters[lowerIndex]) * 0.5f;
            }

            centers.Add(currentCenters);
        }

        return centers;
    }

    // 매치업 프리팹을 생성하고 위치/팀 데이터/강조 상태를 적용
    private void CreateMatchupItem(
        TournamentMatchViewData matchup,
        int roundOrder,
        int matchupOrder,
        float x,
        float topY)
    {
        RectTransform matchupRect = (RectTransform)Instantiate(_matchupContainerPrefab, _contentRoot).transform;
        matchupRect.name = $"TournamentUI (R{roundOrder}-{matchupOrder})";

        matchupRect.anchoredPosition = new Vector2(x, -topY);

        MatchupContainerUI matchupUi = matchupRect.GetComponent<MatchupContainerUI>();
        matchupUi.SetData(
            matchup.UpTeam,
            matchup.DownTeam,
            matchup.IsHighlighted,
            matchup.IsResolved,
            matchup.IsUpTeamWinner);
    }

    // 두 매치업 사이를 잇는 세로 커넥터를 생성하고 높이 적용
    private void CreateConnectorItem(int fromRoundOrder, int pairOrder, float x, float centerY, float height)
    {
        RectTransform connectorRect = Instantiate(_connectorPrefab, _contentRoot);
        connectorRect.name = $"TournamentConnector (R{fromRoundOrder}-{pairOrder})";

        connectorRect.anchoredPosition = new Vector2(x, -centerY);

        Vector2 size = connectorRect.sizeDelta;
        size.y = height;
        connectorRect.sizeDelta = size;
    }

    // 라운드 팀 수에 대응되는 타이틀 스프라이트로 상단 이미지 교체
    private void UpdateRoundTitle(int roundTeamCount)
    {
        int[] teamCounts = { 32, 16, 8, 4, 2 };
        for (int i = 0; i < teamCounts.Length && i < _roundTitleSprites.Length; i++)
        {
            if (teamCounts[i] != roundTeamCount)
                continue;

            _roundTitleImage.sprite = _roundTitleSprites[i];
            return;
        }
    }

    // 결승 집중 패널에 표시할 내 팀/상대 팀 이름을 반영
    private void UpdateFocusedMatchPanel(IReadOnlyList<TournamentMatchViewData> matchups, string mySchoolName)
    {
        string myTeam = string.IsNullOrWhiteSpace(mySchoolName) ? _unknownTeamText : mySchoolName;
        string opponentTeam = _unknownTeamText;

        for (int i = 0; i < matchups.Count; i++)
        {
            TournamentMatchViewData matchup = matchups[i];
            if (!matchup.IsHighlighted)
                continue;

            if (string.IsNullOrWhiteSpace(mySchoolName))
            {
                myTeam = matchup.UpTeam;
                opponentTeam = matchup.DownTeam;
                break;
            }

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

            break;
        }

        _focusedMySchoolText.text = myTeam;
        _focusedOpponentSchoolText.text = opponentTeam;
    }

    // 기존 라운드 아이템(매치업/커넥터)을 모두 제거한다.
    private static void ClearRoundItems(RectTransform root)
    {
        for (int i = root.childCount - 1; i >= 0; i--)
            Destroy(root.GetChild(i).gameObject);
    }
}

// 브래킷 UI에 전달되는 단일 매치업 표시용 데이터 구조체.
public readonly struct TournamentMatchViewData
{
    public readonly string UpTeam;
    public readonly string DownTeam;
    public readonly bool IsHighlighted;
    public readonly bool IsResolved;
    public readonly bool IsUpTeamWinner;

    public TournamentMatchViewData(string upTeam, string downTeam, bool isHighlighted)
        : this(upTeam, downTeam, isHighlighted, isResolved: false, isUpTeamWinner: false)
    {
    }

    public TournamentMatchViewData(string upTeam, string downTeam, bool isHighlighted, bool isResolved, bool isUpTeamWinner)
    {
        UpTeam = upTeam;
        DownTeam = downTeam;
        IsHighlighted = isHighlighted;
        IsResolved = isResolved;
        IsUpTeamWinner = isUpTeamWinner;
    }
}
