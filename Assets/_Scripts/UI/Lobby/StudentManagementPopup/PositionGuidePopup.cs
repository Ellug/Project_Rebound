using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// 포지션 안내 팝업 (페이지 넘김)
// - 패널은 유지, 내부 텍스트/이미지/버튼/점만 갱신
// - 버튼: 이전(전용) + 다음/닫기(공용 1개)
// - 점 표시: 현재 페이지는 검정 + 확대
public class PositionGuidePopup : UIBase
{
    [System.Serializable]
    public sealed class GuidePage
    {
        public string title;          // 제목
        [TextArea(3, 10)]
        public string content;        // 설명
        public Sprite image;          // 이미지(없으면 숨김)
    }

    [Header("UI")]
    [SerializeField] private TMP_Text _txtTitle;
    [SerializeField] private TMP_Text _txtContent;
    [SerializeField] private Image _img;

    [Header("Buttons")]
    [SerializeField] private Button _btnPrev;                 // 이전(전용)
    [SerializeField] private TMP_Text _txtPrevLabel;
    [SerializeField] private Button _btnNextOrClose;          // 다음/닫기(공용)
    [SerializeField] private TMP_Text _txtNextOrCloseLabel;

    [Header("Pages")]
    [SerializeField] private List<GuidePage> _pages = new();

    [Header("Page Dots")]
    [SerializeField] private Transform _dotRoot;              // 점 부모 (Grid/HorizontalLayoutGroup 등)
    [SerializeField] private Image _dotPrefab;                // 점 프리팹(Image)
    [SerializeField] private float _dotNormalScale = 1.0f;
    [SerializeField] private float _dotActiveScale = 1.4f;
    [SerializeField] private Color _dotNormalColor = new Color(0.75f, 0.75f, 0.75f, 1f);
    [SerializeField] private Color _dotActiveColor = Color.black;

    private readonly List<Image> _spawnedDots = new();

    private int _pageIndex;
    private bool _isInited;

    private void OnEnable()
    {
        // 씬에 비활성으로 두었다가 켜도 항상 정상 갱신
        if (!_isInited)
            Init();
        else
            Refresh();
    }

    public override void Init()
    {
        if (_isInited) return;
        _isInited = true;

        base.Init();

        // 이전 버튼은 고정 바인딩
        if (_btnPrev != null)
        {
            _btnPrev.onClick.RemoveAllListeners();
            _btnPrev.onClick.AddListener(Prev);
        }

        // Next/Close는 Refresh에서 페이지 상태에 따라 매번 바인딩
        _pageIndex = 0;

        EnsureDots();
        Refresh();
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

    private void CloseSelf()
    {
        Close(); // UIBase.Close() -> SetActive(false)
    }

    private void Refresh()
    {
        // 데이터 없으면 기본 문구 + 닫기만 노출
        if (_pages == null || _pages.Count == 0)
        {
            if (_txtTitle != null) _txtTitle.text = "포지션 안내";
            if (_txtContent != null) _txtContent.text = "페이지 데이터가 없습니다.";
            if (_img != null) _img.gameObject.SetActive(false);

            ApplyButtonState(isFirst: true, isLast: true);
            EnsureDots();
            RefreshDots();
            return;
        }

        // 인덱스 안전 보정
        _pageIndex = Mathf.Clamp(_pageIndex, 0, _pages.Count - 1);

        GuidePage page = _pages[_pageIndex];

        // 텍스트 갱신
        if (_txtTitle != null) _txtTitle.text = page.title;
        if (_txtContent != null) _txtContent.text = page.content;

        // 이미지 갱신
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

        bool isFirst = _pageIndex == 0;
        bool isLast = _pageIndex == _pages.Count - 1;

        ApplyButtonState(isFirst, isLast);
        EnsureDots();
        RefreshDots();
    }

    // 1페이지: 다음만 / 중간: 이전+다음 / 마지막: 이전+닫기
    private void ApplyButtonState(bool isFirst, bool isLast)
    {
        // 이전 버튼
        if (_btnPrev != null) _btnPrev.gameObject.SetActive(!isFirst);
        if (_txtPrevLabel != null) _txtPrevLabel.text = "이전";

        // 다음/닫기 공용 버튼
        if (_btnNextOrClose == null)
            return;

        _btnNextOrClose.onClick.RemoveAllListeners();

        if (isLast)
        {
            if (_txtNextOrCloseLabel != null) _txtNextOrCloseLabel.text = "닫기";
            _btnNextOrClose.onClick.AddListener(CloseSelf);
        }
        else
        {
            if (_txtNextOrCloseLabel != null) _txtNextOrCloseLabel.text = "다음";
            _btnNextOrClose.onClick.AddListener(Next);
        }
    }

    // pages 개수와 dot 개수를 맞춘다(필요할 때만 생성/삭제)
    private void EnsureDots()
    {
        if (_dotRoot == null || _dotPrefab == null)
            return;

        int targetCount = (_pages != null) ? _pages.Count : 0;

        // 부족하면 생성
        while (_spawnedDots.Count < targetCount)
        {
            Image dot = Instantiate(_dotPrefab, _dotRoot);
            dot.gameObject.SetActive(true);
            _spawnedDots.Add(dot);
        }

        // 많으면 삭제
        while (_spawnedDots.Count > targetCount)
        {
            int last = _spawnedDots.Count - 1;
            Image dot = _spawnedDots[last];
            _spawnedDots.RemoveAt(last);

            if (dot != null)
                Destroy(dot.gameObject);
        }
    }

    // 현재 페이지 점 강조(검정 + 확대)
    private void RefreshDots()
    {
        if (_spawnedDots.Count == 0)
            return;

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