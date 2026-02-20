using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class TournamentResultUI : MonoBehaviour
{
    [Header("Scene References")]
    [SerializeField] private GameObject _panelRoot;
    [SerializeField] private TMP_Text _titleText;
    [SerializeField] private Image _resultImage;
    [SerializeField] private TMP_Text _bodyText;
    [SerializeField] private Image _goldIconImage;
    [SerializeField] private TMP_Text _goldValueText;
    [SerializeField] private Image _fameIconImage;
    [SerializeField] private TMP_Text _fameValueText;

    [Header("Result Sprites")]
    [SerializeField] private Sprite _achievedImage;
    [SerializeField] private Sprite _failedImage;
    [SerializeField] private Sprite _goldIcon;
    [SerializeField] private Sprite _fameIcon;

    [Header("Text Templates")]
    [SerializeField] private string _achievedTitleFormat = "{PLACEMENT} 달성!";
    [SerializeField] private string _failedTitle = "탈락...";
    [TextArea(4, 10)]
    [SerializeField] private string _achievedContentTemplate =
        "{PLACEMENT} 달성 축하드립니다!\n다음 학기에도 팀을 이끌어 주세요!\n\n우승 학교: {CHAMPION}";
    [TextArea(6, 14)]
    [SerializeField] private string _failedContentTemplate =
        "결과는 냉혹했습니다.\n{PLACEMENT}에서 대회를 마무리했습니다.\n\n우승 학교: {CHAMPION}\n하지만 당신의 이름은 업계에 남았습니다.";

    [Header("Rewards")]
    [SerializeField] private int _achievedGold = 0;
    [SerializeField] private int _achievedFame = 0;
    [SerializeField] private int _failedGold = 0;
    [SerializeField] private int _failedFame = 0;

    private bool _isCurrentResultAchieved;

    void Awake()
    {
        Hide();
    }

    public void ShowResult(TournamentData tournamentResultData)
    {
        if (!tournamentResultData.HasPendingResult) return;

        string champion = string.IsNullOrWhiteSpace(tournamentResultData.PendingChampion)
            ? "미정"
            : tournamentResultData.PendingChampion;

        int reachedRoundTeamCount = tournamentResultData.PendingMySchoolReachedRoundTeamCount;
        string placementText = TournamentData.BuildPlacementText(reachedRoundTeamCount);
        _isCurrentResultAchieved = IsAchievedResult(reachedRoundTeamCount);

        if (_titleText != null)
        {
            _titleText.text = _isCurrentResultAchieved
                ? ApplyTemplate(_achievedTitleFormat, placementText, champion)
                : _failedTitle;
        }

        if (_bodyText != null)
        {
            _bodyText.text = _isCurrentResultAchieved
                ? ApplyTemplate(_achievedContentTemplate, placementText, champion)
                : ApplyTemplate(_failedContentTemplate, placementText, champion);
        }

        if (_resultImage != null)
        {
            _resultImage.sprite = _isCurrentResultAchieved ? _achievedImage : _failedImage;
            _resultImage.enabled = _resultImage.sprite != null;
        }

        if (_goldIconImage != null)
        {
            _goldIconImage.sprite = _goldIcon;
            _goldIconImage.enabled = _goldIconImage.sprite != null;
        }

        if (_fameIconImage != null)
        {
            _fameIconImage.sprite = _fameIcon;
            _fameIconImage.enabled = _fameIconImage.sprite != null;
        }

        if (_goldValueText != null)
            _goldValueText.text = (_isCurrentResultAchieved ? _achievedGold : _failedGold).ToString("N0");

        if (_fameValueText != null)
            _fameValueText.text = (_isCurrentResultAchieved ? _achievedFame : _failedFame).ToString("N0");

        _panelRoot.SetActive(true);
        _panelRoot.transform.SetAsLastSibling();
    }

    public void Hide()
    {
        if (_panelRoot != null)
            _panelRoot.SetActive(false);
    }

    // 확인 버튼 : 인스펙터에 직접 연결
    public void OnClickConfirm()
    {
        // TODO 닫기 로직 외에 더 있나?
        // 패배했을 때는 게임오버라던가 추가 로직 있을듯?
        Hide();
    }

    private static bool IsAchievedResult(int reachedRoundTeamCount)
    {
        return reachedRoundTeamCount > 0 && reachedRoundTeamCount <= 2;
    }

    private static string ApplyTemplate(string template, string placement, string champion)
    {
        if (string.IsNullOrEmpty(template))
            return string.Empty;

        return template
            .Replace("{PLACEMENT}", placement)
            .Replace("{CHAMPION}", champion); // 우승 고등학교도 일단은 전달함
    }
}
