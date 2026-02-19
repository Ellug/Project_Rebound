using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

// 훈련 선택 팝업 (페이지 전환 방식)
// FlowController 호출을 여기서 담당 (씬에 배치되어 항상 살아있으므로 콜백 안전)
public class TrainingSelectPopup : UIPopup
{
    [Header("Page Config")]
    [SerializeField] private TrainingPageData _pageData;

    [Header("Training UI")]
    [SerializeField] private TMP_Text _txtPageTitle;
    [SerializeField] private Transform _buttonContainer;
    [SerializeField] private TrainingButtonItem _buttonPrefab2;

    [Header("Navigation")]
    [SerializeField] private Button _btnBack;

    [Header("Confirm Popup")]
    [SerializeField] private TrainingConfirmPopup _confirmPopupPrefab;

    private TrainingFlowController _trainingFlow; // 런타임에 Find

    private int _currentPageIndex = 0;
    private readonly Stack<int> _pageHistory = new Stack<int>();
    private readonly List<TrainingButtonItem> _spawnedButtons = new List<TrainingButtonItem>();

    // 팀원 LobbyUI와 호환 (Action<string>)
    public event System.Action<string> OnTrainingSelected;

    public override void Init()
    {
        base.Init();

        // 씬에 배치된 FlowController 찾기
        if (_trainingFlow == null)
            _trainingFlow = FindFirstObjectByType<TrainingFlowController>();

        if (_btnBack != null)
        {
            _btnBack.onClick.RemoveAllListeners();
            _btnBack.onClick.AddListener(HandleBackButton);
        }

        ShowPage(0, pushHistory: false);
    }

    protected override void OnCloseButtonClicked()
    {
        ClearPageHistory();
        ClearButtons();
        Close();
    }

    public void ShowPage(int pageIndex, bool pushHistory = true)
    {
        if (_pageData == null || _pageData.pages == null
            || pageIndex < 0 || pageIndex >= _pageData.pages.Count)
        {
            Debug.LogError($"[TrainingSelectPopup] 유효하지 않은 페이지: {pageIndex}");
            return;
        }

        if (pushHistory)
            _pageHistory.Push(_currentPageIndex);

        _currentPageIndex = pageIndex;
        TrainingPageInfo page = _pageData.pages[pageIndex];

        if (_txtPageTitle != null)
            _txtPageTitle.text = page.pageTitle;

        ClearButtons();
        SpawnButtons(page);
        UpdateBackButtonVisibility();
    }

    private void SpawnButtons(TrainingPageInfo page)
    {
        foreach (TrainingButtonData btnData in page.buttons)
        {
            TrainingButtonItem item = Instantiate(_buttonPrefab2, _buttonContainer);
            item.gameObject.SetActive(true);

            TrainingButtonData captured = btnData;
            item.Setup(
                captured.trainingName,
                captured.statModifierText,
                () => HandleTrainingButton(captured)
            );

            _spawnedButtons.Add(item);
        }
    }

    private void HandleTrainingButton(TrainingButtonData data)
    {
        if (data.navigateToPageIndex >= 0)
        {
            ShowPage(data.navigateToPageIndex);
            return;
        }

        OpenConfirmPopup(data);
    }

    private void OpenConfirmPopup(TrainingButtonData data)
    {
        if (_confirmPopupPrefab == null)
        {
            Debug.Log($"[TrainingSelectPopup] 확인 팝업 없이 바로 실행: {data.trainingKey}");
            OnTrainingSelected?.Invoke(data.trainingKey);
            Close();
            return;
        }

        TrainingConfirmPopup confirm = Instantiate(_confirmPopupPrefab, transform.parent);
        confirm.Init();
        confirm.Setup(data);
        confirm.Open();

        // ConfirmPopup에서 학생 확정 → FlowController 실행
        confirm.OnTrainingConfirmed += (key, students) =>
        {
            // confirm은 자기가 Destroy함
            // 이 팝업(TrainingSelectPopup)을 닫고 게이지 시작
            ClearPageHistory();
            ClearButtons();
            Close();

            StartFlow(key, data.trainingName, students, data.previewSprite);
        };
    }

    // FlowController 실행 (이 오브젝트는 씬에 배치되어 항상 살아있으므로 콜백 안전)
    private void StartFlow(string key, string name, List<Student> students, Sprite bgSprite)
    {
        if (_trainingFlow == null)
            _trainingFlow = FindFirstObjectByType<TrainingFlowController>();

        if (_trainingFlow != null)
        {
            _trainingFlow.OnFlowComplete -= HandleFlowComplete;
            _trainingFlow.OnFlowComplete += HandleFlowComplete;

            _currentTrainingKey = key;
            _trainingFlow.Execute(key, name, students, null, bgSprite);
        }
        else
        {
            Debug.LogWarning("[TrainingSelectPopup] TrainingFlowController를 찾을 수 없습니다.");
            OnTrainingSelected?.Invoke(key);
        }
    }

    private string _currentTrainingKey;

    private void HandleFlowComplete()
    {
        if (_trainingFlow != null)
            _trainingFlow.OnFlowComplete -= HandleFlowComplete;

        Debug.Log($"[TrainingSelectPopup] 훈련 완료: {_currentTrainingKey}");
        OnTrainingSelected?.Invoke(_currentTrainingKey);
    }

    private void HandleBackButton()
    {
        if (_pageHistory.Count > 0)
        {
            int prevPage = _pageHistory.Pop();
            ShowPage(prevPage, pushHistory: false);
        }
        else
        {
            Close();
        }
    }

    private void UpdateBackButtonVisibility()
    {
        if (_btnBack != null)
            _btnBack.gameObject.SetActive(_pageHistory.Count > 0);
    }

    private void ClearButtons()
    {
        foreach (TrainingButtonItem item in _spawnedButtons)
        {
            if (item != null) Destroy(item.gameObject);
        }
        _spawnedButtons.Clear();
    }

    private void ClearPageHistory()
    {
        _pageHistory.Clear();
    }
}