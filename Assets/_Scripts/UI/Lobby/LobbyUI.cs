using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// 메인 로비 UI 관리
public class LobbyUI : UIBase
{
    [Header("Top Info")]
    [SerializeField] private TMP_Text _txtSchoolName;
    [SerializeField] private TMP_Text _txtDate;
    [SerializeField] private TMP_Text _txtDDay;
    [SerializeField] private TMP_Text _txtMoney;
    [SerializeField] private TMP_Text _txtFame;

    [Header("Top Right Buttons")]
    [SerializeField] private Button _btnLog;     // 로그 (기록)
    [SerializeField] private Button _btnSetting; // 설정

    [Header("Panels")]
    [SerializeField] private SettingsPanel _settingsPanelPrefab;

    [Header("Popups")]
    [SerializeField] private TrainingSelectPopup _trainingSelectPopup; // 씬에 배치된 훈련 선택 팝업 (직접 참조)
    [SerializeField] private StudentManagementPopup _studentManagementPopup; // 씬에 배치된 학생 관리 팝업(비활성화 상태)

    [Header("Center Message")]
    [SerializeField] private TMP_Text _txtMessage;
    [Header("Messenger")]
    [SerializeField] private Button _btnCenterMessage;                   
    [SerializeField] private MessengerInboxPopup _messengerInboxPopup;

    [Header("Bottom Navigation Buttons")]
    [SerializeField] private Button _btnTraining; // 훈련 (구 일과)
    [SerializeField] private Button _btnStudent;  // 학생 관리
    [SerializeField] private Button _btnFacility; // 시설 (MVP 개발 X)
    [SerializeField] private Button _btnCoach;    // 감독 노드 (MVP 개발 X)
    [SerializeField] private Button _btnShop;     // 상점 (MVP 개발 X)

    [Header("Test")]
    [SerializeField] private Sprite _testSprite;

    [SerializeField] private RecruitmentManager _recruitmentManager;

    private bool _inited;
    private bool _isLobbyInited;

    // 씬에 미리 배치된 경우 Start에서 초기화
    void Start()
    {
        Init();
    }

    public override void Init()
    {
        if (_isLobbyInited) return;
        _isLobbyInited = true;

        base.Init();
        BindEvents();
        UpdateUI(); // 초기 데이터 표시

        // 중앙 메시지 창 클릭 시 메신저함 열기
        if (_btnCenterMessage != null)
        {
            _btnCenterMessage.onClick.RemoveAllListeners();
            _btnCenterMessage.onClick.AddListener(OpenMessengerInbox);
        }

        // 메신저 매니저 구독 (새 메시지가 오면 중앙 텍스트 미리보기 갱신)
        if (MessengerManager.Instance != null)
        {
            MessengerManager.Instance.OnLatestMessageReceived -= UpdateMessagePreview;
            MessengerManager.Instance.OnLatestMessageReceived += UpdateMessagePreview;
        }
    }

    private void UpdateMessagePreview(ChatMessage latestMessage)
    {
        if (_txtMessage != null && latestMessage != null)
        {
            _txtMessage.text = $"[{latestMessage.SenderType}] {latestMessage.Content}";
        }
    }

    private void OpenMessengerInbox()
    {
        if (_messengerInboxPopup == null) return;

        _messengerInboxPopup.Init();
        _messengerInboxPopup.transform.SetAsLastSibling(); // 최상단 노출
        _messengerInboxPopup.Open(); // 내부에서 SetActive(true) 동작
    }

    private void BindEvents()
    {
        // 1. 상단 버튼
        if (_btnLog != null)
        {
            _btnLog.onClick.RemoveAllListeners(); // 혹시 모를 중복 방지
            _btnLog.onClick.AddListener(() =>
            {
                // [2] 이미지 + 서브텍스트가 포함된 팝업 데이터 생성
                var buttons = new List<PopupButtonInfo>
                {
                    new PopupButtonInfo("취소", null),
                    new PopupButtonInfo("확인", () => Debug.Log("이미지 팝업 확인됨"))
                };

                UIManager.Instance.ShowPopup(new PopupData(
                    title: "특수 훈련",
                    content: "이 훈련은 부상 위험이 높지만\n성장 속도가 매우 빠릅니다.",
                    subContent: "체력 소모 -30 / 부상 확률 10%", // 서브 텍스트
                    image: _testSprite,                     // 테스트 이미지
                    buttons: buttons
                ));
            });
        }
        if (_btnSetting != null)
        {
            _btnSetting.onClick.AddListener(() =>
            {
                Debug.Log("[LobbyUI] Setting button clicked");
                UIManager.Instance.ShowUI(_settingsPanelPrefab);
            });
        }

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
        if (_trainingSelectPopup == null)
            return;

        bool wasActive = _trainingSelectPopup.gameObject.activeSelf;

        CloseAllLobbyPopups();

        // 이미 열려있던 경우 → 토글로 닫기만 하고 종료
        if (wasActive)
            return;

        if (!_inited)
        {
            _trainingSelectPopup.Init();
            _inited = true;
        }

        _trainingSelectPopup.OnTrainingSelected -= HandleTrainingSelected;
        _trainingSelectPopup.OnTrainingSelected += HandleTrainingSelected;

        _trainingSelectPopup.transform.SetAsLastSibling();
        _trainingSelectPopup.Open();
        _trainingSelectPopup.ShowPage(0, false);
    }

    // 훈련 최종 선택 시 호출
    private void HandleTrainingSelected(string trainingKey)
    {
        Debug.Log($"[LobbyUI] 선택된 훈련: {trainingKey}");

        // 병합용: 훈련 선택을 실제 턴 실행 요청으로 연결한다.
        if (GameManager.Instance != null)
            GameManager.Instance.TryExecuteLobbyTurn(MapTrainingKeyToAction(trainingKey));
    }

    private void OnClickStudent()
    {
        if (_studentManagementPopup == null)
        {
            Debug.LogWarning("[LobbyUI] _studentManagementPopup이 null입니다.");
            return;
        }

        bool wasActive = _studentManagementPopup.gameObject.activeSelf;

        CloseAllLobbyPopups();

        // 이미 열려있던 경우 → 토글로 닫기만 하고 종료
        if (wasActive)
            return;

        _studentManagementPopup.Init();
        _studentManagementPopup.transform.SetAsLastSibling();
        _studentManagementPopup.Open();
    }

    // 데이터 매니저 등에서 정보를 받아와 UI 갱신
    private void ShowNotImplemented(string featureName)
    {
        UIManager.Instance.ShowPopup(new PopupData(
             title: "알림",
             content: $"{featureName} 기능은 아직 개발되지 않았습니다."
         ));
    }

    // 데이터 매니저에서 정보를 받아와 UI 갱신
    public void UpdateUI()
    {
        // 예시 데이터 바인딩
        if (_txtSchoolName) _txtSchoolName.text = "한울고등학교";
        if (_txtMoney) _txtMoney.text = "5000 G";
        if (_txtFame) _txtFame.text = "150";
        if (_txtMessage) _txtMessage.text = "감독님, 신입생들이 입학했습니다. 훈련 일정을 잡아주세요.";
    }

    // 턴 시스템이 계산한 현재 날짜/D-Day를 로비 상단에 반영
    public void UpdateDateAndDday(DateTime currentDate, int dDay)
    {
        if (_txtDate)
            _txtDate.text = currentDate.ToString("yyyy.MM.dd");

        if (_txtDDay)
            _txtDDay.text = dDay < 0 ? "D-?" : (dDay == 0 ? "D-DAY" : $"D-{dDay}");
    }

    // 이벤트/토너먼트 결과 메시지를 중앙 문구로 갱신
    public void SetStatusMessage(string message)
    {
        if (_txtMessage)
            _txtMessage.text = message;
    }

    private static TurnActionType MapTrainingKeyToAction(string trainingKey)
    {
        if (string.IsNullOrEmpty(trainingKey))
            return TurnActionType.Training;

        string loweredKey = trainingKey.ToLowerInvariant();
        if (loweredKey.Contains("personal")) return TurnActionType.PersonalTraining;
        if (loweredKey.Contains("counsel")) return TurnActionType.Counseling;
        if (loweredKey.Contains("rest")) return TurnActionType.Rest;

        return TurnActionType.Training;
    }

    // 팝업창 정리 - 팝업이 동시에 열리는 것을 방지
    private void CloseAllLobbyPopups()
    {
        if (_trainingSelectPopup != null && _trainingSelectPopup.gameObject.activeSelf)
        {
            _trainingSelectPopup.Close();
        }

        if (_studentManagementPopup != null && _studentManagementPopup.gameObject.activeSelf)
        {
            _studentManagementPopup.Close();
        }
    }
}
