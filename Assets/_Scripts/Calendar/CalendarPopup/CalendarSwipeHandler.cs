using System;
using UnityEngine;
using UnityEngine.EventSystems;

// 캘린더 영역의 좌우 스와이프 입력을 감지하는 컴포넌트
public class CalendarSwipeHandler : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    [SerializeField] private float _swipeMinDist = 60f;      // 인식 최소 수평 거리
    [SerializeField] private float _swipeMaxVertRatio = 0.8f; // 수직 비율 초과 시 무효

    public event Action OnSwipeLeft;
    public event Action OnSwipeRight;

    private Vector2 _dragStartPos;

    public void OnBeginDrag(PointerEventData eventData)
    {
        _dragStartPos = eventData.position;
    }

    public void OnDrag(PointerEventData eventData)
    {
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        Vector2 delta = eventData.position - _dragStartPos;

        if (Mathf.Abs(delta.x) <= 0f)
            return;

        // 세로 이동이 더 크면 스와이프로 보지 않음
        if (Mathf.Abs(delta.y) / Mathf.Abs(delta.x) >= _swipeMaxVertRatio)
            return;

        if (Mathf.Abs(delta.x) < _swipeMinDist)
            return;

        if (delta.x < 0f)
            OnSwipeLeft?.Invoke();   // 왼쪽 스와이프 = 다음 달
        else
            OnSwipeRight?.Invoke();  // 오른쪽 스와이프 = 이전 달
    }
}