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

    [Header("Bracket Template")]
    [SerializeField] private RectTransform _bracketTemplatePrefab;

    private bool _hasPlayedFinalBracketSfx;

    // 친선전 모드에서 토너먼트 전용 패널 숨김
    public void HideTournamentPanels()
    {
        _roundListPanel.SetActive(false);
        _focusedMatchPanel.SetActive(false);
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
            if (!_hasPlayedFinalBracketSfx)
            {
                SoundManager.Instance.PlayEffect(305);
                _hasPlayedFinalBracketSfx = true;
            }
        }
        else
        {
            _hasPlayedFinalBracketSfx = false;
        }

        if (isFinalRound)
        {
            UpdateFocusedMatchPanel(currentRound, mySchoolName);
            return;
        }

        ClearRoundItems(_contentRoot);
        RenderBracketFromTemplate(allRounds, currentRoundIndex);

        LayoutRebuilder.ForceRebuildLayoutImmediate(_contentRoot);
        _scrollRect.horizontalNormalizedPosition = 1f;
        _scrollRect.verticalNormalizedPosition = 1f;
    }

    // 사전 배치된 브래킷 템플릿 프리팹에 현재 라운드 데이터를 바인딩
    private void RenderBracketFromTemplate(IReadOnlyList<List<TournamentMatchViewData>> allRounds, int currentRoundIndex)
    {
        if (_bracketTemplatePrefab == null)
        {
            _contentRoot.sizeDelta = Vector2.zero;
            return;
        }

        int roundCount = currentRoundIndex + 1;
        RectTransform templateRoot = Instantiate(_bracketTemplatePrefab, _contentRoot);

        MatchupContainerUI[] matchupItems = templateRoot.GetComponentsInChildren<MatchupContainerUI>(true);
        for (int i = 0; i < matchupItems.Length; i++)
        {
            MatchupContainerUI matchupUi = matchupItems[i];
            if (!TryParseBracketSlotName(matchupUi.name, "TournamentUI", out int roundOrder, out int matchupOrder))
            {
                matchupUi.gameObject.SetActive(false);
                continue;
            }

            if (roundOrder <= 0 || roundOrder > roundCount)
            {
                matchupUi.gameObject.SetActive(false);
                continue;
            }

            IReadOnlyList<TournamentMatchViewData> roundMatchups = allRounds[roundOrder - 1];
            int matchupIndex = matchupOrder - 1;
            if (matchupIndex < 0 || matchupIndex >= roundMatchups.Count)
            {
                matchupUi.gameObject.SetActive(false);
                continue;
            }

            TournamentMatchViewData matchup = roundMatchups[matchupIndex];
            matchupUi.gameObject.SetActive(true);
            matchupUi.SetData(matchup.UpTeam, matchup.DownTeam, matchup.IsHighlighted, matchup.IsResolved, matchup.IsUpTeamWinner);
        }

        RectTransform[] rects = templateRoot.GetComponentsInChildren<RectTransform>(true);
        for (int i = 0; i < rects.Length; i++)
        {
            RectTransform rect = rects[i];
            if (rect == templateRoot)
                continue;

            if (!TryParseBracketSlotName(rect.name, "TournamentConnector", out int fromRoundOrder, out int pairOrder))
                continue;

            bool isVisible = fromRoundOrder > 0 &&
                             fromRoundOrder < roundCount &&
                             pairOrder > 0 &&
                             pairOrder <= allRounds[fromRoundOrder].Count;
            rect.gameObject.SetActive(isVisible);
        }
        _contentRoot.sizeDelta = CalculateTemplateContentSize(templateRoot);
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

    // 활성 슬롯의 실제 Rect 경계를 기준으로 content 크기를 산출
    private static Vector2 CalculateTemplateContentSize(RectTransform templateRoot)
    {
        float maxX = 0f;
        float minY = 0f;
        bool hasActiveChild = false;
        Vector3[] corners = new Vector3[4];

        for (int i = 0; i < templateRoot.childCount; i++)
        {
            RectTransform child = templateRoot.GetChild(i) as RectTransform;
            if (child == null || !child.gameObject.activeInHierarchy)
                continue;

            child.GetWorldCorners(corners);
            for (int c = 0; c < corners.Length; c++)
            {
                Vector3 local = templateRoot.InverseTransformPoint(corners[c]);
                if (local.x > maxX)
                    maxX = local.x;
                if (local.y < minY)
                    minY = local.y;
            }

            hasActiveChild = true;
        }

        if (!hasActiveChild)
            return Vector2.zero;

        return new Vector2(maxX, -minY);
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

            myTeam = string.IsNullOrWhiteSpace(mySchoolName) ? matchup.UpTeam : mySchoolName;
            opponentTeam = matchup.DownTeam;
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

    private static bool TryParseBracketSlotName(string objectName, string prefix, out int roundOrder, out int indexOrder)
    {
        roundOrder = 0;
        indexOrder = 0;

        if (string.IsNullOrEmpty(objectName) || string.IsNullOrEmpty(prefix))
            return false;

        string startToken = $"{prefix} (R";
        if (!objectName.StartsWith(startToken, System.StringComparison.Ordinal))
            return false;

        int roundStart = startToken.Length;
        int dashIndex = objectName.IndexOf('-', roundStart);
        int endIndex = objectName.IndexOf(')', dashIndex + 1);

        if (dashIndex <= roundStart || endIndex <= dashIndex + 1)
            return false;

        if (!int.TryParse(objectName.Substring(roundStart, dashIndex - roundStart), out roundOrder))
            return false;

        if (!int.TryParse(objectName.Substring(dashIndex + 1, endIndex - dashIndex - 1), out indexOrder))
            return false;

        return true;
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

    public TournamentMatchViewData(string upTeam, string downTeam, bool isHighlighted, bool isResolved, bool isUpTeamWinner)
    {
        UpTeam = upTeam;
        DownTeam = downTeam;
        IsHighlighted = isHighlighted;
        IsResolved = isResolved;
        IsUpTeamWinner = isUpTeamWinner;
    }
}
