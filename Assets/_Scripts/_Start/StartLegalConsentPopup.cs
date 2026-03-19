using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class StartLegalConsentPopup : MonoBehaviour
{
    public const string ConsentPrefKey = "start_legal_consent_20260319";
    private const string PrivacyPolicyResourcePath = "Legal/privacy_policy_ko";
    private const string TermsRefundResourcePath = "Legal/terms_refund_policy_ko";

    [Header("Root")]
    [SerializeField] private GameObject _popupRoot;

    [Header("Documents (Optional Override)")]
    [SerializeField] private TextAsset _privacyPolicyDocument;
    [SerializeField] private TextAsset _termsRefundDocument;

    [Header("Toggle Buttons")]
    [SerializeField] private Button _privacyPolicyToggleButton;
    [SerializeField] private Button _termsRefundToggleButton;
    [SerializeField] private Image _privacyPolicyToggleImage;
    [SerializeField] private Image _termsRefundToggleImage;

    [Header("Toggle Colors")]
    [SerializeField] private Color _toggleSelectedColor = new(0.16f, 0.56f, 0.24f, 1f);
    [SerializeField] private Color _toggleUnselectedColor = new(0.25f, 0.25f, 0.25f, 1f);

    [Header("Detail Root")]
    [SerializeField] private ScrollRect _detailScrollRect;
    [SerializeField] private TMP_Text _detailText;

    [Header("Buttons")]
    [SerializeField] private Button _cancelButton;
    [SerializeField] private Button _agreeButton;

    public event Action OnCancel;
    public event Action OnAgree;

    private bool _listenersBound;
    private DetailTab _currentTab;

    private enum DetailTab
    {
        PrivacyPolicy,
        TermsRefund
    }

    private void Awake()
    {
        BindListeners();
        EnsureDocuments();
        ApplyDocumentTexts();
        SetPopupActive(false);
    }

    private void OnDestroy()
    {
        UnbindListeners();
    }

    public static bool HasConsent()
    {
        return PlayerPrefs.GetInt(ConsentPrefKey, 0) == 1;
    }

    public static void SaveConsent(bool consented)
    {
        PlayerPrefs.SetInt(ConsentPrefKey, consented ? 1 : 0);
        PlayerPrefs.Save();
    }

    public void Show()
    {
        if (!_listenersBound)
            BindListeners();

        SetPopupActive(true);
        EnsureDocuments();
        SetDefaultToggleState();
    }

    public void Hide()
    {
        SetPopupActive(false);
    }

    private void BindListeners()
    {
        if (_listenersBound)
            return;

        _privacyPolicyToggleButton.onClick.AddListener(HandlePrivacyPolicyToggleClicked);
        _termsRefundToggleButton.onClick.AddListener(HandleTermsRefundToggleClicked);
        _cancelButton.onClick.AddListener(HandleCancelClicked);
        _agreeButton.onClick.AddListener(HandleAgreeClicked);

        _listenersBound = true;
    }

    private void UnbindListeners()
    {
        if (!_listenersBound)
            return;

        _privacyPolicyToggleButton.onClick.RemoveListener(HandlePrivacyPolicyToggleClicked);
        _termsRefundToggleButton.onClick.RemoveListener(HandleTermsRefundToggleClicked);
        _cancelButton.onClick.RemoveListener(HandleCancelClicked);
        _agreeButton.onClick.RemoveListener(HandleAgreeClicked);

        _listenersBound = false;
    }

    private void EnsureDocuments()
    {
        if (_privacyPolicyDocument == null)
            _privacyPolicyDocument = Resources.Load<TextAsset>(PrivacyPolicyResourcePath);

        if (_termsRefundDocument == null)
            _termsRefundDocument = Resources.Load<TextAsset>(TermsRefundResourcePath);
    }

    private void ApplyDocumentTexts()
    {
        ApplyDetailVisibility();
    }

    private void SetDefaultToggleState()
    {
        _currentTab = DetailTab.PrivacyPolicy;
        ApplyDetailVisibility();
    }

    private void HandlePrivacyPolicyToggleClicked()
    {
        _currentTab = DetailTab.PrivacyPolicy;
        ApplyDetailVisibility();
    }

    private void HandleTermsRefundToggleClicked()
    {
        _currentTab = DetailTab.TermsRefund;
        ApplyDetailVisibility();
    }

    private void ApplyDetailVisibility()
    {
        bool showPrivacyPolicy = _currentTab == DetailTab.PrivacyPolicy;

        _detailText.text = showPrivacyPolicy
            ? (_privacyPolicyDocument != null ? _privacyPolicyDocument.text : "개인정보 처리방침 문서를 찾을 수 없습니다.")
            : (_termsRefundDocument != null ? _termsRefundDocument.text : "이용약관 및 환불 정책 문서를 찾을 수 없습니다.");
        Canvas.ForceUpdateCanvases();
        _detailScrollRect.verticalNormalizedPosition = 1f;

        _privacyPolicyToggleImage.color = showPrivacyPolicy ? _toggleSelectedColor : _toggleUnselectedColor;
        _termsRefundToggleImage.color = showPrivacyPolicy ? _toggleUnselectedColor : _toggleSelectedColor;
    }

    private void HandleCancelClicked()
    {
        OnCancel?.Invoke();
    }

    private void HandleAgreeClicked()
    {
        OnAgree?.Invoke();
    }

    private void SetPopupActive(bool active)
    {
        _popupRoot.SetActive(active);
    }
}
