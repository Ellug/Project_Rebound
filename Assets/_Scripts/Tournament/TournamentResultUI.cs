using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class TournamentResultUI : MonoBehaviour
{
    private const string Rank1ImageId = "EventGamePopup01_img_1";
    private const string Rank2ImageId = "EventGamePopup01_img_2";
    private const string Rank3ImageId = "EventGamePopup01_img_3";
    private const string Rank4ImageId = "EventGamePopup01_img_4";
    private const string FailedImageId = "EventGamePopup02_img";
    private const string LobbySceneName = "Lobby";
    private const int LobbyBgmClipId = 102;

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

    [Header("Reward IDs")]
    [SerializeField] private int _rank1RewardId = 201;  // 1위
    [SerializeField] private int _rank2RewardId = 202;  // 2위
    [SerializeField] private int _rank3RewardId = 203;  // 3위
    [SerializeField] private int _rank4RewardId = 204;  // 4강
    [SerializeField] private int _failedRewardId = 205; // 탈락

    [Header("Scroll Layout")]
    [SerializeField] private ScrollRect _scrollRect;
    [SerializeField] private RectTransform _scrollContent; // Content RectTransform
    [SerializeField] private RectTransform _rewardRow; // RewardRow RectTransform

    private bool _isCurrentResultAchieved;
    private int _currentRewardId;
    private bool _isFriendlyResultMode;
    private int _friendlyRewardMoney;
    private int _friendlyRewardFame;
    private string _currentResultImageId;

    void Awake()
    {
        Hide();
    }

    public void ShowResult(TournamentData tournamentResultData)
    {
        if (!tournamentResultData.HasPendingResult) return;

        _isFriendlyResultMode = false;
        _friendlyRewardMoney = 0;
        _friendlyRewardFame = 0;

        int reachedRoundTeamCount = tournamentResultData.PendingMySchoolReachedRoundTeamCount;
        _isCurrentResultAchieved = IsAchievedResult(reachedRoundTeamCount);
        _currentRewardId = ResolveRewardId(reachedRoundTeamCount);

        if (_isCurrentResultAchieved)
            SoundManager.Instance.PlayBGM(105);
        else
            SoundManager.Instance.PlayBGM(104);

        RewardPopupRow row = GetRewardRow(_currentRewardId);
        _titleText.text = row.titleText;
        _bodyText.text = row.desc;

        SetTournamentResultImage(ResolveTournamentResultImageId(reachedRoundTeamCount));

        _goldIconImage.sprite = _goldIcon;
        _goldIconImage.enabled = _goldIconImage.sprite != null;

        _fameIconImage.sprite = _fameIcon;
        _fameIconImage.enabled = _fameIconImage.sprite != null;

        _goldValueText.text = row.money.ToString("N0");

        int fameValue = row.fame;
        if (MoneyManager.Instance != null)
            fameValue = MoneyManager.Instance.GetAdjustedReputationAmount(fameValue);

        _fameValueText.text = fameValue.ToString("N0");

        _panelRoot.SetActive(true);
        _panelRoot.transform.SetAsLastSibling();

        // 텍스트 반영 후 스크롤 레이아웃을 맞춘다.
        AdjustScrollLayout();
    }

    public void ShowFriendlyResult(bool didWin, string opponentName, int winRewardId)
    {
        _isFriendlyResultMode = true;
        _friendlyRewardMoney = 0;
        _friendlyRewardFame = 0;

        string normalizedOpponent = string.IsNullOrWhiteSpace(opponentName) ? "상대 학교" : opponentName.Trim();

        if (didWin)
        {
            RewardPopupRow row = GetRewardRow(winRewardId);
            _friendlyRewardMoney = row.money;
            _friendlyRewardFame = row.fame;
            _isCurrentResultAchieved = true;
            _titleText.text = row.titleText;
            _bodyText.text = row.desc;
        }
        else
        {
            _isCurrentResultAchieved = false;
            _titleText.text = "친선전 패배...";
            _bodyText.text = $"{normalizedOpponent}와의 친선전에서 패배했습니다.\n다음 경기를 준비하세요.";
        }

        ReleaseTournamentResultImage();

        _resultImage.sprite = _isCurrentResultAchieved ? _achievedImage : _failedImage;
        _resultImage.enabled = _resultImage.sprite != null;

        _goldIconImage.sprite = _goldIcon;
        _goldIconImage.enabled = _goldIconImage.sprite != null;

        _fameIconImage.sprite = _fameIcon;
        _fameIconImage.enabled = _fameIconImage.sprite != null;

        _goldValueText.text = _friendlyRewardMoney.ToString("N0");

        int fameValue = _friendlyRewardFame;
        if (MoneyManager.Instance != null)
            fameValue = MoneyManager.Instance.GetAdjustedReputationAmount(fameValue);

        _fameValueText.text = fameValue.ToString("N0");

        _panelRoot.SetActive(true);
        _panelRoot.transform.SetAsLastSibling();
        AdjustScrollLayout();
    }

    public void Hide()
    {
        ReleaseTournamentResultImage();
        _panelRoot.SetActive(false);
    }

    // 확인 버튼 : 인스펙터에 직접 연결
    public void OnClickConfirm()
    {
        if (_isFriendlyResultMode)
        {
            OnConfirmFriendly();
            return;
        }

        if (_isCurrentResultAchieved)
            OnConfirmAchieved();
        else
            OnConfirmFailed();
    }

    private void OnConfirmFriendly()
    {
        MoneyManager.Instance.ApplyReward(_friendlyRewardMoney, _friendlyRewardFame);

        _friendlyRewardMoney = 0;
        _friendlyRewardFame = 0;
        _isFriendlyResultMode = false;
        Hide();
        RestoreLobbyBgmIfLobby();
    }

    // 승리(입상) 확인
    private void OnConfirmAchieved()
    {
        // 보상 row를 조회해 지급한다.
        RewardPopupRow row = GetRewardRow(_currentRewardId);
        MoneyManager.Instance.ApplyReward(row.money, row.fame);
        Hide();
        RestoreLobbyBgmIfLobby();
    }

    // 패배(탈락) 확인 -> 타이틀로 이동
    private void OnConfirmFailed()
    {
        // 보상 row를 조회해 지급한다.
        RewardPopupRow row = GetRewardRow(_currentRewardId);
        MoneyManager.Instance.ApplyReward(row.money, row.fame);
        Hide();

        if (SaveManager.Instance != null)
            SaveManager.Instance.MarkCurrentRunForDeleteOnTitle();

        UnityEngine.SceneManagement.SceneManager.LoadScene("Title");
    }

    // BodyText 길이에 맞춰 RewardRow를 스크롤 하단에 붙인다.
    private void AdjustScrollLayout()
    {
        if (_scrollRect == null || _scrollContent == null || _rewardRow == null || _bodyText == null)
            return;

        Canvas.ForceUpdateCanvases();

        float viewportHeight = _scrollRect.viewport.rect.height;
        float rewardHeight = _rewardRow.rect.height;
        float requiredBodyHeight = viewportHeight - rewardHeight;

        LayoutElement bodyLayoutElement = _bodyText.GetComponent<LayoutElement>();
        if (bodyLayoutElement == null)
            bodyLayoutElement = _bodyText.gameObject.AddComponent<LayoutElement>();

        float actualBodyHeight = _bodyText.preferredHeight;

        if (actualBodyHeight < requiredBodyHeight)
        {
            bodyLayoutElement.minHeight = requiredBodyHeight;
            bodyLayoutElement.preferredHeight = requiredBodyHeight;
        }
        else
        {
            bodyLayoutElement.minHeight = -1;
            bodyLayoutElement.preferredHeight = -1;
        }

        LayoutRebuilder.ForceRebuildLayoutImmediate(_scrollContent);
    }

    // reachedRoundTeamCount 기준으로 보상 id를 반환한다.
    private int ResolveRewardId(int reachedRoundTeamCount)
    {
        switch (reachedRoundTeamCount)
        {
            case 1: return _rank1RewardId;
            case 2: return _rank2RewardId;
            case 3: return _rank3RewardId;
            case 4: return _rank4RewardId;
            default: return _failedRewardId;
        }
    }

    // RewardPopupTableSO에서 id로 보상 row를 찾는다.
    private static RewardPopupRow GetRewardRow(int id)
    {
        RewardPopupTableSO table = CachedSOData.Get<RewardPopupTableSO>();
        foreach (RewardPopupRow row in table.Rows)
            if (row.id == id) return row;

        throw new InvalidOperationException($"[TournamentResultUI] id {id} 보상 데이터를 찾을 수 없습니다.");
    }

    private static bool IsAchievedResult(int reachedRoundTeamCount)
    {
        return reachedRoundTeamCount > 0 && reachedRoundTeamCount <= 4;
    }

    private static string ResolveTournamentResultImageId(int reachedRoundTeamCount)
    {
        switch (reachedRoundTeamCount)
        {
            case 1: return Rank1ImageId;
            case 2: return Rank2ImageId;
            case 3: return Rank3ImageId;
            case 4: return Rank4ImageId;
            default: return FailedImageId;
        }
    }
    private void SetTournamentResultImage(string imageId)
    {
        ReleaseTournamentResultImage();

        if (_resultImage == null || string.IsNullOrEmpty(imageId))
            return;

        AddressableImageManager.Instance.LoadSprite(imageId, sprite =>
        {
            if (_resultImage == null)
                return;

            if (sprite != null)
            {
                _currentResultImageId = imageId;
                _resultImage.sprite = sprite;
                _resultImage.enabled = true;
            }
            else
            {
                _resultImage.sprite = null;
                _resultImage.enabled = false;
            }
        });
    }

    private void ReleaseTournamentResultImage()
    {
        if (!string.IsNullOrEmpty(_currentResultImageId))
        {
            AddressableImageManager.Instance.ReleaseSprite(_currentResultImageId);
            _currentResultImageId = null;
        }

        if (_resultImage != null && _isFriendlyResultMode == false)
        {
            _resultImage.sprite = null;
            _resultImage.enabled = false;
        }
    }

    private static void RestoreLobbyBgmIfLobby()
    {
        if (UnityEngine.SceneManagement.SceneManager.GetActiveScene().name != LobbySceneName) return;
        SoundManager.Instance.PlayBGM(LobbyBgmClipId);
    }
}
