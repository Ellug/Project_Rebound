using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

// 훈련 선택 팝업 (페이지 전환 방식)
public class TrainingSelectPopup : UIPopup
{
    private enum TrainingPageKind
    {
        Default = 0,
        Team = 1,
        Individual = 2
    }

    [Header("Page Config")]
    [SerializeField] private TrainingPageData _pageData;

    [Header("Training UI")]
    [SerializeField] private Image _imgPageTitle;
    [Tooltip("0: 훈련선택, 1: 단체훈련, 2: 개인훈련")]
    [SerializeField] private Sprite[] _pageTitleSprites;

    [Header("Button Sprite Config")]
    [Tooltip("훈련 선택(루트) 페이지에서 버튼 순서대로 번갈아 사용")]
    [SerializeField] private Sprite[] _selectPageButtonSprites;
    [SerializeField] private Sprite _trainingButtonSprite;

    [SerializeField] private Transform _buttonContainer;
    [SerializeField] private TrainingButtonItem _buttonPrefab2;

    [Header("Navigation")]
    [SerializeField] private Button _btnBack;

    private TrainingFlowController _trainingFlow;

    private int _currentPageIndex = 0;
    private readonly Stack<int> _pageHistory = new Stack<int>();
    private readonly List<TrainingButtonItem> _spawnedButtons = new List<TrainingButtonItem>();

    public event System.Action<string> OnTrainingSelected;

    private string _currentTrainingKey;

    public override void Init()
    {
        base.Init();

        if (_trainingFlow == null)
            _trainingFlow = FindFirstObjectByType<TrainingFlowController>();

        if (_btnBack != null)
        {
            _btnBack.onClick.RemoveAllListeners();
            _btnBack.onClick.AddListener(HandleBackButton);
        }

        ShowPage(0, pushHistory: false);
    }

    public override void Open()
    {
        if (!TryBuildPageDataFromCache()) return;
        base.Open();
    }

    private bool TryBuildPageDataFromCache()
    {
        if (_pageData == null)
        {
            Debug.LogWarning("[TrainingSelectPopup] _pageData SO가 연결되지 않았습니다.");
            return false;
        }

        GrowthCommandTableSO table = CachedSOData.GrowthCommandTable;
        if (!TrainingPageBuilder.Build(_pageData, table))
            return false;

        return true;
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
        TrainingPageKind pageKind = ResolvePageKind(page, pageIndex);

        UpdatePageTitleImage(pageKind);

        ClearButtons();
        SpawnButtons(page, pageKind);
        UpdateBackButtonVisibility();
    }

    private void UpdatePageTitleImage(TrainingPageKind pageKind)
    {
        Sprite pageTitleSprite = ResolvePageTitleSprite(pageKind);
        _imgPageTitle.sprite = pageTitleSprite;
        _imgPageTitle.enabled = pageTitleSprite != null;
    }

    private Sprite ResolvePageTitleSprite(TrainingPageKind pageKind)
    {
        int spriteIndex = (int)pageKind;
        return _pageTitleSprites[spriteIndex];
    }

    private TrainingPageKind ResolvePageKind(TrainingPageInfo page, int pageIndex)
    {
        string normalizedTitle = NormalizePageTitle(page.pageTitle);

        if (normalizedTitle.Contains("단체훈련"))
            return TrainingPageKind.Team;

        if (normalizedTitle.Contains("개인훈련"))
            return TrainingPageKind.Individual;

        TrainingPageKind inferredByButtons = InferPageKindByButtons(page);
        if (inferredByButtons != TrainingPageKind.Default)
            return inferredByButtons;

        if (pageIndex == 1)
            return TrainingPageKind.Team;

        if (pageIndex == 2)
            return TrainingPageKind.Individual;

        return TrainingPageKind.Default;
    }

    private static string NormalizePageTitle(string title)
    {
        return title.Replace(" ", string.Empty).Replace("\r", string.Empty).Replace("\n", string.Empty);
    }

    private static TrainingPageKind InferPageKindByButtons(TrainingPageInfo page)
    {
        if (page.buttons.Count == 0)
            return TrainingPageKind.Default;

        bool hasAction = false;
        bool hasTeamAction = false;
        bool hasIndividualAction = false;

        foreach (TrainingButtonData button in page.buttons)
        {
            if (button.navigateToPageIndex >= 0)
                continue;

            hasAction = true;

            if (button.requiresStudentSelection)
                hasIndividualAction = true;
            else
                hasTeamAction = true;
        }

        if (!hasAction)
            return TrainingPageKind.Default;

        if (hasTeamAction && !hasIndividualAction)
            return TrainingPageKind.Team;

        if (!hasTeamAction && hasIndividualAction)
            return TrainingPageKind.Individual;

        return TrainingPageKind.Default;
    }

    private void SpawnButtons(TrainingPageInfo page, TrainingPageKind pageKind)
    {
        for (int i = 0; i < page.buttons.Count; i++)
        {
            TrainingButtonData btnData = page.buttons[i];
            TrainingButtonItem item = Instantiate(_buttonPrefab2, _buttonContainer);
            item.gameObject.SetActive(true);

            TrainingButtonData captured = btnData;
            Sprite buttonSprite = ResolveButtonSprite(pageKind, i);
            bool centerName = pageKind == TrainingPageKind.Default;

            item.Setup(
                captured.trainingName,
                captured.statModifierText,
                () => HandleTrainingButton(captured),
                buttonSprite,
                centerName
            );

            _spawnedButtons.Add(item);
        }
    }

    private Sprite ResolveButtonSprite(TrainingPageKind pageKind, int buttonIndex)
    {
        switch (pageKind)
        {
            case TrainingPageKind.Team:
            case TrainingPageKind.Individual:
                return _trainingButtonSprite;
        }

        return ResolveDefaultButtonSprite(buttonIndex);
    }

    private Sprite ResolveDefaultButtonSprite(int buttonIndex)
    {
        return _selectPageButtonSprites[buttonIndex % _selectPageButtonSprites.Length];
    }

    private void HandleTrainingButton(TrainingButtonData data)
    {
        if (data == null) return;

        if (data.navigateToPageIndex >= 0)
        {
            ShowPage(data.navigateToPageIndex);
            return;
        }

        OpenConfirmPopup(data);
    }

    private void OpenConfirmPopup(TrainingButtonData data)
    {
        if (UIManager.Instance == null)
        {
            Debug.LogWarning("[TrainingSelectPopup] UIManager가 없습니다.");
            return;
        }

        UIPopupRequest request = new UIPopupRequest
        {
            Type = UIPopupRequest.PanelType.Default,
            Title = data.trainingName,
            Message = data.trainingDesc,
            SubMessage = data.statModifierText,
            PreviewSprite = data.previewSprite,

            ShowCancel = true,
            AutoCloseOnPrimary = true,
            AutoCloseOnCancel = true,

            PrimaryKind = UIPopupRequest.PrimaryButtonKind.StartTraining,
            PrimaryInteractable = true,

            RequiresStudentSelection = data.requiresStudentSelection,
            MaxSelectCount = data.maxSelectCount
        };

        if (request.RequiresStudentSelection)
        {
            request.OnStudentsSelected = (students) =>
            {
                StartTrainingFlowFromConfirm(data, students);
            };

            request.OnPrimary = null;
        }
        else
        {
            request.OnPrimary = () =>
            {
                List<Student> students = GetDefaultStudentsForNoSelect();
                StartTrainingFlowFromConfirm(data, students);
            };
        }

        request.OnCancel = () => { };

        UIManager.Instance.ShowPopup(request);
    }

    private void StartTrainingFlowFromConfirm(TrainingButtonData data, List<Student> students)
    {
        if (data == null)
            return;

        ClearPageHistory();
        ClearButtons();
        Close();

        string key = data.trainingKey;
        string name = data.trainingName;
        Sprite bgSprite = data.previewSprite;

        StartFlow(key, name, students, bgSprite, data);
    }

    private List<Student> GetDefaultStudentsForNoSelect()
    {
        if (StudentManager.Instance == null || StudentManager.Instance.Students == null)
            return new List<Student>();

        return new List<Student>(StudentManager.Instance.Students);
    }

    // FlowController 실행
    private void StartFlow(string key, string name, List<Student> students, Sprite bgSprite, TrainingButtonData data)
    {
        if (_trainingFlow == null)
            _trainingFlow = FindFirstObjectByType<TrainingFlowController>();

        if (_trainingFlow == null)
        {
            Debug.LogWarning("[TrainingSelectPopup] TrainingFlowController를 찾을 수 없습니다.");
            OnTrainingSelected?.Invoke(key);
            return;
        }

        _trainingFlow.OnFlowComplete -= HandleFlowComplete;
        _trainingFlow.OnFlowComplete += HandleFlowComplete;

        _currentTrainingKey = key;

        _trainingFlow.Execute(
            trainingKey: key,
            trainingName: name,
            students: students,
            applyEffect: (k, list) => ApplyCsvEffect(data, list),
            backgroundSprite: bgSprite
        );
    }

    private void ApplyCsvEffect(TrainingButtonData data, List<Student> students)
    {
        if (data == null || students == null)
            return;

        foreach (Student student in students)
        {
            if (student == null) continue;

            student.condition -= data.conditionDelta;
            student.condition = Mathf.Max(0, student.condition);

            student.shoot += Mathf.RoundToInt(data.shootDelta);
            student.speed += Mathf.RoundToInt(data.speedDelta);
            student.jump += Mathf.RoundToInt(data.jumpDelta);
            student.stamina += Mathf.RoundToInt(data.staminaDelta);
            student.mental += data.mentalDelta;

            if (StudentManager.Instance != null)
                StudentManager.Instance.NotifyStudentModified(student);
        }
    }

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
            if (item != null)
                Destroy(item.gameObject);
        }
        _spawnedButtons.Clear();
    }

    private void ClearPageHistory()
    {
        _pageHistory.Clear();
    }
}