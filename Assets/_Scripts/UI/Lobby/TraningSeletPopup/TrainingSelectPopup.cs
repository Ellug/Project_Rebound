using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

// 훈련 선택 팝업 (페이지 전환 방식)
// ConfirmPopup(UIManager.ShowConfirm) 호출을 여기서 담당
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

        if (_txtPageTitle != null)
            _txtPageTitle.text = page.pageTitle;

        ClearButtons();
        SpawnButtons(page);
        UpdateBackButtonVisibility();
    }

    private void SpawnButtons(TrainingPageInfo page)
    {
        if (page == null || page.buttons == null) return;

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
        if (data == null) return;

        if (data.navigateToPageIndex >= 0)
        {
            ShowPage(data.navigateToPageIndex);
            return;
        }

        OpenConfirmPopup(data);
    }

    // 핵심: 학생선택 없는 케이스는 PrimaryAction에서 StartFlow를 직접 호출해야 함
    private void OpenConfirmPopup(TrainingButtonData data)
    {
        if (UIManager.Instance == null)
        {
            Debug.LogWarning("[TrainingSelectPopup] UIManager가 없습니다.");
            return;
        }

        ConfirmPopupRequest request = new ConfirmPopupRequest(
            title: data.trainingName,
            message: data.trainingDesc,
            primaryLabel: "훈련 시작",
            primaryAction: null,
            secondaryLabel: "취소",
            secondaryAction: null,
            previewSprite: data.previewSprite,
            subMessage: data.statModifierText
        );

        request.IsModal = true;
        request.AutoCloseOnPrimary = true;
        request.AutoCloseOnSecondary = true;

        request.RequiresStudentSelection = data.requiresStudentSelection;
        request.MaxSelectCount = data.maxSelectCount;

        if (request.RequiresStudentSelection)
        {
            request.OnStudentsSelected = (students) =>
            {
                StartTrainingFlowFromConfirm(data, students);
            };
        }
        else
        {
            request.PrimaryAction = () =>
            {
                List<Student> students = GetDefaultStudentsForNoSelect();
                StartTrainingFlowFromConfirm(data, students);
            };
        }

        UIManager.Instance.ShowConfirm(request);
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
            student.stamina += Mathf.RoundToInt(data.staminaDelta);

            // jump는 GrowthCommandRow에 없어서 미적용
            // student.jump += Mathf.RoundToInt(data.jumpDelta);

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
