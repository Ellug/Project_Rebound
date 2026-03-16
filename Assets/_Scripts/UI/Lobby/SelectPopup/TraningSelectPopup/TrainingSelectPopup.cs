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
    [SerializeField] private Button _btnTrainingClose;

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

        if (_btnTrainingClose != null)
        {
            _btnTrainingClose.onClick.RemoveAllListeners();
            _btnTrainingClose.onClick.AddListener(HandleCloseButton);
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

        GrowthCommandTableSO table = CachedSOData.Get<GrowthCommandTableSO>();
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

        var req = UIPopupRequest.Default(
            title: data.trainingName,
            message: data.trainingDesc,
            onPrimary: null,
            onCancel: () => { },
            subMessage: data.statModifierText,
            previewImageId: data.previewImageId,
            showCancel: true,
            primaryKind: UIPopupRequest.PrimaryButtonKind.StartTraining
        );

        req.AutoCloseOnPrimary = true;
        req.AutoCloseOnCancel = true;

        req.RequiresStudentSelection = data.requiresStudentSelection;
        req.MaxSelectCount = data.maxSelectCount;
        req.StudentCardPreviewDelta = new StudentCardPreviewDelta
        {
            condition = -data.conditionDelta,
            mental = data.mentalDelta,
            shoot = Mathf.RoundToInt(data.shootDelta),
            speed = Mathf.RoundToInt(data.speedDelta),
            jump = Mathf.RoundToInt(data.jumpDelta),
            stamina = Mathf.RoundToInt(data.staminaDelta)
        };

        if (req.RequiresStudentSelection)
        {
            req.OnStudentsSelected = (students) =>
            {
                StartTrainingFlowFromConfirm(data, students);
            };
            req.OnPrimary = () => { };
        }
        else
        {
            req.OnPrimary = () =>
            {
                List<Student> students = GetDefaultStudentsForNoSelect();
                StartTrainingFlowFromConfirm(data, students);
            };
        }

        UIManager.Instance.ShowPopup(req);
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
        string bgImageId = data.previewImageId;

        StartFlow(key, name, students, bgImageId, data);
    }


    private List<Student> GetDefaultStudentsForNoSelect()
    {
        if (StudentManager.Instance == null || StudentManager.Instance.Students == null)
            return new List<Student>();

        return new List<Student>(StudentManager.Instance.Students);
    }

    // FlowController 실행
    private void StartFlow(string key, string name, List<Student> students, string bgImageId, TrainingButtonData data)
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
            backgroundImageId: bgImageId
        );
    }

    private void ApplyCsvEffect(TrainingButtonData data, List<Student> students)
    {
        if (data == null || students == null)
            return;

        // 현재 시설 레벨
        int schoolLv = FacilitySystem.Instance.GetLevel("school");
        int gymLv = FacilitySystem.Instance.GetLevel("gym");
        int cafeteriaLv = FacilitySystem.Instance.GetLevel("cafeteria");
        int counselingLv = FacilitySystem.Instance.GetLevel("counselingcenter");

        // 필요 시설 레벨
        int requiredLv = data.requiredFacilityLv;

        // 시설 레벨 차이
        int schoolDiff = Mathf.Max(0, schoolLv - requiredLv);
        int gymDiff = Mathf.Max(0, gymLv - requiredLv);
        int cafeteriaDiff = Mathf.Max(0, cafeteriaLv - requiredLv);
        int counselingDiff = Mathf.Max(0, counselingLv - requiredLv);

        // 시설 보너스 %라 0.01f 곱함
        float schoolBonus = FacilitySystem.Instance.GetConditionDecayBonus() * 0.01f;
        float gymBonus = FacilitySystem.Instance.GetTrainingExpBonus() * 0.01f;
        float cafeteriaBonus = FacilitySystem.Instance.GetCafeteriaBonus() * 0.01f;
        float counselingBonus = FacilitySystem.Instance.GetMentalBonus() * 0.01f;

        // 임시 감독노드 나중에 감독노드 들어오면 삭제 예정
        float directorBonusTest = 0f;

        // 훈련시 보너스들
        float statBonus = (gymBonus * gymDiff) + directorBonusTest;
        float conditionBonus = (schoolBonus * schoolDiff) + (cafeteriaBonus * cafeteriaDiff) + directorBonusTest;
        float mentalBonus = (counselingBonus * counselingDiff) + (cafeteriaBonus * cafeteriaDiff) + directorBonusTest;

        foreach (Student student in students)
        {
            if (student == null) continue;

            // 컨디션
            if (data.conditionDelta >= 0)
            {
                student.condition -= data.conditionDelta;
            }
            else
            {
                int recover = -data.conditionDelta;
                recover += Mathf.RoundToInt(recover * conditionBonus);
                student.condition += recover;
            }
            student.condition = Student.ClampCondition(student.condition);

            // 스탯
            student.shoot += Mathf.RoundToInt(data.shootDelta + data.shootDelta * statBonus);
            student.speed += Mathf.RoundToInt(data.speedDelta + data.speedDelta * statBonus);
            student.jump += Mathf.RoundToInt(data.jumpDelta + data.jumpDelta * statBonus);
            student.stamina += Mathf.RoundToInt(data.staminaDelta + data.staminaDelta * statBonus);

            // 멘탈
            if (data.mentalDelta >= 0)
            {
                student.mental += Mathf.RoundToInt(data.mentalDelta + data.mentalDelta * mentalBonus);
            }
            else
            {
                student.mental += data.mentalDelta;
            }

            if (StudentManager.Instance != null)
                StudentManager.Instance.NotifyStudentModified(student);
        }

        if (SaveManager.Instance != null)
        {
            Debug.Log($"[TrainingSelectPopup] 훈련 결과 저장 | key={data.trainingKey} | students={students.Count}");
            SaveManager.Instance.SaveCurrent();
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

    private void HandleCloseButton()
    {
        ClearPageHistory();
        ClearButtons();
        Close();
    }
}
