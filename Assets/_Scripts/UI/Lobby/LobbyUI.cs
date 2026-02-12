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
    [SerializeField] private UIPopup _optionPopupPrefab; // 설정 팝업 프리팹
    // [SerializeField] private UIPopup _trainingPopupPrefab; // 훈련 팝업 (추후 추가)

    [Header("Center Message")]
    [SerializeField] private TMP_Text _txtMessage;

    [Header("Bottom Navigation Buttons")]
    [SerializeField] private Button _btnTraining; // 훈련 (구 일과)
    [SerializeField] private Button _btnStudent;  // 학생 관리
    [SerializeField] private Button _btnFacility; // 시설 (MVP 개발 X)
    [SerializeField] private Button _btnCoach;    // 감독 노드 (MVP 개발 X)
    [SerializeField] private Button _btnShop;     // 상점 (MVP 개발 X)

    public override void Init()
    {
        base.Init();
        BindEvents();
        UpdateUI(); // 초기 데이터 표시
    }

    private void BindEvents()
    {
        // 1. 상단 버튼
        _btnLog.onClick.AddListener(() => Debug.Log("Open Log Popup"));
        _btnSetting.onClick.AddListener(() =>
        {
            if (_optionPopupPrefab != null)
                UIManager.Instance.Show(_optionPopupPrefab);
            else
                Debug.LogWarning("설정 팝업 프리팹이 연결되지 않았습니다.");
        });

        // 2. 하단 네비게이션
        _btnTraining.onClick.AddListener(OnClickTraining);
        _btnStudent.onClick.AddListener(OnClickStudent);

        // MVP 미구현 기능들은 '준비중' 알림
        _btnFacility.onClick.AddListener(() => ShowNotImplemented("시설"));
        _btnCoach.onClick.AddListener(() => ShowNotImplemented("감독 노드"));
        _btnShop.onClick.AddListener(() => ShowNotImplemented("상점"));
    }

    private void OnClickTraining()
    {
        Debug.Log("훈련 팝업 열기");
        // UIManager.Instance.Show<Popup_Training>();
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