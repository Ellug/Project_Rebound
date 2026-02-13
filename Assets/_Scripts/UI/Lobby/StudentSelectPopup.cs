using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class StudentSelectPopup : UIPopup
{
    [Header("Scroll")]
    [SerializeField] private ScrollRect _scrollRect;

    [Header("Optional Close Button (if not using UIPopup closeButton)")]
    [SerializeField] private Button _btnClose;

    [Header("Card Spawn (later)")]
    [SerializeField] private Transform _cardRoot;     // Content(루트용 널 오브젝트)
    [SerializeField] private GameObject _cardPrefab;  // 학생 카드 프리팹(지금은 미사용)

    public override void Init()
    {
        base.Init();

        // 닫기 버튼이 UIPopup의 _closeButton을 안 쓰는 구조면 여기로 연결
        if (_btnClose != null)
        {
            _btnClose.onClick.RemoveAllListeners();
            _btnClose.onClick.AddListener(CloseSelf);
        }

        // 레이아웃 계산 이후 "맨 위"로 강제 고정 (초기 위 짤림 방지)
        StartCoroutine(ForceScrollTopRoutine());
    }

    // 정리 후 팝업 닫기
    protected override void OnCloseButtonClicked()
    {
        Close();
        Destroy(gameObject);
    }

    private void CloseSelf()
    {
        // UIManager 스택 방식으로 닫기
        if (UIManager.Instance != null)
        {
            UIManager.Instance.CloseTop();
            return;
        }

        // fallback
        Close();
        Destroy(gameObject);
    }

    private IEnumerator ForceScrollTopRoutine()
    {
        // 1프레임 대기: ContentSizeFitter/Grid 계산 타이밍 이슈 방지
        yield return null;
        Canvas.ForceUpdateCanvases();

        ForceScrollTop();

        // 한 프레임 더: Grid가 늦게 계산되는 케이스 안정화
        yield return null;
        Canvas.ForceUpdateCanvases();

        ForceScrollTop();
    }

    private void ForceScrollTop()
    {
        if (_scrollRect == null) return;

        _scrollRect.StopMovement();
        _scrollRect.verticalNormalizedPosition = 1f; // 맨 위
        _scrollRect.velocity = Vector2.zero;
    }

    


    public Transform CardRoot => _cardRoot;
    public GameObject CardPrefab => _cardPrefab;
}