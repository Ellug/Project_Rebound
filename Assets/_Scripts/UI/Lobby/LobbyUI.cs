using System;
using System.Collections.Generic;
using TMPro;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;

// 메인 로비 UI 관리
public class LobbyUI : UIBase
{
    [Serializable]
    private sealed class BottomTabActiveSpriteSet
    {
        public Sprite training;
        public Sprite student;
        public Sprite facility;
        public Sprite coach;
        public Sprite shop;
    }

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
    [SerializeField] private HeadCoachPopup _headCoachPopup; // 씬에 배치된 감독 노드 팝업 (비활성화 상태)
    [SerializeField] private FacilityPopup _facilityPopup; // 씬에 배치된 감독 노드 팝업 (비활성화 상태)
    [SerializeField] private AlwaysEffectPopup _alwaysEffectPopup; // 씬에 배치된 상시 효과 확인 팝업 (비활성화 상태)

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
    [SerializeField] private Button _btnEffectIcon; // 현재 적용 중인 상시 효과 확인
    [SerializeField] private BottomTabActiveSpriteSet _activeTabSprites; // 탭 활성 시 교체할 스프라이트

    [SerializeField] private RecruitmentManager _recruitmentManager;

    private bool _inited;
    private bool _isLobbyInited;
    private Sprite _trainingDefaultSprite;
    private Sprite _studentDefaultSprite;
    private Sprite _facilityDefaultSprite;
    private Sprite _coachDefaultSprite;
    private Sprite _shopDefaultSprite;
    private bool _lastTrainingPopupActive;
    private bool _lastStudentPopupActive;
    private bool _lastCoachPopupActive;
    private bool _lastFacilityPopupActive;

    // 씬에 미리 배치된 경우 Start에서 초기화
    void Start()
    {
        Init();
    }

    // 팝업 활성 상태가 외부에서 바뀌었을 때도 탭 이미지를 동기화
    void LateUpdate()
    {
        if (!_isLobbyInited)
            return;

        bool trainingActive = IsPopupActive(_trainingSelectPopup);
        bool studentActive = IsPopupActive(_studentManagementPopup);
        bool coachActive = IsPopupActive(_headCoachPopup);
        bool facilityActive = IsPopupActive(_facilityPopup);

        if (trainingActive == _lastTrainingPopupActive
            && studentActive == _lastStudentPopupActive
            && coachActive == _lastCoachPopupActive
            && facilityActive == _lastFacilityPopupActive)
            return;

        RefreshBottomNavTabSprites();
    }

    public override void Init()
    {
        if (_isLobbyInited) return;
        _isLobbyInited = true;

        base.Init();
        BindEvents();
        CacheBottomNavDefaultSprites();
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

        // MoneyManager 구독
        if (MoneyManager.Instance != null)
        {
            MoneyManager.Instance.OnGoldChanged -= UpdateGoldUI;
            MoneyManager.Instance.OnGoldChanged += UpdateGoldUI;

            MoneyManager.Instance.OnReputationChanged -= UpdateReputationUI;
            MoneyManager.Instance.OnReputationChanged += UpdateReputationUI;
        }

        if (SuddenEventManager.Instance != null)
        {
            SuddenEventManager.Instance.OnPopupRequested -= ShowEventPopup;
            SuddenEventManager.Instance.OnPopupRequested += ShowEventPopup;
        }


        // AlwaysEffectPopup 초기화
        if (_alwaysEffectPopup != null)
            _alwaysEffectPopup.Init();

        RefreshBottomNavTabSprites();
        MoneyManager.Instance.ForceNotify();
    }

    private void ShowEventPopup(SuddenEventManager.EventPopupData data)
    {
        UIPopupRequest req = new UIPopupRequest
        {
            Type = UIPopupRequest.PanelType.Simple, // 안내 위주이므로 Simple 패널 사용
            Title = data.title,
            Message = data.previewText,
            ShowCancel = true, // 취소 버튼 표시
            AutoCloseOnPrimary = true, 
            AutoCloseOnCancel = true,
            OnPrimary = () =>
            {
                // 1. 인박스 팝업을 열고
                OpenMessengerInbox();

                // 2. 해당 채팅방으로 다이렉트 이동
                if (_messengerInboxPopup != null)
                {
                    _messengerInboxPopup.OpenRoom(data.roomId);
                }
            },
            OnCancel = () =>
            {
                // 취소 누르면 방금 창은 닫히고 다음 팝업 띄우기
                if (SuddenEventManager.Instance != null)
                {
                    if (DialogueRunner.Instance != null)
                    {
                        DialogueRunner.Instance.SkipRoom(data.roomId);
                    }

                    SuddenEventManager.Instance.ProcessNextPopup();
                }
            }
        };

        UIManager.Instance.ShowPopup(req); // UIManager를 통해 안전하게 팝업 호출
    }

    private void UpdateMessagePreview(ChatMessage latestMessage)
    {
        if (_txtMessage != null && latestMessage != null)
        {
            _txtMessage.text = latestMessage.Content;
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
                //팝업 데이터 생성
                UIPopupRequest request = new UIPopupRequest
                {
                    Type = UIPopupRequest.PanelType.Default,
                    Title = "특수 훈련",
                    Message = "이 훈련은 부상 위험이 높지만\n성장 속도가 매우 빠릅니다.",
                    SubMessage = "체력 소모 -30 / 부상 확률 10%",
                    PreviewImageId = null,
                    ShowCancel = true,
                    OnPrimary = () => Debug.Log("이미지 팝업 확인됨"),
                    OnCancel = null,
                    AutoCloseOnPrimary = true,
                    AutoCloseOnCancel = true
                };

                UIManager.Instance.ShowPopup(request);
            });
        }
        if (_btnSetting != null)
        {
            _btnSetting.onClick.AddListener(() =>
            {
                UIManager.Instance.ShowUIUnique(_settingsPanelPrefab);
            });
        }

        // 2. 하단 네비게이션
        if (_btnTraining != null)
            _btnTraining.onClick.AddListener(OnClickTraining);
        if (_btnStudent != null)
            _btnStudent.onClick.AddListener(OnClickStudent);

        // 감독 노드 버튼
        if (_btnCoach != null)
            _btnCoach.onClick.AddListener(OnClickCoach);

        // 시설
        if (_btnFacility != null)
            _btnFacility.onClick.AddListener(OnClickFacility);

        // MVP 미구현 기능들은 '준비중' 알림
        if (_btnShop != null)
            _btnShop.onClick.AddListener(() => ShowNotImplemented("상점"));

        // 현재 적용 중인 상시 효과 확인 팝업 오픈
        if (_btnEffectIcon != null)
            _btnEffectIcon.onClick.AddListener(OnClickEffectIcon);
    }

    private void OnClickTraining()
    {
        if (_trainingSelectPopup == null)
            return;

        bool wasActive = _trainingSelectPopup.gameObject.activeSelf;

        CloseAllLobbyPopups();

        // 이미 열려있던 경우 → 토글로 닫기만 하고 종료
        if (wasActive)
        {
            RefreshBottomNavTabSprites();
            return;
        }

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
        RefreshBottomNavTabSprites();
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
        {
            RefreshBottomNavTabSprites();
            return;
        }

        _studentManagementPopup.Init();
        //_studentManagementPopup.transform.SetAsLastSibling();
        _studentManagementPopup.Open();
        RefreshBottomNavTabSprites();
    }

    private void OnClickCoach()
    {
        if (_headCoachPopup == null)
            return;

        bool wasActive = _headCoachPopup.gameObject.activeSelf;

        CloseAllLobbyPopups();

        // 이미 열려있던 경우 → 토글로 닫기만 하고 종료
        if (wasActive)
        {
            RefreshBottomNavTabSprites();
            return;
        }

        _headCoachPopup.Init();
        //_headCoachPopup.transform.SetAsLastSibling();
        _headCoachPopup.Open();
        RefreshBottomNavTabSprites();
    }
    private void OnClickFacility()
    {
        if (_facilityPopup == null)
            return;

        bool wasActive = _facilityPopup.gameObject.activeSelf;

        CloseAllLobbyPopups();

        // 이미 열려있던 경우 → 토글로 닫기만 하고 종료
        if (wasActive)
        {
            RefreshBottomNavTabSprites();
            return;
        }

        _facilityPopup.Init();
        //_headCoachPopup.transform.SetAsLastSibling();
        _facilityPopup.Open();
        RefreshBottomNavTabSprites();
    }

    // 상시 효과 확인 팝업 — 다른 로비 팝업과 독립적으로 토글
    private void OnClickEffectIcon()
    {
        if (_alwaysEffectPopup == null)
            return;

        if (_alwaysEffectPopup.gameObject.activeSelf)
        {
            _alwaysEffectPopup.Close();
            return;
        }

        _alwaysEffectPopup.transform.SetAsLastSibling();
        _alwaysEffectPopup.Open();
    }

    // 데이터 매니저 등에서 정보를 받아와 UI 갱신
    private void ShowNotImplemented(string featureName)
    {
        UIManager.Instance.ShowPopup(new PopupData(
            title: "알림",
            content: $"{featureName} 기능은 아직 개발되지 않았습니다.",
            buttons: new List<PopupButtonInfo>
            {
            new PopupButtonInfo(() => { }) // 확인 버튼 표시용
            }
        ));
    }

    // 데이터 매니저에서 정보를 받아와 UI 갱신
    public void UpdateUI()
    {
        // 예시 데이터 바인딩
        if (_txtSchoolName) _txtSchoolName.text = FormatSchoolNameWithHighlightedPrefix("한울고등학교");
        if (_txtMoney) _txtMoney.text = MoneyManager.Instance.Gold.ToString();
        if (_txtFame) _txtFame.text = MoneyManager.Instance.Reputation.ToString();
        if (_txtMessage)
        {
            var rooms = MessengerManager.Instance?.ActiveRooms;
            if (rooms != null && rooms.Count > 0 && rooms[0].Messages.Count > 0)
            {
                var lastRoom = rooms[0];
                var lastMsg = lastRoom.Messages[lastRoom.Messages.Count - 1];

                string content = lastMsg.Content.Replace("\n", " ");

                if (content.Length > 18)
                {
                    content = content.Substring(0, 18) + "...";
                }

                _txtMessage.text = $"<b>{lastRoom.RoomName}</b>\n{content}";
            }
            else
            {
                // 기존 기본 대사 유지
                _txtMessage.text = "감독님, 신입생들이 입학했습니다. 훈련 일정을 잡아주세요.";
            }
        }
    }

    // 학교명 접두부(고등학교 앞)를 강조 색상으로 감싼 TMP RichText 문자열 생성
    private static string FormatSchoolNameWithHighlightedPrefix(string schoolName)
    {
        if (string.IsNullOrWhiteSpace(schoolName))
            return string.Empty;

        const string suffix = "고등학교";
        const string highlightColorHex = "FF4500";

        int suffixStartIndex = schoolName.IndexOf(suffix, StringComparison.Ordinal);
        if (suffixStartIndex <= 0)
            return schoolName;

        string prefix = schoolName[..suffixStartIndex];
        string suffixText = schoolName[suffixStartIndex..];
        return $"<color=#{highlightColorHex}>{prefix}</color>{suffixText}";
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

    // GameManager 슬롯 자동 배치 시 필드 슬롯 목록 제공
    public List<StudentSlot> GetFieldSlots()
    {
        return _studentManagementPopup != null
            ? _studentManagementPopup.GetFieldSlots()
            : null;
    }

    // 일반 학생 관리 팝업 오픈 (GameManager 자동 배치 복원 후 호출 등)
    public void OpenStudentManagementPopup()
    {
        if (_studentManagementPopup == null) return;

        bool wasActive = _studentManagementPopup.gameObject.activeSelf;

        CloseAllLobbyPopups();

        // 이미 열려있던 경우 → 토글로 닫기만 하고 종료
        if (wasActive)
            return;

        _studentManagementPopup.Init();
        //_studentManagementPopup.transform.SetAsLastSibling();
        _studentManagementPopup.Open();
        RefreshBottomNavTabSprites();
    }

    // 토너먼트 진입 흐름용 — 학생 관리 팝업을 열고 토너먼트 시작 콜백 주입
    public void OpenStudentManagementPopupForTournament(Action onTournamentStart)
    {
        if (_studentManagementPopup == null) return;

        CloseAllLobbyPopups();

        _studentManagementPopup.Init();
        _studentManagementPopup.SetTournamentStartCallback(onTournamentStart);
        _studentManagementPopup.transform.SetAsLastSibling();
        _studentManagementPopup.Open();
        RefreshBottomNavTabSprites();
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

        if (_headCoachPopup != null && _headCoachPopup.gameObject.activeSelf)
        {
            _headCoachPopup.Close();
        }

        if (_facilityPopup != null && _facilityPopup.gameObject.activeSelf)
        {
            _facilityPopup.Close();
        }

        RefreshBottomNavTabSprites();
    }

    // 버튼 기본 스프라이트를 캐시해 비활성 상태 복원에 사용
    private void CacheBottomNavDefaultSprites()
    {
        _trainingDefaultSprite = GetButtonSprite(_btnTraining);
        _studentDefaultSprite = GetButtonSprite(_btnStudent);
        _facilityDefaultSprite = GetButtonSprite(_btnFacility);
        _coachDefaultSprite = GetButtonSprite(_btnCoach);
        _shopDefaultSprite = GetButtonSprite(_btnShop);
    }

    // 현재 팝업 상태에 맞춰 하단 탭 버튼 이미지를 갱신
    private void RefreshBottomNavTabSprites()
    {
        bool trainingActive = IsPopupActive(_trainingSelectPopup);
        bool studentActive = IsPopupActive(_studentManagementPopup);
        bool coachActive = IsPopupActive(_headCoachPopup);
        bool facilityActive = IsPopupActive(_facilityPopup);

        ApplyTabSprite(_btnTraining, _trainingDefaultSprite, _activeTabSprites != null ? _activeTabSprites.training : null, trainingActive);
        ApplyTabSprite(_btnStudent, _studentDefaultSprite, _activeTabSprites != null ? _activeTabSprites.student : null, studentActive);
        ApplyTabSprite(_btnFacility, _facilityDefaultSprite, _activeTabSprites != null ? _activeTabSprites.facility : null, facilityActive);
        ApplyTabSprite(_btnCoach, _coachDefaultSprite, _activeTabSprites != null ? _activeTabSprites.coach : null, coachActive);
        ApplyTabSprite(_btnShop, _shopDefaultSprite, _activeTabSprites != null ? _activeTabSprites.shop : null, false);

        _lastTrainingPopupActive = trainingActive;
        _lastStudentPopupActive = studentActive;
        _lastCoachPopupActive = coachActive;
        _lastFacilityPopupActive = facilityActive;
    }

    // 탭 활성 여부에 따라 버튼 타겟 이미지 스프라이트를 변경
    private static void ApplyTabSprite(Button button, Sprite defaultSprite, Sprite activeSprite, bool isActive)
    {
        if (button == null) return;

        Image targetImage = button.targetGraphic as Image;
        if (targetImage == null)
            return;

        Sprite nextSprite = isActive ? (activeSprite != null ? activeSprite : defaultSprite) : defaultSprite;
        if (nextSprite != null)
            targetImage.sprite = nextSprite;
    }

    private void UpdateGoldUI(int gold)
    {
        if (_txtMoney != null)
            _txtMoney.text = gold.ToString();
    }

    private void UpdateReputationUI(int reputation)
    {
        if (_txtFame != null)
            _txtFame.text = reputation.ToString();
    }

    // 버튼 타겟 이미지에서 현재 스프라이트를 읽는다
    private static Sprite GetButtonSprite(Button button)
    {
        if (button == null) return null;

        Image targetImage = button.targetGraphic as Image;
        return targetImage != null ? targetImage.sprite : null;
    }

    // UIBase 계열 팝업의 활성 상태를 안전하게 조회
    private static bool IsPopupActive(UIBase popup)
    {
        return popup != null && popup.gameObject.activeSelf;
    }

    // StudentManagementPopup 닫기 버튼에서 호출
    public void OnClickStudentClose()
    {
        RefreshBottomNavTabSprites();
    }
}