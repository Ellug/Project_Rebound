using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// UIPopup (Host)
// Simple / Default / Guide 3패널 고정 레이아웃
// 버튼/텍스트는 "위치 고정" 전제. 코드에서는 SetActive 토글만 수행한다.
// RequiresStudentSelection=true 인 경우: Primary 클릭 시 StudentSelectPopup을 열고 선택 완료 콜백을 실행
public class UIPopup : UIPopupBase
{
    [Header("Panels")]
    [SerializeField] private GameObject _panelSimple;           // Simple 패널 루트
    [SerializeField] private GameObject _panelDefault;          // Default 패널 루트
    [SerializeField] private GameObject _panelGuide;            // Guide 패널 루트

    [Header("Simple UI")]
    [SerializeField] private TMP_Text _txtSimpleTitle;          // Simple 제목
    [SerializeField] private TMP_Text _txtSimpleMessage;        // Simple 본문
    [SerializeField] private Button _btnSimpleCancel;           // Simple 취소 버튼
    [SerializeField] private Button _btnSimpleConfirm;          // Simple 확인 버튼

    [Header("Default UI")]
    [SerializeField] private TMP_Text _txtDefaultTitle;         // Default 제목
    [SerializeField] private TMP_Text _txtDefaultSub;           // Default 서브
    [SerializeField] private TMP_Text _txtDefaultMessage;       // Default 본문
    [SerializeField] private Image _imgDefaultPreview;          // Default 이미지
    [SerializeField] private Button _btnDefaultCancel;          // Default 취소 버튼

    // Default Primary 버튼들 (같은 위치 고정, 필요한 것만 켜기)
    [SerializeField] private Button _btnDefaultConfirm;         // 확인 버튼 (PrimaryKind=Confirm)
    [SerializeField] private Button _btnDefaultStartTraining;   // 훈련 시작/훈련 확인

    [Header("Guide UI")]
    [SerializeField] private TMP_Text _txtGuideTitle;           // Guide 제목(페이지 타이틀)
    [SerializeField] private TMP_Text _txtGuideSub;             // Guide 서브
    [SerializeField] private TMP_Text _txtGuideMessage;         // Guide 본문(페이지 메시지)
    [SerializeField] private Image _imgGuidePreview;            // Guide 이미지
    [SerializeField] private Button _btnGuidePrev;              // 이전 페이지 버튼
    [SerializeField] private Button _btnGuideCancel;            // 가이드 취소 버튼
    [SerializeField] private Button _btnGuideNext;              // 다음 페이지 버튼
    [SerializeField] private Button _btnGuideClose;             // 마지막 페이지 닫기 버튼 (Primary 액션)

    [Header("Guide Dots")]
    [SerializeField] private Transform _dotRoot;                // 페이지 도트 부모
    [SerializeField] private Image _dotPrefab;                  // 페이지 도트 프리팹

    [SerializeField] private float _dotNormalScale = 1.0f;      // 비활성 스케일
    [SerializeField] private float _dotActiveScale = 1.4f;      // 활성 스케일

    [SerializeField] private Color _dotNormalColor = new Color(0.75f, 0.75f, 0.75f, 1f);  // 비활성 색상
    [SerializeField] private Color _dotActiveColor = Color.white;                         // 활성화 색상

    [Header("애니메이션")]
    [SerializeField] private PopupAnimator _animator;

    private readonly List<Image> _spawnedDots = new();          // 생성된 페이지 도트 캐시

    private UIPopupRequest _request;                            // 현재 팝업 요청 데이터
    private int _pageIndex;                                     // 가이드 현재 페이지 인덱스
    private string _currentDefaultImageId;                      // Default 패널 현재 로드된 이미지 ID (해제용)
    private string _currentGuideImageId;                        // Guide 패널 현재 로드된 이미지 ID (해제용)

    public override void Init()
    {
        base.Init();
        SetAllPanels(false); // 초기에는 모든 패널 비활성
    }

    // 파생 클래스(RecruitmentPopup 등)에서 자체 애니메이션을 쓸 때 호출
    // UIPopup._animator를 거치지 않고 UIBase.Open()만 실행
    public void OpenBase()
    {
        base.Open();
    }

    public override void Open()
    {
        if (_animator == null)
        {
            base.Open();
            return;
        }

        // SetActive(true) 전에 Initialize로 위치/스케일 초기화 보장
        _animator.Initialize();

        base.Open();

        _animator.PlayIn();
    }

    public override void Close()
    {
        TryInvokePrimaryOnClose(); // 닫힘 시 Primary 호출 옵션 처리
        ReleaseDefaultImage();
        ReleaseGuideImage();

        // PlayOut 완료 후 base.Close() (SetActive(false)) 처리
        _animator.PlayOut(() => base.Close());
    }

    // UIManager가 UIPopupRequest를 주입
    public void Setup(UIPopupRequest request)
    {
        _request = request;

        if (_request == null)
            return;

        this.DisableBackKey = _request.DisableBackKey;

        switch (_request.Type)
        {
            case UIPopupRequest.PanelType.Simple:
                SetupSimple(_request);
                break;

            case UIPopupRequest.PanelType.Default:
                SetupDefault(_request);
                break;

            case UIPopupRequest.PanelType.Guide:
                SetupGuide(_request);
                break;
        }
    }

    // 팝업이 닫힐 때 PrimaryAction을 실행해야 하는 케이스 지원 (예: 리그 진입)
    private void TryInvokePrimaryOnClose()
    {
        if (_request == null)
            return;

        if (!_request.InvokePrimaryOnClose)
            return;

        if (_request.RequiresStudentSelection)
            return;

        if (!_request.PrimaryInteractable)
            return;

        _request.OnPrimary?.Invoke();
    }

    // Simple
    private void SetupSimple(UIPopupRequest request)
    {
        ActivatePanel(_panelSimple);

        if (_txtSimpleTitle != null) _txtSimpleTitle.text = request.Title ?? "";
        if (_txtSimpleMessage != null) _txtSimpleMessage.text = request.Message ?? "";

        if (_btnSimpleCancel != null)
        {
            _btnSimpleCancel.gameObject.SetActive(request.ShowCancel);
            _btnSimpleCancel.onClick.RemoveAllListeners();
            _btnSimpleCancel.onClick.AddListener(() => InvokeCancel(request));
        }

        if (_btnSimpleConfirm != null)
        {
            _btnSimpleConfirm.gameObject.SetActive(true);
            _btnSimpleConfirm.interactable = request.PrimaryInteractable;
            _btnSimpleConfirm.onClick.RemoveAllListeners();
            _btnSimpleConfirm.onClick.AddListener(() => InvokePrimary(request));
        }
    }

    // Default
    private void SetupDefault(UIPopupRequest request)
    {
        ActivatePanel(_panelDefault);

        if (_txtDefaultTitle != null) _txtDefaultTitle.text = request.Title ?? "";
        if (_txtDefaultMessage != null) _txtDefaultMessage.text = request.Message ?? "";

        if (_txtDefaultSub != null)
        {
            bool hasSub = !string.IsNullOrEmpty(request.SubMessage);
            _txtDefaultSub.gameObject.SetActive(hasSub);
            if (hasSub) _txtDefaultSub.text = request.SubMessage;
        }

        // 이미지 ID 기준으로 Addressable 비동기 로드
        LoadDefaultImage(request.PreviewImageId);

        if (_btnDefaultCancel != null)
        {
            _btnDefaultCancel.gameObject.SetActive(request.ShowCancel);
            _btnDefaultCancel.onClick.RemoveAllListeners();
            _btnDefaultCancel.onClick.AddListener(() => InvokeCancel(request));
        }

        ApplyDefaultPrimary(request);
    }

    // Default Primary 버튼 토글 (Confirm vs StartTraining)
    // - 버튼 위치는 프리팹에서 고정
    // - 여기서는 "나오고/안나오고"만 제어
    private void ApplyDefaultPrimary(UIPopupRequest request)
    {
        bool useConfirm = request.PrimaryKind == UIPopupRequest.PrimaryButtonKind.Confirm;
        bool useTraining = request.PrimaryKind == UIPopupRequest.PrimaryButtonKind.StartTraining;

        if (_btnDefaultConfirm != null)
        {
            _btnDefaultConfirm.gameObject.SetActive(useConfirm);
            _btnDefaultConfirm.interactable = request.PrimaryInteractable;
            _btnDefaultConfirm.onClick.RemoveAllListeners();
            _btnDefaultConfirm.onClick.AddListener(() => InvokePrimary(request));
        }

        if (_btnDefaultStartTraining != null)
        {
            _btnDefaultStartTraining.gameObject.SetActive(useTraining);
            _btnDefaultStartTraining.interactable = request.PrimaryInteractable;
            _btnDefaultStartTraining.onClick.RemoveAllListeners();
            _btnDefaultStartTraining.onClick.AddListener(() => InvokePrimary(request));
        }
    }

    // Guide
    private void SetupGuide(UIPopupRequest request)
    {
        ActivatePanel(_panelGuide);

        _pageIndex = 0; // 가이드 시작은 0페이지

        if (_btnGuideCancel != null)
        {
            _btnGuideCancel.gameObject.SetActive(request.ShowCancel);
            _btnGuideCancel.onClick.RemoveAllListeners();
            _btnGuideCancel.onClick.AddListener(() => InvokeCancel(request));
        }

        EnsureDots();        // 페이지 수에 맞게 도트 생성/정리
        RefreshGuidePage();  // 첫 페이지 표시
    }

    // 현재 페이지 데이터를 UI에 반영
    private void RefreshGuidePage()
    {
        List<UIPopupRequest.GuidePage> pages = _request != null ? _request.Pages : null;

        // 페이지가 없으면 단일 메시지(Title/Message)로 폴백
        if (pages == null || pages.Count == 0)
        {
            if (_txtGuideTitle != null) _txtGuideTitle.text = _request != null ? (_request.Title ?? "") : "";
            if (_txtGuideMessage != null) _txtGuideMessage.text = _request != null ? (_request.Message ?? "") : "";
            if (_txtGuideSub != null) _txtGuideSub.gameObject.SetActive(false);

            LoadGuideImage(null);
            ApplyGuideButtonState(isLast: true);
            RefreshDots();
            return;
        }

        _pageIndex = Mathf.Clamp(_pageIndex, 0, pages.Count - 1);
        UIPopupRequest.GuidePage page = pages[_pageIndex];

        if (_txtGuideTitle != null) _txtGuideTitle.text = page.Title ?? "";
        if (_txtGuideMessage != null) _txtGuideMessage.text = page.Message ?? "";

        if (_txtGuideSub != null)
        {
            bool hasSub = !string.IsNullOrEmpty(page.SubMessage);
            _txtGuideSub.gameObject.SetActive(hasSub);
            if (hasSub) _txtGuideSub.text = page.SubMessage;
        }

        // 이미지 ID 기준으로 Addressable 비동기 로드
        LoadGuideImage(page.PreviewImageId);

        bool isLast = _pageIndex == pages.Count - 1;
        ApplyGuideButtonState(isLast);
        EnsureDots();  // 페이지 수 변경 대응(동적 주입 가능)
        RefreshDots(); // 현재 페이지 강조
    }

    private void PrevGuidePage()
    {
        if (_request == null || _request.Pages == null || _request.Pages.Count == 0)
            return;

        _pageIndex = Mathf.Max(0, _pageIndex - 1);
        RefreshGuidePage();
    }

    private void ApplyGuideButtonState(bool isLast)
    {
        // Prev: 0페이지면 숨김, 그 외 표시
        if (_btnGuidePrev != null)
        {
            bool canPrev = _pageIndex > 0;
            _btnGuidePrev.gameObject.SetActive(canPrev);
            _btnGuidePrev.onClick.RemoveAllListeners();
            if (canPrev)
                _btnGuidePrev.onClick.AddListener(PrevGuidePage);
        }

        // Next: 마지막 페이지면 숨김
        if (_btnGuideNext != null)
        {
            _btnGuideNext.gameObject.SetActive(!isLast);
            _btnGuideNext.onClick.RemoveAllListeners();
            _btnGuideNext.onClick.AddListener(NextGuidePage);
        }

        // Close: 마지막 페이지에서만 표시
        if (_btnGuideClose != null)
        {
            _btnGuideClose.gameObject.SetActive(isLast);
            _btnGuideClose.onClick.RemoveAllListeners();
            _btnGuideClose.onClick.AddListener(() =>
            {
                InvokePrimary(_request);
            });
        }
    }

    private void NextGuidePage()
    {
        if (_request == null || _request.Pages == null || _request.Pages.Count == 0)
            return;

        _pageIndex = Mathf.Min(_request.Pages.Count - 1, _pageIndex + 1);
        RefreshGuidePage();
    }

    private void EnsureDots()
    {
        if (_dotRoot == null || _dotPrefab == null)
            return;

        int targetCount = (_request != null && _request.Pages != null) ? _request.Pages.Count : 0;

        // 부족하면 생성
        while (_spawnedDots.Count < targetCount)
        {
            Image dot = Instantiate(_dotPrefab, _dotRoot);
            dot.gameObject.SetActive(true);
            _spawnedDots.Add(dot);
        }

        // 많으면 제거
        while (_spawnedDots.Count > targetCount)
        {
            int last = _spawnedDots.Count - 1;
            Image dot = _spawnedDots[last];
            _spawnedDots.RemoveAt(last);
            if (dot != null) Destroy(dot.gameObject);
        }
    }

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

    // Default 패널 이미지 ID 기준으로 Addressable 비동기 로드
    private void LoadDefaultImage(string imageId)
    {
        ReleaseDefaultImage();

        if (_imgDefaultPreview == null) return;

        if (string.IsNullOrEmpty(imageId))
        {
            _imgDefaultPreview.gameObject.SetActive(false);
            return;
        }

        _imgDefaultPreview.gameObject.SetActive(false);
        _currentDefaultImageId = imageId;

        AddressableImageManager.Instance.LoadSprite(imageId, sprite =>
        {
            if (_imgDefaultPreview == null) return;

            if (sprite != null)
            {
                _imgDefaultPreview.sprite = sprite;
                _imgDefaultPreview.gameObject.SetActive(true);
            }
            else
            {
                _imgDefaultPreview.gameObject.SetActive(false);
            }
        });
    }

    // Guide 패널 이미지 ID 기준으로 Addressable 비동기 로드
    private void LoadGuideImage(string imageId)
    {
        ReleaseGuideImage();

        if (_imgGuidePreview == null) return;

        if (string.IsNullOrEmpty(imageId))
        {
            _imgGuidePreview.gameObject.SetActive(false);
            return;
        }

        _imgGuidePreview.gameObject.SetActive(false);
        _currentGuideImageId = imageId;

        AddressableImageManager.Instance.LoadSprite(imageId, sprite =>
        {
            if (_imgGuidePreview == null) return;

            if (sprite != null)
            {
                _imgGuidePreview.sprite = sprite;
                _imgGuidePreview.preserveAspect = true;
                _imgGuidePreview.gameObject.SetActive(true);
            }
            else
            {
                _imgGuidePreview.gameObject.SetActive(false);
            }
        });
    }

    // Default 패널 이미지 해제
    private void ReleaseDefaultImage()
    {
        if (string.IsNullOrEmpty(_currentDefaultImageId)) return;

        AddressableImageManager.Instance.ReleaseSprite(_currentDefaultImageId);
        _currentDefaultImageId = null;
    }

    // Guide 패널 이미지 해제
    private void ReleaseGuideImage()
    {
        if (string.IsNullOrEmpty(_currentGuideImageId)) return;

        AddressableImageManager.Instance.ReleaseSprite(_currentGuideImageId);
        _currentGuideImageId = null;
    }

    // Shared
    private void ActivatePanel(GameObject panel)
    {
        SetAllPanels(false);
        if (panel != null) panel.SetActive(true);
    }

    private void SetAllPanels(bool active)
    {
        if (_panelSimple != null) _panelSimple.SetActive(active);
        if (_panelDefault != null) _panelDefault.SetActive(active);
        if (_panelGuide != null) _panelGuide.SetActive(active);
    }

    private void InvokePrimary(UIPopupRequest request)
    {
        if (request == null)
            return;

        if (!request.PrimaryInteractable)
            return;

        // 학생 선택 필요 → StudentSelectPopup으로 위임
        if (request.RequiresStudentSelection)
        {
            if (UIManager.Instance == null)
            {
                Debug.LogWarning("[UIPopup] UIManager가 없어 학생 선택을 열 수 없습니다.");
                return;
            }

            int max = Mathf.Max(0, request.MaxSelectCount);

            UIManager.Instance.OpenStudentSelect(
                maxSelectCount: max,
                onSelected: (students) => request.OnStudentsSelected?.Invoke(students),
                onCancelled: () => { },
                previewDelta: request.StudentCardPreviewDelta
            );

            // 학생 선택 UI를 띄운 뒤, 요청 옵션에 따라 Host 팝업을 닫음
            if (request.AutoCloseOnPrimary)
                CloseSelfByManager();

            return;
        }

        request.InvokePrimaryOnClose = false;
        request.OnPrimary?.Invoke();

        if (request.AutoCloseOnPrimary)
            CloseSelfByManager();
    }

    // Cancel 동작: Cancel 액션 호출 + 옵션에 따라 닫기
    private void InvokeCancel(UIPopupRequest request)
    {
        if (request == null)
            return;

        request.OnCancel?.Invoke();

        if (request.AutoCloseOnCancel)
            CloseSelfByManager();
    }
}