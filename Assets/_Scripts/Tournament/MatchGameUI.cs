using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// 경기 화면 전체 View: 스코어보드·진행 표시바·로그 패널·결과 패널을 통합 관리
public class MatchGameUI : MonoBehaviour
{
    [Header("Match Panel")]
    [SerializeField] private GameObject _matchGamePanel;
    [SerializeField] private TMP_Text _leftSchoolText;
    [SerializeField] private TMP_Text _rightSchoolText;

    [Header("Match Flow UI")]
    [SerializeField] private TMP_Text _currentMatchStateText;
    [SerializeField] private TMP_Text _leftScoreValueText;
    [SerializeField] private TMP_Text _rightScoreValueText;
    [SerializeField] private ScrollRect _matchLogScrollRect;
    [SerializeField] private RectTransform _matchLogContentRoot;
    [SerializeField] private TMP_Text _matchLogText;
    [SerializeField] private Image[] _quarterProgressNodes = new Image[0];
    [SerializeField] private Image[] _quarterProgressLines = new Image[0];
    [SerializeField] private Color _progressCompletedColor = new(0.17f, 0.17f, 0.17f, 1f);
    [SerializeField] private Color _progressCurrentColor = new(0f, 0f, 0f, 1f);
    [SerializeField] private Color _progressPendingColor = new(0.74f, 0.74f, 0.74f, 1f);

    [Header("Match Context Image")]
    [SerializeField] private Image _matchContextImage; // 경기 상황 이미지 (쿼터/공방/하프타임 상황별 교체)

    [Header("Match Result Panel")]
    [SerializeField] private GameObject _matchResultPanel;
    [SerializeField] private GameObject _matchResultWinHeader;
    [SerializeField] private GameObject _matchResultLoseHeader;
    [SerializeField] private Image _matchResultImage; // 승/패 결과 이미지

    private readonly List<string> _matchLogLines = new();
    private string _currentMatchImageId;   // 현재 로드된 경기 상황 이미지 ID
    private string _currentResultImageId;  // 현재 로드된 결과 패널 이미지 ID

    // 경기 시작 시 팀명·점수·진행바·로그를 초기 상태로 세팅
    public void PrepareMatchGameUi(string leftSchoolName, string rightSchoolName, IReadOnlyList<string> progressStages)
    {
        HideMatchResultPanel();
        ShowMatchGamePanel(leftSchoolName, rightSchoolName);
        SetMatchScore(0, 0);
        SetQuarterProgress(progressStages, 0);
        SetCurrentMatchState(progressStages != null && progressStages.Count > 0 ? progressStages[0] : "1쿼터");
        ClearMatchLogs();

        // 경기 진입 기본 이미지
        SetMatchContextImage("EventGame00_image01");
    }

    // 현재 진행 단계 텍스트 업데이트 (빈 값이면 "-" 표시)
    public void SetCurrentMatchState(string stateText)
    {
        _currentMatchStateText.text = string.IsNullOrWhiteSpace(stateText) ? "-" : stateText;
    }

    // 스코어보드 좌·우 점수를 "00" 포맷으로 갱신
    public void SetMatchScore(int leftScore, int rightScore)
    {
        _leftScoreValueText.text = Mathf.Max(0, leftScore).ToString("00");
        _rightScoreValueText.text = Mathf.Max(0, rightScore).ToString("00");
    }

    // 진행 표시바의 노드·라인 색상을 완료/현재/대기 상태에 따라 갱신
    public void SetQuarterProgress(IReadOnlyList<string> stageLabels, int activeStageIndex)
    {
        int stageCount = stageLabels != null ? stageLabels.Count : 0;
        if (stageCount <= 0)
            return;

        int currentIndex = Mathf.Clamp(activeStageIndex, 0, stageCount - 1);
        int visibleNodeCount = Mathf.Min(stageCount, _quarterProgressNodes.Length);

        for (int i = 0; i < _quarterProgressNodes.Length; i++)
        {
            bool isVisible = i < visibleNodeCount;
            _quarterProgressNodes[i].gameObject.SetActive(isVisible);

            if (!isVisible)
                continue;

            if (i < currentIndex)
                _quarterProgressNodes[i].color = _progressCompletedColor;
            else if (i == currentIndex)
                _quarterProgressNodes[i].color = _progressCurrentColor;
            else
                _quarterProgressNodes[i].color = _progressPendingColor;
        }

        // 노드 사이 연결선: 노드 수 - 1개, 완료 구간만 진한 색
        int visibleLineCount = Mathf.Min(Mathf.Max(visibleNodeCount - 1, 0), _quarterProgressLines.Length);
        for (int i = 0; i < _quarterProgressLines.Length; i++)
        {
            bool isVisible = i < visibleLineCount;
            _quarterProgressLines[i].gameObject.SetActive(isVisible);
            if (!isVisible)
                continue;

            _quarterProgressLines[i].color = i < currentIndex ? _progressCompletedColor : _progressPendingColor;
        }
    }

    // 로그 한 줄 추가 후 스크롤을 최하단으로 이동
    public void AppendMatchLog(string logLine)
    {
        if (logLine == null)
            return;

        _matchLogLines.Add(logLine);
        _matchLogText.text = string.Join("\n", _matchLogLines);
        RebuildMatchLogLayout();
    }

    // 로그 전체 초기화
    public void ClearMatchLogs()
    {
        _matchLogLines.Clear();
        _matchLogText.text = string.Empty;
        RebuildMatchLogLayout();
    }

    // 팀명을 설정하고 경기 패널을 표시
    public void ShowMatchGamePanel(string leftSchoolName, string rightSchoolName)
    {
        _leftSchoolText.text = leftSchoolName;
        _rightSchoolText.text = rightSchoolName;
        _matchGamePanel.SetActive(true);
    }

    public void HideMatchGamePanel()
    {
        _matchGamePanel.SetActive(false);
    }

    // 승패 이미지를 설정하고 결과 패널을 표시
    public void ShowMatchResultPanel(bool didWin)
    {
        // 경기 중 상황 이미지는 여기서 정리
        ReleaseMatchContextImage();

        string resultImageId = didWin
            ? "EventGame00_img_victory"
            : "EventGame00_img_defeat";

        // Header 승/패 활성화 복구
        if (_matchResultWinHeader != null)
            _matchResultWinHeader.SetActive(didWin);

        if (_matchResultLoseHeader != null)
            _matchResultLoseHeader.SetActive(!didWin);

        // 기존 결과 이미지 해제
        ReleaseMatchResultImage();

        if (_matchResultImage == null)
        {
            _matchResultPanel.SetActive(true);
            return;
        }

        AddressableImageManager.Instance.LoadSprite(resultImageId, sprite =>
        {
            if (_matchResultImage == null)
                return;

            if (sprite != null)
            {
                _currentResultImageId = resultImageId;
                _matchResultImage.sprite = sprite;
                _matchResultImage.gameObject.SetActive(true);
            }

            _matchResultPanel.SetActive(true);
        });
    }

    public void HideMatchResultPanel()
    {
        ReleaseMatchResultImage();

        if (_matchResultWinHeader != null)
            _matchResultWinHeader.SetActive(false);

        if (_matchResultLoseHeader != null)
            _matchResultLoseHeader.SetActive(false);

        _matchResultPanel.SetActive(false);
    }

    // 외부 UI 이벤트(예: 하프타임 선택 패널 오픈)에서 로그 스크롤 하단 정렬 용도
    public void ScrollMatchLogToBottom()
    {
        MoveMatchLogToBottom();
    }

    // 경기 상황 이미지를 Addressable로 교체
    public void SetMatchContextImage(string imageId)
    {
        if (_currentMatchImageId == imageId)
            return;

        // 기존 이미지 해제
        if (!string.IsNullOrEmpty(_currentMatchImageId))
            AddressableImageManager.Instance.ReleaseSprite(_currentMatchImageId);

        _currentMatchImageId = imageId;

        if (string.IsNullOrEmpty(imageId) || _matchContextImage == null)
            return;

        AddressableImageManager.Instance.LoadSprite(imageId, sprite =>
        {
            if (_matchContextImage == null)
                return;

            if (sprite != null)
            {
                _matchContextImage.sprite = sprite;
                _matchContextImage.gameObject.SetActive(true);
            }
        });
    }

    // 현재 로드된 상황 이미지 해제
    public void ReleaseMatchContextImage()
    {
        if (!string.IsNullOrEmpty(_currentMatchImageId))
        {
            AddressableImageManager.Instance.ReleaseSprite(_currentMatchImageId);
            _currentMatchImageId = null;
        }

        if (_matchContextImage != null)
        {
            _matchContextImage.sprite = null;
            _matchContextImage.gameObject.SetActive(false);
        }
    }

    // 현재 로드된 결과 이미지 해제
    private void ReleaseMatchResultImage()
    {
        if (!string.IsNullOrEmpty(_currentResultImageId))
        {
            AddressableImageManager.Instance.ReleaseSprite(_currentResultImageId);
            _currentResultImageId = null;
        }

        if (_matchResultImage != null)
        {
            _matchResultImage.sprite = null;
            _matchResultImage.gameObject.SetActive(false);
        }
    }

    // Canvas 업데이트 후 스크롤 위치를 강제로 최하단(0)으로 고정
    private void MoveMatchLogToBottom()
    {
        Canvas.ForceUpdateCanvases();
        _matchLogScrollRect.verticalNormalizedPosition = 0f;
    }

    // 로그 텍스트 추가 후 ContentSizeFitter 재계산 + 스크롤을 항상 하단으로 이동
    private void RebuildMatchLogLayout()
    {
        LayoutRebuilder.ForceRebuildLayoutImmediate(_matchLogText.rectTransform);
        LayoutRebuilder.ForceRebuildLayoutImmediate(_matchLogContentRoot);
        MoveMatchLogToBottom();
    }
}