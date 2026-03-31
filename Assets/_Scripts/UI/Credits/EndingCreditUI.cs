using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// 엔딩 크레딧 스크롤 UI 컨트롤러
public class EndingCreditUI : MonoBehaviour
{
    [Header("데이터")]
    [SerializeField] private EndingCreditDataSO _creditData;

    [Header("애니메이터")]
    [Tooltip("페이드 인/아웃 전담. 같은 GameObject 또는 자식에 배치.")]
    [SerializeField] private EndingCreditAnimator _animator;

    [Header("스크롤")]
    [Tooltip("크레딧 줄들이 생성되는 부모 RectTransform")]
    [SerializeField] private RectTransform _creditContainer;
    [SerializeField] private float _lineSpacing = 36f;

    [Header("줄 프리팹")]
    [SerializeField] private TMP_Text _sectionPrefab;  // 섹션 헤더 (대문자 굵게)
    [SerializeField] private TMP_Text _namePrefab;     // 이름
    [SerializeField] private TMP_Text _rolePrefab;     // 역할
    [SerializeField] private TMP_Text _specialPrefab;  // SPECIAL THANKS 본문 (미연결 시 _namePrefab 사용)

    [Header("이미지 프리팹")]
    [Tooltip("크레딧 하단 로고/이미지 줄에 사용할 Image 프리팹. 미연결 시 빈 GameObject에 Image를 자동 추가.")]
    [SerializeField] private Image _logoPrefab;

    [Tooltip("EndingCreditLine.ImageSize가 (0,0)일 때 적용되는 기본 크기 (px). x=폭, y=높이.")]
    [SerializeField] private Vector2 _defaultLogoSize = new Vector2(200f, 200f);

    [Header("버튼")]
    [SerializeField] private Button _btnSkip;

    [Header("배경")]
    [Tooltip("단색 검정 Image 권장")]
    [SerializeField] private Image _backgroundImage;

    private float _scrollSpeed;
    private float _totalDuration;
    private float _totalHeight;
    private float _viewportHeight;
    private bool _isSkipRequested;
    private bool _isPlaying;
    private Coroutine _scrollCoroutine;

    // EndingManager가 구독 — 크레딧 완료(자동/Skip) 시 발행
    public event Action OnCreditFinished;

    private void Awake()
    {
        if (_btnSkip != null)
        {
            _btnSkip.onClick.RemoveAllListeners();
            _btnSkip.onClick.AddListener(OnSkipClicked);
        }
    }

    // EndingManager에서 호출
    public void Play()
    {
        if (_creditData == null)
        {
            Debug.LogError("[EndingCreditUI] EndingCreditDataSO가 연결되지 않았습니다.");
            return;
        }

        _scrollSpeed = _creditData.ScrollSpeed;
        _totalDuration = _creditData.TotalDuration;

        PlayBgm();

        if (_scrollCoroutine != null)
            StopCoroutine(_scrollCoroutine);

        _scrollCoroutine = StartCoroutine(CreditSequence());
    }

    private IEnumerator CreditSequence()
    {
        _isSkipRequested = false;
        _isPlaying = true;

        // 줄 빌드 — 한 프레임 대기 후 TMP preferredHeight 확정
        yield return StartCoroutine(BuildCreditLinesAsync(_creditData.GetCreditLines()));

        yield return PlayAnimatorIn();

        _creditContainer.anchoredPosition = Vector2.zero;
        float scrolled = 0f;
        float elapsed = 0f;

        while (scrolled < _totalHeight && elapsed < _totalDuration && !_isSkipRequested)
        {
            float delta = _scrollSpeed * Time.deltaTime;
            scrolled += delta;
            elapsed += Time.deltaTime;

            _creditContainer.anchoredPosition = new Vector2(
                0f,
                _creditContainer.anchoredPosition.y + delta
            );

            yield return null;
        }

        yield return PlayAnimatorOut();

        _isPlaying = false;
        FinishCredit();
    }

    private IEnumerator BuildCreditLinesAsync(List<EndingCreditLine> lines)
    {
        foreach (Transform child in _creditContainer)
            Destroy(child.gameObject);

        yield return null; // Destroy 처리 대기

        float viewport = GetViewportHeight();
        float currentY = -viewport; // 화면 아래 밖에서 시작

        foreach (EndingCreditLine line in lines)
        {
            if (line.Type == EndingCreditLine.LineType.Empty)
            {
                currentY -= _lineSpacing;
                continue;
            }

            if (line.Type == EndingCreditLine.LineType.Logo)
            {
                currentY = PlaceLogoLine(line, currentY);
                continue;
            }

            TMP_Text prefab = GetPrefabForType(line.Type);
            if (prefab == null) continue;

            TMP_Text instance = Instantiate(prefab, _creditContainer);
            instance.text = line.Text;

            RectTransform rt = instance.rectTransform;
            rt.anchorMin = new Vector2(0.5f, 1f);
            rt.anchorMax = new Vector2(0.5f, 1f);
            rt.pivot = new Vector2(0.5f, 1f);
            rt.anchoredPosition = new Vector2(0f, currentY);

            // TMP preferredHeight는 레이아웃 갱신 전 0 반환 — 강제 갱신
            Canvas.ForceUpdateCanvases();
            float lineHeight = instance.preferredHeight > 0 ? instance.preferredHeight : _lineSpacing;
            currentY -= lineHeight + _lineSpacing * 0.3f;
        }

        _totalHeight = Mathf.Abs(currentY) + viewport;
        _creditContainer.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, _totalHeight);
    }

    // Logo 타입 줄을 컨테이너에 배치하고 다음 currentY를 반환
    // EndingCreditLine.ImageSize 규칙:
    //   (w, h) : 폭·높이 모두 고정
    //   (w, 0) : 폭 고정, 높이는 원본 비율로 자동 계산
    //   (0, h) : 높이 고정, 폭은 원본 비율로 자동 계산
    //   (0, 0) : _defaultLogoSize 사용
    private float PlaceLogoLine(EndingCreditLine line, float currentY)
    {
        if (line.Sprite == null) return currentY;

        Image instance;
        if (_logoPrefab != null)
        {
            instance = Instantiate(_logoPrefab, _creditContainer);
        }
        else
        {
            GameObject go = new GameObject("CreditLogo", typeof(RectTransform), typeof(Image));
            go.transform.SetParent(_creditContainer, false);
            instance = go.GetComponent<Image>();
        }

        instance.sprite = line.Sprite;
        instance.preserveAspect = true;
        instance.raycastTarget = false;

        float srcW = line.Sprite.rect.width;
        float srcH = line.Sprite.rect.height;
        float aspect = srcW / srcH;

        Vector2 size = (line.ImageSize == Vector2.zero) ? _defaultLogoSize : line.ImageSize;

        float width, height;

        if (size.x > 0f && size.y > 0f)
        {
            width = size.x;
            height = size.y;
        }
        else if (size.x > 0f)
        {
            width = size.x;
            height = width / aspect;
        }
        else if (size.y > 0f)
        {
            height = size.y;
            width = height * aspect;
        }
        else
        {
            width = srcW;
            height = srcH;
        }

        RectTransform rt = instance.rectTransform;
        rt.anchorMin = new Vector2(0.5f, 1f);
        rt.anchorMax = new Vector2(0.5f, 1f);
        rt.pivot = new Vector2(0.5f, 1f);
        rt.sizeDelta = new Vector2(width, height);
        rt.anchoredPosition = new Vector2(0f, currentY);

        return currentY - height - _lineSpacing * 0.3f;
    }

    private TMP_Text GetPrefabForType(EndingCreditLine.LineType type)
    {
        return type switch
        {
            EndingCreditLine.LineType.Section => _sectionPrefab,
            EndingCreditLine.LineType.Name => _namePrefab,
            EndingCreditLine.LineType.Role => _rolePrefab,
            EndingCreditLine.LineType.Special => _specialPrefab != null ? _specialPrefab : _namePrefab,
            _ => null
        };
    }

    private IEnumerator PlayAnimatorIn()
    {
        if (_animator == null) yield break;
        bool done = false;
        _animator.PlayIn(() => done = true);
        yield return new WaitUntil(() => done);
    }

    private IEnumerator PlayAnimatorOut()
    {
        if (_animator == null) yield break;
        bool done = false;
        _animator.PlayOut(() => done = true);
        yield return new WaitUntil(() => done);
    }

    private void OnSkipClicked()
    {
        if (_isSkipRequested || !_isPlaying) return;
        if (_animator != null && _animator.CurrentAlpha <= 0f) return;

        _isSkipRequested = true;

        if (_scrollCoroutine != null)
        {
            StopCoroutine(_scrollCoroutine);
            _scrollCoroutine = null;
        }

        StartCoroutine(SkipSequence());
    }

    private IEnumerator SkipSequence()
    {
        // Skip 버튼을 누르면 페이드 아웃 후 메인화면으로 이동 (확인창 없음)
        yield return PlayAnimatorOut();
        _isPlaying = false;
        FinishCredit();
    }

    private float GetViewportHeight()
    {
        if (_viewportHeight > 0f) return _viewportHeight;

        Canvas canvas = GetComponentInParent<Canvas>();
        _viewportHeight = (canvas != null && canvas.renderMode == RenderMode.ScreenSpaceOverlay)
            ? Screen.height
            : (transform.parent is RectTransform p ? p.rect.height : Screen.height);

        return _viewportHeight;
    }

    private void PlayBgm()
    {
        if (_creditData == null || SoundManager.Instance == null) return;
        SoundManager.Instance.PlayBGM(_creditData.BgmId);
    }

    private void FinishCredit()
    {
        OnCreditFinished?.Invoke();
    }
}