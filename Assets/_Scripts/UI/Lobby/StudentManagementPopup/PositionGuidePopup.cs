using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// 포지션 안내 팝업 (페이지 넘김 구조)
public class PositionGuidePopup : UIBase
{
    // 1페이지 데이터 구조
    [System.Serializable]
    public sealed class GuidePage
    {
        public string title;          // 제목
        [TextArea(3, 10)]
        public string content;        // 설명 내용
        public Sprite image;          // 이미지
    }

    [Header("UI")]
    [SerializeField] private TMP_Text _txtTitle;    // 제목 텍스트
    [SerializeField] private TMP_Text _txtContent;  // 내용 텍스트
    [SerializeField] private Image _img;            // 이미지

    [Header("Buttons")]
    [SerializeField] private Button _btnPrev;       // 이전 버튼
    [SerializeField] private Button _btnNext;       // 다음 버튼
    [SerializeField] private Button _btnClose;      // 닫기 버튼

    [Header("Pages")]
    [SerializeField] private List<GuidePage> _pages = new(); // 페이지 목록

    private int _pageIndex;     // 현재 페이지 인덱스
    private bool _isInited;

    public override void Init()
    {
        if (_isInited) return;
        _isInited = true;

        base.Init();

        // 버튼 이벤트 바인딩
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

        // Init 누락 방지
        if (!_isInited)
            Init();

        Refresh();
    }

    // 팝업 닫기
    private void CloseSelf()
    {
        Close();
    }

    // 이전 페이지
    private void Prev()
    {
        if (_pages == null || _pages.Count == 0)
            return;

        _pageIndex = Mathf.Max(0, _pageIndex - 1);
        Refresh();
    }

    // 다음 페이지
    private void Next()
    {
        if (_pages == null || _pages.Count == 0)
            return;

        _pageIndex = Mathf.Min(_pages.Count - 1, _pageIndex + 1);
        Refresh();
    }

    // 현재 페이지 UI 갱신
    private void Refresh()
    {
        if (_pages == null || _pages.Count == 0)
        {
            // 페이지 데이터 없음 처리
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

        // 이미지 표시 여부
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

        // 버튼 활성 조건
        if (_btnPrev != null) _btnPrev.gameObject.SetActive(_pageIndex > 0);
        if (_btnNext != null) _btnNext.gameObject.SetActive(_pageIndex < _pages.Count - 1);
    }
}