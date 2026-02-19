using UnityEngine;
using UnityEngine.EventSystems;

public class StudentSlot : MonoBehaviour, IDropHandler
{
    public enum SlotType
    {
        WaitList,
        StartingMember,
        SubMember
    }

    [SerializeField] private SlotType _slotType;

    public void OnDrop(PointerEventData eventData)
    {
        if (eventData.pointerDrag == null)
        {
            return;
        }

        StudentCard card = eventData.pointerDrag.GetComponent<StudentCard>();

        if (card != null)
        {
            AcceptCard(card);
        }
    }

    private void AcceptCard(StudentCard card)
    {
        // 현재 슬롯에 이미 다른 카드가 있는지 확인
        if (transform.childCount > 0)
        {
            // 빈 자리가 없을 경우 드롭을 거부하고 원래 위치로 돌아가게 함
            Debug.Log("[StudentSlot] Slot is already full.");
            return;
        }

        // 카드의 부모를 해당 슬롯으로 변경
        card.transform.SetParent(this.transform);

        // 카드 위치를 슬롯 정중앙으로 정렬
        RectTransform cardRect = card.GetComponent<RectTransform>();
        if (cardRect != null)
        {
            cardRect.anchoredPosition = Vector2.zero;
        }

        string studentName = card.StudentData != null ? card.StudentData.studentName : "Unknown";
        Debug.Log($"[StudentSlot] {studentName} moved to {_slotType}");
    }
}