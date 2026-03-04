using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public sealed class GuidePanelController : MonoBehaviour
{
    [SerializeField] private TMP_Text _txtTitle;
    [SerializeField] private TMP_Text _txtSub;
    [SerializeField] private TMP_Text _txtMessage;
    [SerializeField] private Image _imgPreview;

    [SerializeField] private Button _btnCancel;
    [SerializeField] private Button _btnNext;
    [SerializeField] private Button _btnGuideClose;

    [Header("Dots")]
    [SerializeField] private Transform _dotRoot;
    [SerializeField] private Image _dotPrefab;
    [SerializeField] private float _dotNormalScale = 1.0f;
    [SerializeField] private float _dotActiveScale = 1.4f;
    [SerializeField] private Color _dotNormalColor = new Color(0.75f, 0.75f, 0.75f, 1f);
    [SerializeField] private Color _dotActiveColor = Color.white;

    private readonly List<Image> _spawnedDots = new List<Image>();
    private UIPopupRequest _request;
    private Action _closeSelf;
    private int _pageIndex;

    public void Bind(UIPopupRequest request, Action closeSelf)
    {
        _request = request;
        _closeSelf = closeSelf;
        _pageIndex = 0;

        if (_btnCancel != null)
        {
            _btnCancel.gameObject.SetActive(request.ShowCancel);
            _btnCancel.onClick.RemoveAllListeners();
            _btnCancel.onClick.AddListener(() =>
            {
                request.OnCancel?.Invoke();
                if (request.AutoCloseOnCancel)
                    _closeSelf?.Invoke();
            });
        }

        EnsureDots();
        Refresh();
    }

    private void Refresh()
    {
        List<UIPopupRequest.GuidePage> pages = _request != null ? _request.Pages : null;

        if (pages == null || pages.Count == 0)
        {
            if (_txtTitle != null) _txtTitle.text = _request != null ? (_request.Title ?? "") : "";
            if (_txtMessage != null) _txtMessage.text = _request != null ? (_request.Message ?? "") : "";
            if (_txtSub != null) _txtSub.gameObject.SetActive(false);
            if (_imgPreview != null) _imgPreview.gameObject.SetActive(false);

            ApplyButtonState(isLast: true);
            RefreshDots();
            return;
        }

        _pageIndex = Mathf.Clamp(_pageIndex, 0, pages.Count - 1);
        UIPopupRequest.GuidePage page = pages[_pageIndex];

        if (_txtTitle != null) _txtTitle.text = page.Title ?? "";
        if (_txtMessage != null) _txtMessage.text = page.Message ?? "";

        if (_txtSub != null)
        {
            bool hasSub = !string.IsNullOrEmpty(page.SubMessage);
            _txtSub.gameObject.SetActive(hasSub);
            if (hasSub) _txtSub.text = page.SubMessage;
        }

        if (_imgPreview != null)
        {
            bool hasSprite = page.PreviewSprite != null;
            _imgPreview.gameObject.SetActive(hasSprite);
            if (hasSprite)
            {
                _imgPreview.sprite = page.PreviewSprite;
                _imgPreview.preserveAspect = true;
            }
        }

        bool isLast = _pageIndex == pages.Count - 1;
        ApplyButtonState(isLast);

        EnsureDots();
        RefreshDots();
    }

    private void ApplyButtonState(bool isLast)
    {
        if (_btnNext != null)
        {
            _btnNext.gameObject.SetActive(!isLast);
            _btnNext.onClick.RemoveAllListeners();
            _btnNext.onClick.AddListener(NextPage);
        }

        if (_btnGuideClose != null)
        {
            _btnGuideClose.gameObject.SetActive(isLast);
            _btnGuideClose.onClick.RemoveAllListeners();
            _btnGuideClose.onClick.AddListener(() =>
            {
                _request?.OnPrimary?.Invoke();
                if (_request == null || _request.AutoCloseOnPrimary)
                    _closeSelf?.Invoke();
            });
        }
    }

    private void NextPage()
    {
        if (_request == null || _request.Pages == null || _request.Pages.Count == 0)
            return;

        _pageIndex = Mathf.Min(_request.Pages.Count - 1, _pageIndex + 1);
        Refresh();
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
}