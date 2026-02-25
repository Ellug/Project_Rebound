using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// 튜토리얼 가이드 팝업 (페이지형 UI)
public class TutorialGuidePopup : UIBase
{
    [System.Serializable]
    // 1페이지에 해당하는 데이터 구조
    public sealed class GuidePage
    {
        public string title;
        [TextArea(3, 10)]
        public string content;
        public Sprite image; // 선택 이미지 (없으면 null)
    }

    [Header("UI")]
    [SerializeField] private TMP_Text _txtTitle;
    [SerializeField] private TMP_Text _txtContent;
    [SerializeField] private Image _img;

    [Header("Buttons")]
    [SerializeField] private Button _btnPrev;
    [SerializeField] private TMP_Text _txtPrevLabel;
    [SerializeField] private Button _btnNextOrClose; // 마지막 페이지면 닫기 역할
    [SerializeField] private TMP_Text _txtNextOrCloseLabel;

    [Header("Page Dots")]
    [SerializeField] private Transform _dotRoot; // 점 표시 부모
    [SerializeField] private Image _dotPrefab;   // 점 프리팹
    [SerializeField] private float _dotNormalScale = 1.0f;
    [SerializeField] private float _dotActiveScale = 1.4f;
    [SerializeField] private Color _dotNormalColor = new Color(0.75f, 0.75f, 0.75f, 1f);
    [SerializeField] private Color _dotActiveColor = Color.black;

    private readonly List<Image> _spawnedDots = new(); // 생성된 점 캐시
    private readonly List<GuidePage> _pages = new();   // 페이지 데이터

    private int _pageIndex; // 현재 페이지 인덱스
    private bool _isInited;

    private void OnEnable()
    {
        // 최초 1회 Init, 이후엔 Refresh만 수행
        if (!_isInited) Init();
        else Refresh();
    }

    public override void Init()
    {
        if (_isInited) return;
        _isInited = true;

        base.Init();

        // 이전 버튼 바인딩
        if (_btnPrev != null)
        {
            _btnPrev.onClick.RemoveAllListeners();
            _btnPrev.onClick.AddListener(Prev);
        }

        // 테이블 기반 페이지 생성
        BuildPagesFromTable();
        _pageIndex = 0;

        EnsureDots();
        Refresh();
    }

    // SO 테이블 데이터를 GuidePage 리스트로 변환
    private void BuildPagesFromTable()
    {
        _pages.Clear();

        TutorialGuideTableSO table = CachedSOData.TutorialGuideTable;
        if (table == null || table.Rows == null || table.Rows.Count == 0)
            return;

        for (int i = 0; i < table.Rows.Count; i++)
        {
            var row = table.Rows[i];
            if (row == null) continue;

            _pages.Add(new GuidePage
            {
                title = row.titleText,
                content = row.desc,
                image = null // 프로토타입 단계: 이미지 미사용
            });
        }
    }

    // 이전 페이지 이동
    private void Prev()
    {
        if (_pages.Count == 0) return;
        _pageIndex = Mathf.Max(0, _pageIndex - 1);
        Refresh();
    }

    // 다음 페이지 이동
    private void Next()
    {
        if (_pages.Count == 0) return;
        _pageIndex = Mathf.Min(_pages.Count - 1, _pageIndex + 1);
        Refresh();
    }

    // 팝업 닫기
    private void CloseSelf()
    {
        Close();
    }

    // 현재 페이지 기준 UI 전체 갱신
    private void Refresh()
    {
        if (_pages.Count == 0)
        {
            // 데이터가 없을 때 기본 메시지
            if (_txtTitle != null) _txtTitle.text = "튜토리얼 가이드";
            if (_txtContent != null) _txtContent.text = "페이지 데이터가 없습니다.";
            if (_img != null) _img.gameObject.SetActive(false);

            ApplyButtonState(isFirst: true, isLast: true);
            EnsureDots();
            RefreshDots();
            return;
        }

        _pageIndex = Mathf.Clamp(_pageIndex, 0, _pages.Count - 1);
        GuidePage page = _pages[_pageIndex];

        // 텍스트 갱신
        if (_txtTitle != null) _txtTitle.text = page.title;
        if (_txtContent != null) _txtContent.text = page.content;

        // 이미지 표시 여부 처리
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

    // 버튼 상태(이전 / 다음 / 닫기) 적용
    private void ApplyButtonState(bool isFirst, bool isLast)
    {
        if (_btnPrev != null) _btnPrev.gameObject.SetActive(!isFirst);
        if (_txtPrevLabel != null) _txtPrevLabel.text = "이전";

        if (_btnNextOrClose == null) return;

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

    // 페이지 수에 맞게 점 개수 조정
    private void EnsureDots()
    {
        if (_dotRoot == null || _dotPrefab == null) return;

        int targetCount = _pages.Count;

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
}