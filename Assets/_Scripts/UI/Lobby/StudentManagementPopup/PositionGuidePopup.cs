using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// 포지션 안내 팝업(이전/다음/닫기)
// 이벤트 팝업 용도: 여러 페이지 설명을 넘기는 구조
public class PositionGuidePopup : UIBase
{
    [System.Serializable]
    public sealed class GuidePage
    {
        public string title;
        [TextArea(3, 10)]
        public string content;
        public Sprite image;
    }

    [Header("UI")]
    [SerializeField] private TMP_Text _txtTitle;
    [SerializeField] private TMP_Text _txtContent;
    [SerializeField] private Image _img;

    [Header("Buttons")]
    [SerializeField] private Button _btnPrev;
    [SerializeField] private Button _btnNext;
    [SerializeField] private Button _btnClose;

    [Header("Pages")]
    [SerializeField] private List<GuidePage> _pages = new();

    private int _pageIndex;
    private bool _isInited;

    public override void Init()
    {
        if (_isInited) return;
        _isInited = true;

        base.Init();

        if (_btnPrev != null)
        {
            _btnPrev.onClick.RemoveAllListeners();
            _btnPrev.onClick.AddListener(Prev);
        }

        if (_btnNext != null)
        {
            _btnNext.onClick.RemoveAllListeners();
            _btnNext.onClick.AddListener(Next);
        }

        if (_btnClose != null)
        {
            _btnClose.onClick.RemoveAllListeners();
            _btnClose.onClick.AddListener(CloseSelf);
        }

        _pageIndex = 0;
        Refresh();
    }

    public override void Open()
    {
        base.Open();

        if (!_isInited)
            Init();

        Refresh();
    }

    private void CloseSelf()
    {
        Close();
    }

    private void Prev()
    {
        if (_pages == null || _pages.Count == 0)
            return;

        _pageIndex = Mathf.Max(0, _pageIndex - 1);
        Refresh();
    }

    private void Next()
    {
        if (_pages == null || _pages.Count == 0)
            return;

        _pageIndex = Mathf.Min(_pages.Count - 1, _pageIndex + 1);
        Refresh();
    }

    private void Refresh()
    {
        if (_pages == null || _pages.Count == 0)
        {
            if (_txtTitle != null) _txtTitle.text = "포지션 안내";
            if (_txtContent != null) _txtContent.text = "페이지 데이터가 없습니다.";
            if (_img != null) _img.gameObject.SetActive(false);

            if (_btnPrev != null) _btnPrev.gameObject.SetActive(false);
            if (_btnNext != null) _btnNext.gameObject.SetActive(false);
            return;
        }

        GuidePage page = _pages[_pageIndex];

        if (_txtTitle != null) _txtTitle.text = page.title;
        if (_txtContent != null) _txtContent.text = page.content;

        if (_img != null)
        {
            bool has = page.image != null;
            _img.gameObject.SetActive(has);
            if (has)
            {
                _img.sprite = page.image;
                _img.preserveAspect = true;
            }
        }

        if (_btnPrev != null) _btnPrev.gameObject.SetActive(_pageIndex > 0);
        if (_btnNext != null) _btnNext.gameObject.SetActive(_pageIndex < _pages.Count - 1);
    }
}