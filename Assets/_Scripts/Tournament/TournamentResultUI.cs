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

    [Header("Reward IDs")]
    [SerializeField] private int _rank1RewardId = 201;  // 1위
    [SerializeField] private int _rank2RewardId = 202;  // 2위
    [SerializeField] private int _rank3RewardId = 203;  // 3위
    [SerializeField] private int _rank4RewardId = 204;  // 4강
    [SerializeField] private int _failedRewardId = 205;  // 탈락

    [Header("Scroll Layout")]
    [SerializeField] private ScrollRect _scrollRect;
    [SerializeField] private RectTransform _scrollContent;   // Content RectTransform
    [SerializeField] private RectTransform _rewardRow;       // RewardRow RectTransform

    private bool _isCurrentResultAchieved;
    private int _currentRewardId;

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
        _isCurrentResultAchieved = IsAchievedResult(reachedRoundTeamCount);
        _currentRewardId = ResolveRewardId(reachedRoundTeamCount);

        RewardPopupRow row = GetRewardRow(_currentRewardId);

        if (_titleText != null)
            _titleText.text = row != null ? row.titleText : string.Empty;

        if (_bodyText != null)
        {
            string body = row != null ? row.desc : string.Empty;
            // {CHAMPION} 치환 지원
            _bodyText.text = body.Replace("{CHAMPION}", champion);
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
            _goldValueText.text = (row != null ? row.money : 0).ToString("N0");

        if (_fameValueText != null)
            _fameValueText.text = (row != null ? row.fame : 0).ToString("N0");

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
        // CachedSOData에서 보상 row를 조회해 MoneyManager에 지급
        RewardPopupRow row = GetRewardRow(_currentRewardId);
        if (row != null && MoneyManager.Instance != null)
            MoneyManager.Instance.ApplyReward(row.money, row.fame);

        // TODO: 다음 시즌 진입 등 후처리
        Hide();
    }

    // 패배(탈락) 확인 -> 타이틀로 이동
    // GameManager.OnSceneLoaded에서 TitleScene 감지 시 CleanupManagers() -> ClearFlowRuntimeState() 자동 호출해서 초기화
    private void OnConfirmFailed()
    {
        // CachedSOData에서 보상 row를 조회해 MoneyManager에 지급
        RewardPopupRow row = GetRewardRow(_currentRewardId);
        if (row != null && MoneyManager.Instance != null)
            MoneyManager.Instance.ApplyReward(row.money, row.fame);

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

    // reachedRoundTeamCount 기준으로 보상 id 결정
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

    // CachedSOData에서 RewardPopupTableSO를 꺼내 id로 row 조회
    private static RewardPopupRow GetRewardRow(int id)
    {
        if (!CachedSOData.TryGet<RewardPopupTableSO>(out var table))
        {
            Debug.LogWarning("[TournamentResultUI] RewardPopupTableSO가 CachedSOData에 등록되지 않았습니다.");
            return null;
        }
        foreach (var row in table.Rows)
            if (row.id == id) return row;

        Debug.LogWarning($"[TournamentResultUI] id {id} 에 해당하는 보상 데이터를 찾을 수 없습니다.");
        return null;
    }

    private static bool IsAchievedResult(int reachedRoundTeamCount)
    {
        return reachedRoundTeamCount > 0 && reachedRoundTeamCount <= 4;
    }
}