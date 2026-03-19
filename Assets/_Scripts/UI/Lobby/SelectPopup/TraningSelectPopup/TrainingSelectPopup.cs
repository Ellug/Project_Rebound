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

            var preview = TrainingStatsView(captured);
            string statText = StatTextChange(preview);

            item.Setup(
                captured.trainingName,
                statText,
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

        var preview = TrainingStatsView(data);
        string conditionText = ConditionTextChange(preview);

        var req = UIPopupRequest.Default(
            title: data.trainingName,
            message: data.trainingDesc,
            onPrimary: null,
            onCancel: () => { },
            subMessage: conditionText,
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
            condition = preview.condition,
            treatStatFieldsAsExp = true, // 나중에 복구 위해서 카드 델타 stat / exp 불로 관리
            mental = preview.mental,
            shoot = preview.shoot,
            speed = preview.speed,
            jump = preview.jump,
            stamina = preview.stamina
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
        string bgImageId = data.backgroundImageId;  // ProgressUI 배경 이미지

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
            backgroundImageId: bgImageId,
            resultImageId: data.resultImageId
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
        int cafeteriaDiff = Mathf.Max(0, cafeteriaLv - requiredLv);
        int counselingDiff = Mathf.Max(0, counselingLv - requiredLv);

        // 시설 보너스 %라 0.01f 곱함
        float schoolBonus = FacilitySystem.Instance.GetConditionDecayBonus() * 0.01f;
        float gymBonus = FacilitySystem.Instance.GetTrainingExpBonus() * 0.01f;
        float cafeteriaBonus = FacilitySystem.Instance.GetCafeteriaBonus() * 0.01f;
        float counselingBonus = FacilitySystem.Instance.GetMentalBonus() * 0.01f;

        // 훈련시 보너스들
        float conditionBonus = (schoolBonus * schoolDiff) + (cafeteriaBonus * cafeteriaDiff);
        float mentalBonus = (counselingBonus * counselingDiff) + (cafeteriaBonus * cafeteriaDiff);
        float nodeTrainingBonus = 0f;

        if (HeadCoachManager.Instance != null && HeadCoachManager.Instance.IsInitialized)
        {
            // 현재 테이블에는 키가 없으면 0%로 동작
            nodeTrainingBonus = HeadCoachManager.Instance.GetStatBonusValue("Training_Exp_Bonus") * 0.01f;
        }

        // 감독 노드 훈련 컨디션 소모 감소 보너스 적용
        float nodeConditionBonus = 0f;
        if (HeadCoachManager.Instance != null && HeadCoachManager.Instance.IsInitialized)
        {
            string trainingKey = data.trainingKey;

            // 슈팅 드릴 (index: 1201)
            if (trainingKey == "cmd_1201")
            {
                nodeConditionBonus += 
                    HeadCoachManager.Instance.GetStatBonusValue("Condition_Drain_ShootingDrill") * 0.01f;
                
                int previewCost =
                    Mathf.Max(
                        0, Mathf.FloorToInt(
                            data.conditionDelta *
                            (1f + nodeConditionBonus)
                            )
                        );

                Debug.Log($"[TrainingSelectPopup] 슈팅 드릴 컨디션 소모 감소 : 기본 소모: {data.conditionDelta}, 실제 소모: {previewCost}");
            }

            // 디펜스 워크 (index: 1203)
            if (trainingKey == "cmd_1203")
            {
                nodeConditionBonus +=
                    HeadCoachManager.Instance.GetStatBonusValue("Condition_Drain_DefenceWork") * 0.01f;
                
                int previewCost =
                    Mathf.Max(
                        0, Mathf.FloorToInt(
                            data.conditionDelta *
                            (1f + nodeConditionBonus)
                            )
                        );

                Debug.Log($"[TrainingSelectPopup] 디펜스 워크 컨디션 소모 감소 : 기본 소모: {data.conditionDelta}, 실제 소모: {previewCost}");
            }

            // 단체 훈련 계열 (index: 1101, 1102, 1103)
            if (trainingKey == "cmd_1101" || trainingKey == "cmd_1102" || trainingKey == "cmd_1103")
            {
                nodeConditionBonus +=
                    HeadCoachManager.Instance.GetStatBonusValue("Condition_Drain_TeamPractice") * 0.01f;

                int previewCost = 
                    Mathf.Max(
                        0, Mathf.FloorToInt(
                            data.conditionDelta *
                            (1f + nodeConditionBonus)
                            )
                    );

                Debug.Log($"[TrainingSelectPopup] 단체 훈련 컨디션 소모 감소 : 기본 소모: {data.conditionDelta}, 실제 소모: {previewCost}");
            }
        }

        foreach (Student student in students)
        {
            if (student == null) continue;

            // 컨디션
            if (data.conditionDelta >= 0)
            {
                // 소모량에 노드 감소 보너스 적용 (Floor로 내림 처리해 소량 보너스도 반영)
                int cost = Mathf.FloorToInt(data.conditionDelta * (1f + nodeConditionBonus));
                cost = Mathf.Max(0, cost);
                student.condition -= cost;
            }
            else
            {
                int recover = -data.conditionDelta;
                recover += Mathf.RoundToInt(recover * conditionBonus);
                student.condition += recover;
            }
            student.condition = Student.ClampCondition(student.condition);

            // 스탯 경험치(훈련 공식 적용)
            StudentStatExpSystem.AddTrainingExp(student, StudentCoreStat.Shoot, data.shootDelta, gymBonus, gymLv, requiredLv, nodeTrainingBonus);
            StudentStatExpSystem.AddTrainingExp(student, StudentCoreStat.Speed, data.speedDelta, gymBonus, gymLv, requiredLv, nodeTrainingBonus);
            StudentStatExpSystem.AddTrainingExp(student, StudentCoreStat.Jump, data.jumpDelta, gymBonus, gymLv, requiredLv, nodeTrainingBonus);
            StudentStatExpSystem.AddTrainingExp(student, StudentCoreStat.Stamina, data.staminaDelta, gymBonus, gymLv, requiredLv, nodeTrainingBonus);

            // 멘탈
            if (data.mentalDelta >= 0)
            {
                StudentStatExpSystem.AddTrainingExpWithRate(student, StudentCoreStat.Mental, data.mentalDelta, mentalBonus, nodeTrainingBonus);
            }
            else
            {
                StudentStatExpSystem.AddRawExp(student, StudentCoreStat.Mental, data.mentalDelta);
            }

            // 포텐셜 추가 경험치(매 훈련 실행마다 1회)
            StudentStatExpSystem.ApplyPotentialTrainingBonusExp(student);

            if (StudentManager.Instance != null)
                StudentManager.Instance.NotifyStudentModified(student);
        }

        if (SaveManager.Instance != null)
        {
            Debug.Log($"[TrainingSelectPopup] 훈련 결과 저장 | key={data.trainingKey} | students={students.Count}");
            SaveManager.Instance.SaveCurrent();
        }
    }
    public struct TrainingStat
    {
        public int condition;
        public int shoot;
        public int speed;
        public int jump;
        public int stamina;
        public int mental;
    }
    // ApplyCsvEffect 복사 안쓰는 것들 빼기 ui 표시용
    private TrainingStat TrainingStatsView(TrainingButtonData data)
    {
        TrainingStat result = new TrainingStat();

        if (data == null)
            return result;

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

        // 훈련시 보너스들
        float statBonus = gymBonus * gymDiff;
        float conditionBonus = (schoolBonus * schoolDiff) + (cafeteriaBonus * cafeteriaDiff);
        float mentalBonus = (counselingBonus * counselingDiff) + (cafeteriaBonus * cafeteriaDiff);

        // 감독 노드 훈련 컨디션 소모 감소 보너스 적용
        float nodeConditionBonus = 0f;
        if (HeadCoachManager.Instance != null && HeadCoachManager.Instance.IsInitialized)
        {
            string trainingKey = data.trainingKey;

            // 슈팅 드릴 (index: 1201)
            if (trainingKey == "cmd_1201")
            {
                nodeConditionBonus +=
                    HeadCoachManager.Instance.GetStatBonusValue("Condition_Drain_ShootingDrill") * 0.01f;
            }

            // 디펜스 워크 (index: 1203)
            if (trainingKey == "cmd_1203")
            {
                nodeConditionBonus +=
                    HeadCoachManager.Instance.GetStatBonusValue("Condition_Drain_DefenceWork") * 0.01f;
            }

            // 단체 훈련 계열 (index: 1101, 1102, 1103)
            if (trainingKey == "cmd_1101" || trainingKey == "cmd_1102" || trainingKey == "cmd_1103")
            {
                nodeConditionBonus +=
                    HeadCoachManager.Instance.GetStatBonusValue("Condition_Drain_TeamPractice") * 0.01f;
            }
        }
        // 컨디션
        if (data.conditionDelta >= 0)
        {
            // 소모량에 노드 감소 보너스 적용 (Floor로 내림 처리해 소량 보너스도 반영)
            int cost = Mathf.FloorToInt(data.conditionDelta * (1f + nodeConditionBonus));
            cost = Mathf.Max(0, cost);
            result.condition = -cost;
        }
        else
        {
            int recover = -data.conditionDelta;
            recover += Mathf.RoundToInt(recover * conditionBonus);
            result.condition = recover;
        }
        // 스탯
        result.shoot = Mathf.RoundToInt(data.shootDelta + data.shootDelta * statBonus);
        result.speed = Mathf.RoundToInt(data.speedDelta + data.speedDelta * statBonus);
        result.jump = Mathf.RoundToInt(data.jumpDelta + data.jumpDelta * statBonus);
        result.stamina = Mathf.RoundToInt(data.staminaDelta + data.staminaDelta * statBonus);

        // 멘탈
        if (data.mentalDelta >= 0)
        {
            result.mental = Mathf.RoundToInt(data.mentalDelta + data.mentalDelta * mentalBonus);
        }
        else
        {
            result.mental = data.mentalDelta;
        }

        return result;
    }

    private string StatTextChange(TrainingStat result)
    {
        List<string> parts = new();

        if (result.shoot != 0)
            parts.Add($"슛 {(result.shoot > 0 ? "+" : "")}{result.shoot}");
        if (result.speed != 0)
            parts.Add($"스피드 {(result.speed > 0 ? "+" : "")}{result.speed}");
        if (result.jump != 0)
            parts.Add($"점프 {(result.jump > 0 ? "+" : "")}{result.jump}");
        if (result.stamina != 0)
            parts.Add($"지구력 {(result.stamina > 0 ? "+" : "")}{result.stamina}");
        //if (result.mental != 0)
        //    parts.Add($"멘탈 {(result.mental > 0 ? "+" : "")}{result.mental}");

        return string.Join(" / ", parts);
    }
    private string ConditionTextChange(TrainingStat result)
    {
        if (result.condition == 0)
            return "";

        return $"컨디션 {(result.condition > 0 ? "+" : "")}{result.condition}";
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
