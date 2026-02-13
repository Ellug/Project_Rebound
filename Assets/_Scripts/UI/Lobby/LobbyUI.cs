using UnityEngine;
using UnityEngine.UI;
using TMPro;

// 메인 로비 UI 관리
public class LobbyUI : UIBase
{
    [Header("Top Left Info")]
    [SerializeField] private TMP_Text _txtSchoolName;
    [SerializeField] private TMP_Text _txtDate;
    [SerializeField] private TMP_Text _txtDDay;
    [SerializeField] private TMP_Text _txtMoney;
    [SerializeField] private TMP_Text _txtFame;

    [Header("Top Right Buttons")]
    [SerializeField] private Button _btnLog;     // 로그 (기록)
    [SerializeField] private Button _btnSetting; // 설정

    [Header("Popups")]
    [SerializeField] private UIPopup _optionPopupPrefab;              // 설정 팝업 프리팹
    [SerializeField] private TrainingSelectPopup _trainingPopupPrefab; // 훈련 선택 팝업 프리팹

    [Header("Center Message")]
    [SerializeField] private TMP_Text _txtMessage;

    [Header("Bottom Navigation Buttons")]
    [SerializeField] private Button _btnTraining; // 훈련 (구 일과)
    [SerializeField] private Button _btnStudent;  // 학생 관리
    [SerializeField] private Button _btnFacility; // 시설 (MVP 개발 X)
    [SerializeField] private Button _btnCoach;    // 감독 노드 (MVP 개발 X)
    [SerializeField] private Button _btnShop;     // 상점 (MVP 개발 X)

    // 씬에 미리 배치된 경우 Start에서 초기화
    void Start()
    {
        Init();
    }

    public override void Init()
    {
        base.Init();
        BindEvents();
        UpdateUI(); // 초기 데이터 표시
    }

    private void BindEvents()
    {
        // 1. 상단 버튼
        if (_btnLog != null)
            _btnLog.onClick.AddListener(() => Debug.Log("Open Log Popup"));
        if (_btnSetting != null)
            _btnSetting.onClick.AddListener(() =>
            {
                if (_optionPopupPrefab != null)
                    UIManager.Instance.Show(_optionPopupPrefab);
                else
                    Debug.LogWarning("설정 팝업 프리팹이 연결되지 않았습니다.");
            });

        // 2. 하단 네비게이션
        if (_btnTraining != null)
            _btnTraining.onClick.AddListener(OnClickTraining);
        if (_btnStudent != null)
            _btnStudent.onClick.AddListener(OnClickStudent);

        // MVP 미구현 기능들은 '준비중' 알림
        if (_btnFacility != null)
            _btnFacility.onClick.AddListener(() => ShowNotImplemented("시설"));
        if (_btnCoach != null)
            _btnCoach.onClick.AddListener(() => ShowNotImplemented("감독 노드"));
        if (_btnShop != null)
            _btnShop.onClick.AddListener(() => ShowNotImplemented("상점"));
    }

    private void OnClickTraining()
    {
        Debug.Log("[LobbyUI] OnClickTraining 호출됨");

        if (_trainingPopupPrefab == null)
        {
            Debug.LogError("[LobbyUI] _trainingPopupPrefab이 null입니다!");
            return;
        }

        Debug.Log("[LobbyUI] UIManager.Show 호출 시도");
        TrainingSelectPopup popup = UIManager.Instance.Show(_trainingPopupPrefab);

        if (popup == null)
        {
            Debug.LogError("[LobbyUI] UIManager.Show가 null을 반환했습니다!");
            return;
        }

        Debug.Log("[LobbyUI] 팝업 생성 성공, 이벤트 구독");
        popup.OnTrainingSelected += HandleTrainingSelected;
    }

    // 훈련 최종 선택 시 호출
    private void HandleTrainingSelected(string trainingKey)
    {
        Debug.Log($"[LobbyUI] 선택된 훈련: {trainingKey}");
        // TODO: TrainingManager를 통한 훈련 실행 처리
    }

    private void OnClickStudent()
    {
        Debug.Log("학생 관리 팝업 열기");
        // UIManager.Instance.Show<Popup_StudentManagement>();
    }

    private void ShowNotImplemented(string featureName)
    {
        Debug.LogWarning($"[MVP] {featureName} 기능은 아직 개발되지 않았습니다.");
        // 추후 Toast Message나 알림 팝업으로 대체 가능
    }

    // 데이터 매니저 등에서 정보를 받아와 UI 갱신
    public void UpdateUI()
    {
        // 예시 데이터 바인딩
        if (_txtSchoolName) _txtSchoolName.text = "한울고등학교";
        if (_txtDate) _txtDate.text = "2000.03.02";
        if (_txtDDay) _txtDDay.text = "D-100";
        if (_txtMoney) _txtMoney.text = "5000 G";
        if (_txtFame) _txtFame.text = "150";
        if (_txtMessage) _txtMessage.text = "감독님, 신입생들이 입학했습니다. 훈련 일정을 잡아주세요.";
    }
}
