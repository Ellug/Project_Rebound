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

    [Header("Popups")]
    [SerializeField] private TrainingSelectPopup _trainingSelectPopup; // 씬에 배치된 훈련 선택 팝업 (직접 참조)

    [Header("Center Message")]
    [SerializeField] private TMP_Text _txtMessage;

    [Header("Bottom Navigation Buttons")]
    [SerializeField] private Button _btnTraining; // 훈련 (구 일과)
    [SerializeField] private Button _btnStudent;  // 학생 관리
    [SerializeField] private Button _btnFacility; // 시설 (MVP 개발 X)
    [SerializeField] private Button _btnCoach;    // 감독 노드 (MVP 개발 X)
    [SerializeField] private Button _btnShop;     // 상점 (MVP 개발 X)

    [Header("Test")]
    [SerializeField] private Sprite _testSprite;

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
            _btnSetting.onClick.AddListener(() =>
            {
                UIManager.Instance.ShowPopup(new PopupData(
                    title: "설정",
                    content: "환경설정 기능은 준비 중입니다."
                ));
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

        if (_trainingSelectPopup == null)
        {
            Debug.LogError("[LobbyUI] _trainingSelectPopup이 null입니다!");
            return;
        }

        // 씬에 배치된 팝업을 직접 열기
        _trainingSelectPopup.Init();
        _trainingSelectPopup.Open();

        // 이벤트 중복 구독 방지 후 구독
        _trainingSelectPopup.OnTrainingSelected -= HandleTrainingSelected;
        _trainingSelectPopup.OnTrainingSelected += HandleTrainingSelected;

        Debug.Log("[LobbyUI] 훈련 선택 팝업 열림");
    }

    // 훈련 최종 선택 시 호출
    private void HandleTrainingSelected(string trainingKey)
    {
        Debug.Log($"[LobbyUI] 선택된 훈련: {trainingKey}");
        // TODO: TrainingManager를 통한 훈련 실행 처리
    }

    private void OnClickStudent()
    {
        var buttons = new List<PopupButtonInfo>
        {
            new PopupButtonInfo("취소", null),
            new PopupButtonInfo("이동", () => Debug.Log("학생 관리 씬 이동"))
        };

        UIManager.Instance.ShowPopup(new PopupData(
            title: "학생 관리",
            content: "학생 관리 메뉴로 이동하시겠습니까?",
            buttons: buttons
        ));
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
        if (_txtDate) _txtDate.text = "2000.03.02";
        if (_txtDDay) _txtDDay.text = "D-100";
        if (_txtMoney) _txtMoney.text = "5000 G";
        if (_txtFame) _txtFame.text = "150";
        if (_txtMessage) _txtMessage.text = "감독님, 신입생들이 입학했습니다. 훈련 일정을 잡아주세요.";
    }
}