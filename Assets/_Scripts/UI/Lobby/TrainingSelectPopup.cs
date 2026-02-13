using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

// 훈련 선택 팝업 (훈련 선택 / 단체 훈련 / 개인 훈련을 하나의 프리팹에서 페이지 전환으로 처리)
public class TrainingSelectPopup : UIPopup
{
    [Header("Page Config")]
    [SerializeField] private TrainingPageData _pageData;

    [Header("Training UI")]
    [SerializeField] private TMP_Text _txtPageTitle;
    [SerializeField] private Transform _buttonContainer;        // 버튼 부모 (VerticalLayoutGroup)
    [SerializeField] private TrainingButtonItem _buttonPrefab2; // 부모와 자식에서 같은 이름을 쓰면 유니티에서 허용하지 않음.

    [Header("Navigation")]
    [SerializeField] private Button _btnBack;

    [Header("Confirm Popup")]
    [SerializeField] private TrainingConfirmPopup _confirmPopupPrefab;

    private int _currentPageIndex = 0;
    private readonly Stack<int> _pageHistory = new Stack<int>();          // 뒤로가기용 페이지 스택
    private readonly List<TrainingButtonItem> _spawnedButtons = new List<TrainingButtonItem>();

    // 최종 훈련 선택 확정 시 trainingKey 전달
    public event System.Action<string> OnTrainingSelected;

    public override void Init()
    {
        base.Init();

        if (_btnBack != null)
        {
            _btnBack.onClick.RemoveAllListeners();
            _btnBack.onClick.AddListener(HandleBackButton);
        }

        // 첫 페이지(훈련 선택) 표시
        ShowPage(0, pushHistory: false);
    }

    // 정리 후 팝업 닫기
    protected override void OnCloseButtonClicked()
    {
        ClearPageHistory();
        ClearButtons();
        Close();
    }

    // 지정 인덱스의 페이지로 전환
    public void ShowPage(int pageIndex, bool pushHistory = true)
    {
        if (_pageData == null || _pageData.pages == null
            || pageIndex < 0 || pageIndex >= _pageData.pages.Count)
        {
            Debug.LogError($"[TrainingSelectPopup] 유효하지 않은 페이지: {pageIndex}");
            return;
        }

        if (pushHistory)
        {
            _pageHistory.Push(_currentPageIndex);
        }

        _currentPageIndex = pageIndex;
        TrainingPageInfo page = _pageData.pages[pageIndex];

        if (_txtPageTitle != null)
        {
            _txtPageTitle.text = page.pageTitle;
        }

        ClearButtons();
        SpawnButtons(page);
        UpdateBackButtonVisibility();
    }

    // 페이지 데이터 기반으로 버튼 동적 생성
    private void SpawnButtons(TrainingPageInfo page)
    {
        foreach (TrainingButtonData btnData in page.buttons)
        {
            TrainingButtonItem item = Instantiate(_buttonPrefab2, _buttonContainer);
            item.gameObject.SetActive(true);

            // 클로저 캡처용 로컬 변수
            TrainingButtonData captured = btnData;

            item.Setup(
                captured.trainingName,
                captured.statModifierText,
                () => HandleTrainingButton(captured)
            );

            _spawnedButtons.Add(item);
        }
    }

    // 페이지 이동 or 확인 팝업 분기 처리
    private void HandleTrainingButton(TrainingButtonData data)
    {
        // 1) 페이지 이동 버튼
        if (data.navigateToPageIndex >= 0)
        {
            ShowPage(data.navigateToPageIndex);
            return;
        }

        // 2) 최종 실행 버튼 -> 확인 팝업 띄우기
        OpenConfirmPopup(data);
    }

    // 훈련 확인 팝업 열기
    private void OpenConfirmPopup(TrainingButtonData data)
    {
        // 확인 팝업 프리팹이 없으면 바로 선택 완료
        if (_confirmPopupPrefab == null)
        {
            Debug.Log($"[TrainingSelectPopup] 확인 팝업 없이 바로 실행: {data.trainingKey}");
            OnTrainingSelected?.Invoke(data.trainingKey);
            Close();
            return;
        }

        // 확인 팝업 생성 (Instantiate → 자체 관리, UIManager 스택 미사용)
        TrainingConfirmPopup confirm = Instantiate(_confirmPopupPrefab, transform.parent);
        confirm.SetOwner(this);
        confirm.Init();
        confirm.Setup(
            data.trainingKey,
            data.trainingName,
            data.conditionDelta,
            data.trainingDesc,
            data.previewSprite
        );
        confirm.Open();

        // 확인(시작) 버튼 클릭 시
        confirm.OnConfirm += (key) =>
        {
            OnTrainingSelected?.Invoke(key);
        };
    }

    // 이전 페이지로 복귀
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

    // 첫 페이지에서는 뒤로가기 버튼 숨김
    private void UpdateBackButtonVisibility()
    {
        if (_btnBack != null)
        {
            _btnBack.gameObject.SetActive(_pageHistory.Count > 0);
        }
    }

    private void ClearButtons()
    {
        foreach (TrainingButtonItem item in _spawnedButtons)
        {
            if (item != null)
            {
                Destroy(item.gameObject);
            }
        }
        _spawnedButtons.Clear();
    }

    private void ClearPageHistory()
    {
        _pageHistory.Clear();
    }

    // 자식 팝업(훈련 확인)에서 호출: 훈련 선택 팝업 정리 후 닫기
    public void ForceCloseFromChild()
    {
        ClearPageHistory();
        ClearButtons();
        Close();
    }

    public override void Open()
    {
        base.Open();

        // 열릴 때마다 초기 페이지 다시 구성
        ShowPage(0, pushHistory: false);
    }
}