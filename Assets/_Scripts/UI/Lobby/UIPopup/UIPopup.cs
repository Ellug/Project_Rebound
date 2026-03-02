using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;


// 범용 팝업 베이스 클래스
// PopupType에 따라 이미지/서브텍스트/페이지 기능을 선택적으로 활성화
public class UIPopup : UIPopupBase
{
    [Header("Panels")]
    [SerializeField] private GameObject _panelSimple;
    [SerializeField] private GameObject _panelDefault;
    [SerializeField] private GameObject _panelGuide;

    [Header("Simple UI")]
    [SerializeField] private TMP_Text _txtSimpleTitle;
    [SerializeField] private TMP_Text _txtSimpleMessage;
    [SerializeField] private Button _btnSimpleCancel;
    [SerializeField] private Button _btnSimpleConfirm;

    [Header("Default UI")]
    [SerializeField] private TMP_Text _txtDefaultTitle;
    [SerializeField] private TMP_Text _txtDefaultSub;
    [SerializeField] private TMP_Text _txtDefaultMessage;
    [SerializeField] private Image _imgDefaultPreview;

    [SerializeField] private Button _btnDefaultCancel;

    [SerializeField] private GameObject _defaultPrimaryConfirmRoot;
    [SerializeField] private Button _btnDefaultConfirm;

    [SerializeField] private GameObject _defaultPrimaryStartTrainingRoot;
    [SerializeField] private Button _btnDefaultStartTraining;

    [Header("Guide UI")]
    [SerializeField] private TMP_Text _txtGuideTitle;
    [SerializeField] private TMP_Text _txtGuideSub;
    [SerializeField] private TMP_Text _txtGuideMessage;
    [SerializeField] private Image _imgGuidePreview;

    [SerializeField] private Button _btnGuideCancel;

    [SerializeField] private Button _btnGuideNext;
    [SerializeField] private Button _btnGuideClose;

    [Header("Guide Dots")]
    [SerializeField] private Transform _dotRoot;
    [SerializeField] private Image _dotPrefab;
    [SerializeField] private float _dotNormalScale = 1.0f;
    [SerializeField] private float _dotActiveScale = 1.4f;
    [SerializeField] private Color _dotNormalColor = new Color(0.75f, 0.75f, 0.75f, 1f);
    [SerializeField] private Color _dotActiveColor = Color.white;

    private readonly List<Image> _spawnedDots = new();

    private UIPopupRequest _request;
    private int _pageIndex;

    public override void Init()
    {
        base.Init();
        SetAllPanels(false);
    }

    public void Setup(UIPopupRequest request)
    {
        _request = request;

        if (_request == null)
            return;

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

    public override void Close()
    {
        TryInvokePrimaryOnClose();
        base.Close();
    }

    private void TryInvokePrimaryOnClose()
    {
        if (_request == null)
            return;

        if (!_request.InvokePrimaryOnClose)
            return;

        if (_request.RequiresStudentSelection)
            return;

        _request.OnPrimary?.Invoke();
    }

    private void SetupSimple(UIPopupRequest request)
    {
        ActivatePanel(_panelSimple);

        if (_txtSimpleTitle != null) _txtSimpleTitle.text = request.Title ?? "";
        if (_txtSimpleMessage != null) _txtSimpleMessage.text = request.Message ?? "";

        if (_btnSimpleCancel != null)
        {
            _btnSimpleCancel.gameObject.SetActive(request.ShowCancel);
            _btnSimpleCancel.onClick.RemoveAllListeners();
            _btnSimpleCancel.onClick.AddListener(() =>
            {
                request.OnCancel?.Invoke();
                if (request.AutoCloseOnCancel)
                    CloseSelfByManager();
            });
        }

        if (_btnSimpleConfirm != null)
        {
            _btnSimpleConfirm.gameObject.SetActive(true);
            _btnSimpleConfirm.interactable = request.PrimaryInteractable;
            _btnSimpleConfirm.onClick.RemoveAllListeners();
            _btnSimpleConfirm.onClick.AddListener(() => InvokePrimary(request));
        }
    }

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

        if (_imgDefaultPreview != null)
        {
            bool hasSprite = request.PreviewSprite != null;
            _imgDefaultPreview.gameObject.SetActive(hasSprite);
            if (hasSprite)
            {
                _imgDefaultPreview.sprite = request.PreviewSprite;
                _imgDefaultPreview.preserveAspect = true;
            }
        }

        if (_btnDefaultCancel != null)
        {
            _btnDefaultCancel.gameObject.SetActive(request.ShowCancel);
            _btnDefaultCancel.onClick.RemoveAllListeners();
            _btnDefaultCancel.onClick.AddListener(() =>
            {
                request.OnCancel?.Invoke();
                if (request.AutoCloseOnCancel)
                    CloseSelfByManager();
            });
        }

        ApplyDefaultPrimaryKind(request);
    }

    private void ApplyDefaultPrimaryKind(UIPopupRequest request)
    {
        if (_defaultPrimaryConfirmRoot != null)
            _defaultPrimaryConfirmRoot.SetActive(request.PrimaryKind == UIPopupRequest.PrimaryButtonKind.Confirm);

        if (_defaultPrimaryStartTrainingRoot != null)
            _defaultPrimaryStartTrainingRoot.SetActive(request.PrimaryKind == UIPopupRequest.PrimaryButtonKind.StartTraining);

        if (_btnDefaultConfirm != null)
        {
            _btnDefaultConfirm.interactable = request.PrimaryInteractable;
            _btnDefaultConfirm.onClick.RemoveAllListeners();
            _btnDefaultConfirm.onClick.AddListener(() => InvokePrimary(request));
        }

        if (_btnDefaultStartTraining != null)
        {
            _btnDefaultStartTraining.interactable = request.PrimaryInteractable;
            _btnDefaultStartTraining.onClick.RemoveAllListeners();
            _btnDefaultStartTraining.onClick.AddListener(() => InvokePrimary(request));
        }
    }

    private void SetupGuide(UIPopupRequest request)
    {
        ActivatePanel(_panelGuide);

        _pageIndex = 0;

        if (_btnGuideCancel != null)
        {
            _btnGuideCancel.gameObject.SetActive(request.ShowCancel);
            _btnGuideCancel.onClick.RemoveAllListeners();
            _btnGuideCancel.onClick.AddListener(() =>
            {
                request.OnCancel?.Invoke();
                if (request.AutoCloseOnCancel)
                    CloseSelfByManager();
            });
        }

        EnsureDots();
        RefreshGuidePage();
    }

    private void RefreshGuidePage()
    {
        List<UIPopupRequest.GuidePage> pages = _request != null ? _request.Pages : null;

        if (pages == null || pages.Count == 0)
        {
            if (_txtGuideTitle != null) _txtGuideTitle.text = _request != null ? (_request.Title ?? "") : "";
            if (_txtGuideMessage != null) _txtGuideMessage.text = _request != null ? (_request.Message ?? "") : "";
            if (_txtGuideSub != null) _txtGuideSub.gameObject.SetActive(false);
            if (_imgGuidePreview != null) _imgGuidePreview.gameObject.SetActive(false);

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

        if (_imgGuidePreview != null)
        {
            bool hasSprite = page.PreviewSprite != null;
            _imgGuidePreview.gameObject.SetActive(hasSprite);
            if (hasSprite)
            {
                _imgGuidePreview.sprite = page.PreviewSprite;
                _imgGuidePreview.preserveAspect = true;
            }
        }

        bool isLast = _pageIndex == pages.Count - 1;
        ApplyGuideButtonState(isLast);
        EnsureDots();
        RefreshDots();
    }

    private void ApplyGuideButtonState(bool isLast)
    {
        if (_btnGuideNext != null)
        {
            _btnGuideNext.gameObject.SetActive(!isLast);
            _btnGuideNext.onClick.RemoveAllListeners();
            _btnGuideNext.onClick.AddListener(NextGuidePage);
        }

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

        while (_spawnedDots.Count < targetCount)
        {
            Image dot = Instantiate(_dotPrefab, _dotRoot);
            dot.gameObject.SetActive(true);
            _spawnedDots.Add(dot);
        }

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
                onSelected: (students) =>
                {
                    request.OnStudentsSelected?.Invoke(students);
                },
                onCancelled: () => { }
            );

            if (request.AutoCloseOnPrimary)
                CloseSelfByManager();

            return;
        }

        request.OnPrimary?.Invoke();

        if (request.AutoCloseOnPrimary)
            CloseSelfByManager();
    }
}