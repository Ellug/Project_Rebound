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

        BuildCreditLines(_creditData.GetCreditLines());
        PlayBgm();

        if (_scrollCoroutine != null)
            StopCoroutine(_scrollCoroutine);

        _scrollCoroutine = StartCoroutine(CreditSequence());
    }

    private void BuildCreditLines(List<EndingCreditLine> lines)
    {
        foreach (Transform child in _creditContainer)
            Destroy(child.gameObject);

        float viewport = GetViewportHeight();
        float currentY = -viewport; // 화면 아래 밖에서 시작

        foreach (EndingCreditLine line in lines)
        {
            TMP_Text prefab = GetPrefabForType(line.Type);
            if (prefab == null)
            {
                currentY -= _lineSpacing; // Empty: 간격만 추가
                continue;
            }

            TMP_Text instance = Instantiate(prefab, _creditContainer);
            instance.text = line.Text;

            RectTransform rt = instance.rectTransform;
            rt.anchorMin = new Vector2(0.5f, 1f);
            rt.anchorMax = new Vector2(0.5f, 1f);
            rt.pivot = new Vector2(0.5f, 1f);
            rt.anchoredPosition = new Vector2(0f, currentY);

            float lineHeight = instance.preferredHeight > 0 ? instance.preferredHeight : _lineSpacing;
            currentY -= lineHeight + _lineSpacing * 1f;
        }

        _totalHeight = Mathf.Abs(currentY) + viewport;
        _creditContainer.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, _totalHeight);
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

    private IEnumerator CreditSequence()
    {
        _isSkipRequested = false;
        _isPlaying = true;

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