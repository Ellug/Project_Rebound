using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// 팝업의 용도를 열거형으로 분류
// - Simple  : 이미지/서브텍스트 없이 설명 텍스트와 버튼만 사용하는 단순 안내 팝업 (기존 UIPopup)
// - Confirm : 이미지, 서브텍스트, 설명 텍스트를 모두 사용하는 이벤트/훈련 확인 팝업 (기존 ConfirmPopup)
// - Guide   : Confirm 기반에 페이지 기능이 추가된 가이드용 안내 팝업 (기존 TutorialGuidePopup, PositionGuidePopup)
public enum PopupType
{
    Simple,
    Confirm,
    Guide
}

// Guide 타입 팝업의 페이지 1개에 해당하는 데이터
// TutorialGuidePopup, PositionGuidePopup 공용
[Serializable]
public sealed class GuidePage
{
    public string Title;

    [TextArea(3, 10)]
    public string Content;

    public Sprite Image; // 이미지 없으면 null
}

// 범용 팝업 베이스 클래스
// PopupType에 따라 이미지/서브텍스트/페이지 기능을 선택적으로 활성화
public class UIPopup : UIBase
{
    [Header("Layout")]
    [SerializeField] private RectTransform _backPanelRect;        // 팝업 패널 전체
    [SerializeField] private RectTransform _textGroupRect;        // 텍스트/버튼 그룹

    [Header("Simple 전용 레이아웃 보정")]
    [SerializeField] private float _simplePullUpDistance = 50f;   // Simple 타입일 때 패널 축소 및 텍스트 이동 거리

    [Header("Content")]
    [SerializeField] private Image _imgPreview;                   // 미리보기 이미지 (Confirm/Guide 전용)
    [SerializeField] private TMP_Text _txtTitle;                  // 제목
    [SerializeField] private TMP_Text _txtSub;                    // 서브 메시지 (Confirm/Guide 전용)
    [SerializeField] private TMP_Text _txtMessage;                // 본문 메시지

    [Header("Buttons - 미리 배치 후 상황에 따라 활성화")]
    [SerializeField] private Button _btnConfirm;                  // 확인 버튼
    [SerializeField] private Button _btnCancel;                   // 취소 버튼
    [SerializeField] private Button _btnNext;                     // 다음 버튼 (Guide 전용)
    [SerializeField] private Button _btnPrev;                     // 이전 버튼 (Guide 전용)
    [SerializeField] private Button _btnTrainingConfirm;          // 훈련확인 버튼 (Confirm 전용)
    [SerializeField] private Button _btnClose;                    // X 닫기 버튼

    [Header("Page Dots (Guide 전용)")]
    [SerializeField] private Transform _dotRoot;
    [SerializeField] private Image _dotPrefab;
    [SerializeField] private float _dotNormalScale = 1.0f;
    [SerializeField] private float _dotActiveScale = 1.4f;
    [SerializeField] private Color _dotNormalColor = new Color(0.75f, 0.75f, 0.75f, 1f);
    [SerializeField] private Color _dotActiveColor = Color.black;

    [Header("Student Select (Confirm 전용)")]
    [SerializeField] private StudentSelectPopup _studentSelectPrefab;

    private readonly List<Image> _spawnedDots = new(); // 생성된 점 캐시

    private PopupType _popupType;
    private ConfirmPopupRequest _request;         // 외부에서 전달받는 설정 데이터
    private List<GuidePage> _guidePages;          // Guide 타입 페이지 데이터
    private int _pageIndex;                       // 현재 페이지 인덱스

    private StudentSelectPopup _activeStudentSelectPopup; // 참조 보관용 필드
    private bool _hasInvokedConfirmAction;
    private bool _skipConfirmOnCloseInvocation;
    private bool _isLayoutAdjusted;              // Simple 레이아웃 보정 중복 방지

    public override void Init()
    {
        base.Init();

        // 버튼 이벤트 바인딩
        if (_btnClose != null)
        {
            _btnClose.onClick.RemoveAllListeners();
            _btnClose.onClick.AddListener(OnCloseButtonClicked);
        }

        BindButtonEvents();
    }

    // 버튼 이벤트 일괄 바인딩
    private void BindButtonEvents()
    {
        if (_btnConfirm != null)
        {
            _btnConfirm.onClick.RemoveAllListeners();
            _btnConfirm.onClick.AddListener(HandleConfirmClicked);
        }

        if (_btnCancel != null)
        {
            _btnCancel.onClick.RemoveAllListeners();
            _btnCancel.onClick.AddListener(HandleCancelClicked);
        }

        if (_btnTrainingConfirm != null)
        {
            _btnTrainingConfirm.onClick.RemoveAllListeners();
            _btnTrainingConfirm.onClick.AddListener(HandleConfirmClicked);
        }

        // 이전 버튼 바인딩
        if (_btnPrev != null)
        {
            _btnPrev.onClick.RemoveAllListeners();
            _btnPrev.onClick.AddListener(HandlePrevClicked);
        }

        if (_btnNext != null)
        {
            _btnNext.onClick.RemoveAllListeners();
            _btnNext.onClick.AddListener(HandleNextClicked);
        }
    }

    // ───────────────────────────────────────────────
    // Setup - 외부에서 팝업 설정 적용
    // ───────────────────────────────────────────────

    // Simple / Confirm 타입 설정
    public void Setup(ConfirmPopupRequest request, PopupType popupType = PopupType.Confirm)
    {
        _request = request;
        _popupType = popupType;
        _hasInvokedConfirmAction = false;
        _skipConfirmOnCloseInvocation = false;
        IsModal = request.IsModal;

        ApplyLayout();
        ApplyTexts(request);
        ApplyPreview(request);
        ApplyButtons(request);
    }

    // Guide 타입 설정
    public void SetupGuide(List<GuidePage> pages)
    {
        _popupType = PopupType.Guide;
        _guidePages = pages ?? new List<GuidePage>();
        _pageIndex = 0;

        ApplyLayout();
        EnsureDots();
        RefreshGuidePage();
    }

    // ───────────────────────────────────────────────
    // 레이아웃 적용
    // ───────────────────────────────────────────────

    // PopupType에 따라 이미지/서브텍스트/페이지 UI 활성화 여부 결정
    private void ApplyLayout()
    {
        bool isSimple = _popupType == PopupType.Simple;
        bool isGuide = _popupType == PopupType.Guide;

        // Simple: 이미지 비활성화 (서브텍스트는 ApplyTexts에서 제어)
        if (_imgPreview != null)
            _imgPreview.gameObject.SetActive(false);

        // 페이지 dot은 Guide 타입에서만 활성화
        if (_dotRoot != null)
            _dotRoot.gameObject.SetActive(isGuide);

        // Guide용 버튼 초기 비활성 (RefreshGuidePage에서 세부 제어)
        if (_btnPrev != null) _btnPrev.gameObject.SetActive(false);
        if (_btnNext != null) _btnNext.gameObject.SetActive(false);

        // Simple 타입이고 아직 보정 전이면 패널 세로 축소 + 텍스트 그룹 이동
        if (isSimple && !_isLayoutAdjusted)
        {
            _isLayoutAdjusted = true;
            AdjustSimpleLayout();
        }
    }

    // Simple 타입 전용 레이아웃 보정
    // 이미지/서브텍스트 없는 만큼 패널 세로를 줄이고 텍스트를 올림
    private void AdjustSimpleLayout()
    {
        // 1. back panel 전체 세로 길이 줄이기
        if (_backPanelRect != null)
        {
            _backPanelRect.sizeDelta = new Vector2(
                _backPanelRect.sizeDelta.x,
                _backPanelRect.sizeDelta.y - _simplePullUpDistance
            );
        }

        // 2. 텍스트, 버튼 묶음을 절반만큼 위로 당기기
        if (_textGroupRect != null)
        {
            _textGroupRect.anchoredPosition = new Vector2(
                _textGroupRect.anchoredPosition.x,
                _textGroupRect.anchoredPosition.y + _simplePullUpDistance / 2f
            );
        }
    }

    // ───────────────────────────────────────────────
    // 텍스트 영역 표시/숨김 처리
    // ───────────────────────────────────────────────

    private void ApplyTexts(ConfirmPopupRequest request)
    {
        if (_txtTitle != null)
        {
            bool hasTitle = !string.IsNullOrEmpty(request.Title);
            _txtTitle.gameObject.SetActive(hasTitle);
            if (hasTitle) _txtTitle.text = request.Title;
        }

        // 서브 메시지는 Simple 타입에서 비활성화
        if (_txtSub != null)
        {
            bool hasSub = _popupType != PopupType.Simple && !string.IsNullOrEmpty(request.SubMessage);
            _txtSub.gameObject.SetActive(hasSub);
            if (hasSub) _txtSub.text = request.SubMessage;
        }

        if (_txtMessage != null)
        {
            bool hasMsg = !string.IsNullOrEmpty(request.Message);
            _txtMessage.gameObject.SetActive(hasMsg);
            if (hasMsg) _txtMessage.text = request.Message;
        }
    }

    // 미리보기 이미지 설정
    // Simple 타입이면 이미지 자체를 사용하지 않음
    private void ApplyPreview(ConfirmPopupRequest request)
    {
        if (_imgPreview == null || _popupType == PopupType.Simple)
            return;

        bool hasSprite = request.PreviewSprite != null;
        _imgPreview.gameObject.SetActive(hasSprite);

        if (hasSprite)
        {
            _imgPreview.sprite = request.PreviewSprite;
            _imgPreview.preserveAspect = true;
        }
    }

    // 버튼 표시 여부 설정
    //
    // [Simple]  이미지1~5 기준
    //   - 확인(_btnConfirm)   : 항상 표시
    //   - 취소(_btnCancel)    : 항상 비활성 (Simple은 확인 버튼 하나만 사용)
    //
    // [Confirm] 이미지6~11 기준
    //   - 취소(_btnCancel)          : SecondaryLabel 있을 때만
    //                                 (이미지6,9,10 = 확인만 / 이미지7,8,11 = 취소+확인)
    //   - 훈련시작(_btnTrainingConfirm): request.UseTrainingConfirmButton == true 일 때만
    //                                 (이미지11 웨이트 트레이닝처럼 훈련 실행 전용 확인에 사용)
    //   - 확인(_btnConfirm)         : UseTrainingConfirmButton == false 일 때
    //                                 (이미지6~10: 일반 이벤트 확인 / 이미지8 주말훈련제안 포함)
    //
    // [Guide]  ApplyGuideButtonState에서 제어
    private void ApplyButtons(ConfirmPopupRequest request)
    {
        // 취소 버튼: Simple 타입은 항상 비활성, Confirm은 SecondaryLabel 있을 때만 표시
        bool hasSecondary = _popupType != PopupType.Simple
            && !string.IsNullOrEmpty(request.SecondaryLabel);
        if (_btnCancel != null)
            _btnCancel.gameObject.SetActive(hasSecondary);

        // 훈련시작 버튼: 호출부에서 명시적으로 UseTrainingConfirmButton = true 를 지정한 경우에만 표시
        // SubMessage 유무로 판단하지 않음 (이미지8 주말훈련제안은 서브텍스트 있어도 일반 확인 버튼 사용)
        bool showTrainingConfirm = _popupType == PopupType.Confirm
            && request.UseTrainingConfirmButton;
        if (_btnTrainingConfirm != null)
            _btnTrainingConfirm.gameObject.SetActive(showTrainingConfirm);

        // 일반 확인 버튼: 훈련시작 버튼이 표시되지 않을 때 사용
        if (_btnConfirm != null)
        {
            _btnConfirm.gameObject.SetActive(!showTrainingConfirm);
            _btnConfirm.interactable = request.PrimaryInteractable;
        }
    }

    // ───────────────────────────────────────────────
    // 확인 버튼 클릭
    // ───────────────────────────────────────────────

    private void HandleConfirmClicked()
    {
        if (_request == null)
        {
            CloseSelf();
            return;
        }

        // 학생 선택이 필요한 경우
        if (_request.RequiresStudentSelection)
        {
            OpenStudentSelect();
            return;
        }

        InvokeConfirmAction();

        if (_request.AutoCloseOnPrimary)
            CloseSelf();
    }

    // 취소 버튼 클릭
    private void HandleCancelClicked()
    {
        if (_request == null)
        {
            CloseSelf();
            return;
        }

        _skipConfirmOnCloseInvocation = true;
        _request.SecondaryAction?.Invoke();

        if (_request.AutoCloseOnSecondary)
            CloseSelf();
    }

    // 이전 페이지 이동
    private void HandlePrevClicked()
    {
        if (_guidePages == null || _guidePages.Count == 0) return;
        _pageIndex = Mathf.Max(0, _pageIndex - 1);
        RefreshGuidePage();
    }

    // 다음 페이지 이동 또는 닫기 (마지막 페이지일 때)
    private void HandleNextClicked()
    {
        if (_guidePages == null || _guidePages.Count == 0)
        {
            CloseSelf();
            return;
        }

        bool isLast = _pageIndex >= _guidePages.Count - 1;
        if (isLast)
        {
            CloseSelf();
        }
        else
        {
            _pageIndex = Mathf.Min(_guidePages.Count - 1, _pageIndex + 1);
            RefreshGuidePage();
        }
    }

    // ───────────────────────────────────────────────
    // 현재 페이지 기준 UI 전체 갱신 (Guide 전용)
    // ───────────────────────────────────────────────

    private void RefreshGuidePage()
    {
        if (_guidePages == null || _guidePages.Count == 0)
        {
            // 데이터가 없을 때 기본 메시지
            if (_txtTitle != null) _txtTitle.text = "안내";
            if (_txtMessage != null) _txtMessage.text = "페이지 데이터가 없습니다.";
            if (_imgPreview != null) _imgPreview.gameObject.SetActive(false);

            ApplyGuideButtonState(isFirst: true, isLast: true);
            RefreshDots();
            return;
        }

        _pageIndex = Mathf.Clamp(_pageIndex, 0, _guidePages.Count - 1);
        GuidePage page = _guidePages[_pageIndex];

        // 텍스트 갱신
        if (_txtTitle != null) _txtTitle.text = page.Title;
        if (_txtMessage != null) _txtMessage.text = page.Content;

        // 이미지 표시 여부 처리
        if (_imgPreview != null)
        {
            bool has = page.Image != null;
            _imgPreview.gameObject.SetActive(has);
            if (has)
            {
                _imgPreview.sprite = page.Image;
                _imgPreview.preserveAspect = true;
            }
        }

        bool isFirst = _pageIndex == 0;
        bool isLast = _pageIndex == _guidePages.Count - 1;

        ApplyGuideButtonState(isFirst, isLast);
        EnsureDots();
        RefreshDots();
    }

    // 버튼 상태(취소 / 이전 / 다음 / 닫기) 적용
    // Guide 타입: 취소는 항상 표시, 이전은 첫 페이지 아닐 때, 다음은 마지막 페이지에서 "닫기"로 전환
    private void ApplyGuideButtonState(bool isFirst, bool isLast)
    {
        // 취소 버튼: Guide 타입에서는 항상 표시
        if (_btnCancel != null) _btnCancel.gameObject.SetActive(true);

        // 이전 버튼: 첫 페이지이면 숨김
        if (_btnPrev != null) _btnPrev.gameObject.SetActive(!isFirst);

        if (_btnNext == null) return;

        // 다음/닫기 버튼: 마지막 페이지이면 닫기 역할로 전환
        _btnNext.onClick.RemoveAllListeners();

        if (isLast)
        {
            _btnNext.onClick.AddListener(CloseSelf);
        }
        else
        {
            _btnNext.onClick.AddListener(HandleNextClicked);
        }

        _btnNext.gameObject.SetActive(true);
    }

    // ───────────────────────────────────────────────
    // 페이지 수에 맞게 점 개수 조정 (Guide 전용)
    // ───────────────────────────────────────────────

    private void EnsureDots()
    {
        if (_dotRoot == null || _dotPrefab == null) return;

        int targetCount = _guidePages != null ? _guidePages.Count : 0;

        // 부족하면 생성
        while (_spawnedDots.Count < targetCount)
        {
            Image dot = Instantiate(_dotPrefab, _dotRoot);
            dot.gameObject.SetActive(true);
            _spawnedDots.Add(dot);
        }

        // 초과하면 제거
        while (_spawnedDots.Count > targetCount)
        {
            int last = _spawnedDots.Count - 1;
            Image dot = _spawnedDots[last];
            _spawnedDots.RemoveAt(last);
            if (dot != null) Destroy(dot.gameObject);
        }
    }

    // 현재 페이지 기준 점 강조 갱신
    private void RefreshDots()
    {
        for (int i = 0; i < _spawnedDots.Count; i++)
        {
            Image dot = _spawnedDots[i];
            if (dot == null) continue;

            bool active = i == _pageIndex;
            dot.color = active ? _dotActiveColor : _dotNormalColor;

            float scale = active ? _dotActiveScale : _dotNormalScale;
            dot.rectTransform.localScale = new Vector3(scale, scale, 1f);
        }
    }

    // ───────────────────────────────────────────────
    // 학생 선택 팝업 열기 (Confirm 타입 전용)
    // ───────────────────────────────────────────────

    private void OpenStudentSelect()
    {
        if (_studentSelectPrefab == null)
        {
            // 프리팹 없으면 전체 학생 반환
            List<Student> fallback = StudentManager.Instance != null
                ? new List<Student>(StudentManager.Instance.Students)
                : new List<Student>();

            _request.OnStudentsSelected?.Invoke(fallback);

            if (_request.AutoCloseOnPrimary)
                CloseSelf();

            return;
        }

        // 현재 팝업 닫고 학생 선택 팝업 생성
        Close();

        StudentSelectPopup popup = Instantiate(_studentSelectPrefab, transform.parent);
        _activeStudentSelectPopup = popup; // 참조 저장
        popup.SetMaxSelectCount(_request.MaxSelectCount);
        popup.Init();
        popup.Open();

        popup.OnSelectionConfirmed += HandleStudentsSelected;
        popup.OnCancelled += HandleStudentSelectCancelled;
    }

    // 학생 선택 완료 콜백
    private void HandleStudentsSelected(List<Student> students)
    {
        _activeStudentSelectPopup = null;
        _request.OnStudentsSelected?.Invoke(students);

        if (_request.AutoCloseOnPrimary)
            CloseSelf();
    }

    // 학생 선택 취소 시 다시 열기
    private void HandleStudentSelectCancelled()
    {
        // 팝업이 이미 파괴됐으면 무시
        if (this == null || gameObject == null) return;
        Open();
    }

    public override void Close()
    {
        TryInvokeConfirmOnClose();
        base.Close();
    }

    protected virtual void OnCloseButtonClicked()
    {
        CloseSelf();
    }

    // 안전한 닫기 처리 (UIManager 우선)
    private void CloseSelf()
    {
        if (UIManager.Instance != null)
        {
            UIManager.Instance.Close(this);
            return;
        }

        Close();
        Destroy(gameObject);
    }

    private void InvokeConfirmAction()
    {
        if (_request == null || _hasInvokedConfirmAction)
            return;

        _hasInvokedConfirmAction = true;
        _request.PrimaryAction?.Invoke();
    }

    private void TryInvokeConfirmOnClose()
    {
        if (_request == null) return;

        if (_skipConfirmOnCloseInvocation || !_request.InvokeConfirmOnClose || _hasInvokedConfirmAction)
            return;

        // 학생 선택이 필요한 요청은 닫힘 시 동일한 입력을 재현할 수 없어 강제 실행하지 않는다.
        if (_request.RequiresStudentSelection)
            return;

        InvokeConfirmAction();
    }

    // UIPopup이 외부 요인으로 먼저 파괴될 때 구독 해제
    private void OnDestroy()
    {
        if (_activeStudentSelectPopup != null)
        {
            _activeStudentSelectPopup.OnSelectionConfirmed -= HandleStudentsSelected;
            _activeStudentSelectPopup.OnCancelled -= HandleStudentSelectCancelled;
            _activeStudentSelectPopup = null;
        }
    }
}
