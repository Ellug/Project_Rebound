using UnityEngine;
using UnityEngine.UI;

// [docs] 메인 로비 화면 및 기능 버튼 관리
public class LobbyUI : UIBase
{
    [Header("Lobby Menu Buttons")]
    [SerializeField] private Button _btnSchedule;    // 일과 시작 (훈련/휴식)
    [SerializeField] private Button _btnStudentMgmt; // 학생 관리 (드래그앤드롭)
    [SerializeField] private Button _btnFacility;    // 설비 (상점)
    [SerializeField] private Button _btnCoach;       // 감독 노드 (특성)

    public override void Init()
    {
        base.Init();
        BindEvents();
    }

    private void BindEvents()
    {
        // 각 버튼 클릭 시 해당 기능의 팝업을 열도록 연결
        // 아직 팝업 프리팹이 없으므로 로그만 찍거나 주석 처리

        _btnSchedule.onClick.AddListener(() =>
        {
            Debug.Log("Open Schedule Popup");
            // UIManager.Instance.Show<Popup_Schedule>("UI/Popup_Schedule"); 
        });

        _btnStudentMgmt.onClick.AddListener(() =>
        {
            Debug.Log("Open Student Management Popup");
            // UIManager.Instance.Show<Popup_Student>("UI/Popup_Student");
        });

        _btnFacility.onClick.AddListener(() => Debug.Log("Open Facility Popup"));
        _btnCoach.onClick.AddListener(() => Debug.Log("Open Coach Popup"));
    }

    // [docs] 로비에서 뒤로가기 키를 누르면 게임 종료 팝업 등을 띄움
    public override void OnBackKey()
    {
        Debug.Log("Show Game Exit Confirmation");
        // Popup_Exit 같은 확인창 띄우기
    }
}