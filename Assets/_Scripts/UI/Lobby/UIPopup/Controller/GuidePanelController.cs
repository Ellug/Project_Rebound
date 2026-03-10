using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public sealed class GuidePanelController : MonoBehaviour
{
    [SerializeField] private TMP_Text _txtTitle;                    // 페이지 제목
    [SerializeField] private TMP_Text _txtSub;                      // 페이지 서브
    [SerializeField] private TMP_Text _txtMessage;                  // 페이지 본문
    [SerializeField] private Image _imgPreview;                     // 페이지 이미지
                                                                    
    [SerializeField] private Button _btnCancel;                     // 취소 버튼
    [SerializeField] private Button _btnNext;                       // 다음 버튼
    [SerializeField] private Button _btnGuideClose;                 // 마지막 페이지 닫기 버튼
                                                                    
    [Header("Dots")]                                                
    [SerializeField] private Transform _dotRoot;                    // 도트 부모
    [SerializeField] private Image _dotPrefab;                      // 도트 프리팹
    [SerializeField] private float _dotNormalScale = 1.0f;          // 비활성 스케일
    [SerializeField] private float _dotActiveScale = 1.4f;          // 활성 스케일

    [SerializeField] private Color _dotNormalColor = new Color(0.75f, 0.75f, 0.75f, 1f);  // 비활성 색상
    [SerializeField] private Color _dotActiveColor = Color.white;                         // 활성화 색상

    private readonly List<Image> _spawnedDots = new List<Image>();  // 생성된 도트 캐시
    private UIPopupRequest _request;                                // 현재 요청 데이터
    private Action _closeSelf;                                      // AutoClose 처리를 위한 닫기 콜백
    private int _pageIndex;                                         // 현재 페이지 인덱스

    // 요청 데이터를 Guide 패널에 바인딩
    public void Bind(UIPopupRequest request, Action closeSelf)
    {
        _request = request;
        _closeSelf = closeSelf;
        _pageIndex = 0; // 항상 첫 페이지부터 시작

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

        EnsureDots(); // 페이지 수만큼 도트 준비
        Refresh();    // 첫 화면 갱신
    }

    // 현재 페이지 내용을 UI에 반영
    private void Refresh()
    {
        List<UIPopupRequest.GuidePage> pages = _request != null ? _request.Pages : null;

        // 페이지 데이터가 없으면 Title/Message로 폴백
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

        EnsureDots();  // 페이지 수 변경 대응
        RefreshDots(); // 현재 페이지 강조 표시
    }

    // 마지막 페이지 여부에 따라 Next/Close 토글
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

    // 다음 페이지로 이동
    private void NextPage()
    {
        if (_request == null || _request.Pages == null || _request.Pages.Count == 0)
            return;

        _pageIndex = Mathf.Min(_request.Pages.Count - 1, _pageIndex + 1);
        Refresh();
    }

    // 도트 개수를 페이지 수에 맞춰 생성/정리
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

    // 현재 페이지에 해당하는 도트를 강조 표시
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