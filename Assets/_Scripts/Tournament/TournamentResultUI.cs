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

    [Header("Scroll Layout")]
    [SerializeField] private ScrollRect _scrollRect;
    [SerializeField] private RectTransform _scrollContent;   // Content RectTransform
    [SerializeField] private RectTransform _rewardRow;       // RewardRow RectTransform

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

        // 텍스트 세팅 완료 후 스크롤 레이아웃 조정
        // BodyText 길이에 따라 RewardRow가 항상 스크롤 최하단에 위치하도록 보정
        AdjustScrollLayout();
    }

    public void Hide()
    {
        if (_panelRoot != null)
            _panelRoot.SetActive(false);
    }

    // 확인 버튼 : 인스펙터에 직접 연결
    public void OnClickConfirm()
    {
        if (_isCurrentResultAchieved)
            OnConfirmAchieved();
        else
            OnConfirmFailed();
    }

    // 승리(입상) 확인
    private void OnConfirmAchieved()
    {
        // TODO: 승리 보상 지급, 다음 시즌 진입 등 후처리
        Hide();
    }

    // 패배(탈락) 확인 -> 타이틀로 이동
    // GameManager.OnSceneLoaded에서 TitleScene 감지 시 CleanupManagers() -> ClearFlowRuntimeState() 자동 호출해서 초기화
    private void OnConfirmFailed()
    {
        // TODO: 패배 연출?? 보존 재화 계산?
        Hide();

        if (SaveManager.Instance != null)
        {
            SaveManager.Instance.MarkCurrentRunForDeleteOnTitle();
        }
        UnityEngine.SceneManagement.SceneManager.LoadScene("Title");
    }


    // BodyText가 짧을 때 RewardRow가 스크롤 바닥에 붙도록 BodyText의 minHeight를 동적으로 조정
    // BodyText가 충분히 길면 자연스럽게 스크롤이 생기고 RewardRow는 스크롤 끝에 위치

    private void AdjustScrollLayout()
    {
        if (_scrollRect == null || _scrollContent == null || _rewardRow == null || _bodyText == null)
            return;

        Canvas.ForceUpdateCanvases();

        float viewportHeight = _scrollRect.viewport.rect.height;
        float rewardHeight = _rewardRow.rect.height;

        // Viewport를 꽉 채우려면 BodyText가 최소한 이 높이 이상이어야 함
        float requiredBodyHeight = viewportHeight - rewardHeight;

        LayoutElement bodyLayoutElement = _bodyText.GetComponent<LayoutElement>();
        if (bodyLayoutElement == null)
            bodyLayoutElement = _bodyText.gameObject.AddComponent<LayoutElement>();

        float actualBodyHeight = _bodyText.preferredHeight;

        if (actualBodyHeight < requiredBodyHeight)
        {
            // 텍스트가 짧은 경우: BodyText 영역을 늘려 RewardRow를 바닥으로 밀어냄
            bodyLayoutElement.minHeight = requiredBodyHeight;
            bodyLayoutElement.preferredHeight = requiredBodyHeight;
        }
        else
        {
            // 텍스트가 긴 경우: 텍스트 크기 그대로 사용, 스크롤이 자동으로 생김
            bodyLayoutElement.minHeight = -1;
            bodyLayoutElement.preferredHeight = -1;
        }

        // LayoutElement 변경 사항을 Content에 즉시 반영
        LayoutRebuilder.ForceRebuildLayoutImmediate(_scrollContent);
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

