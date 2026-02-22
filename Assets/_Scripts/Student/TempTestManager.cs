//using UnityEngine;
//
//public class TempTestManager : MonoBehaviour
//{
//    [Header("테스트용 오브젝트 연결")]
//    public StudentCard testCard;
//    public StudentSlot testSlot;
//
//    // 현재 선택해서 손에 쥐고 있는 카드를 기억하는 변수
//    private StudentCard _selectedCard;
//
//    void Start()
//    {
//        // 1. 가짜 데이터 주입
//        Student dummyData = new Student
//        {
//            studentName = "서태웅",
//            grade = 1,
//            positionName = "SF",
//            stamina = 80,
//            shoot = 95,
//            condition = 90
//        };
//        testCard.SetStudentData(dummyData);
//
//        // 2. 이벤트 구독
//        testCard.OnCardClicked += HandleCardClicked;
//        testSlot.OnSlotClicked += HandleSlotClicked;
//    }
//
//    private void HandleCardClicked(StudentCard card)
//    {
//        // 동작 4: 이미 코트에 배치된 학생을 다시 터치했을 때 (배치 해제)
//        if (testSlot.AssignedStudent != null && testSlot.AssignedStudent.id == card.StudentData.id)
//        {
//            Debug.Log("[시스템] 배치를 해제하시겠습니까? -> (예 버튼을 눌렀다고 가정)");
//
//            testSlot.ClearSlot(); // 슬롯 비우기
//            card.SetViewState(StudentCard.CardViewState.Normal); // 카드에서 '배치 중' 패널 끄기
//            _selectedCard = null;
//        }
//        // 동작 2: 리스트에 있는 학생을 처음 터치했을 때
//        else
//        {
//            Debug.Log($"[시스템] {card.StudentData.studentName} 학생 선택됨! (하단에 정보 팝업이 떴다고 가정)");
//            _selectedCard = card; // 이 카드를 선택 상태로 기억
//        }
//    }
//
//    private void HandleSlotClicked(StudentSlot slot)
//    {
//        // 동작 3: 학생을 선택한 상태로 빈 슬롯을 터치했을 때 (배치 완료)
//        if (_selectedCard != null && slot.IsEmpty)
//        {
//            Debug.Log($"[시스템] {slot.Type} 슬롯에 {_selectedCard.StudentData.studentName} 배치 완료!");
//
//            slot.AssignStudent(_selectedCard.StudentData); // 슬롯에 데이터 전달
//
//            // 코트에 배치되었으므로 카드의 상태를 '배치 중'으로 변경!
//            _selectedCard.SetViewState(StudentCard.CardViewState.Placing);
//
//            _selectedCard = null; // 배치가 끝났으니 손을 비움
//        }
//        else if (slot.IsEmpty == false)
//        {
//            Debug.Log("[시스템] 이미 다른 학생이 배치된 슬롯입니다.");
//        }
//    }
//}