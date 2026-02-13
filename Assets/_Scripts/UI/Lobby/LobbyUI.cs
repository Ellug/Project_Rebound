using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

public class LobbyUI : UIBase
{
    [Header("Top Info")]
    [SerializeField] private TMP_Text _txtSchoolName;
    [SerializeField] private TMP_Text _txtDate;
    [SerializeField] private TMP_Text _txtDDay;
    [SerializeField] private TMP_Text _txtMoney;
    [SerializeField] private TMP_Text _txtFame;

    [Header("Top Buttons")]
    [SerializeField] private Button _btnLog;
    [SerializeField] private Button _btnSetting;

    [Header("Center Message")]
    [SerializeField] private TMP_Text _txtMessage;

    [Header("Bottom Nav Buttons")]
    [SerializeField] private Button _btnTraining;
    [SerializeField] private Button _btnStudent;
    [SerializeField] private Button _btnFacility;
    [SerializeField] private Button _btnCoach;
    [SerializeField] private Button _btnShop;


    void Start()
    {
        Init();
    }

    public override void Init()
    {
        base.Init();
        BindEvents();
        UpdateUI();
    }

    private void BindEvents()
    {
        // 1. 설정 버튼: 기능 버튼 예시
        _btnSetting.onClick.AddListener(() =>
        {
            // 간단한 확인 팝업
            UIManager.Instance.ShowPopup("설정", "환경설정 기능은 준비 중입니다.");
        });

        // 2. 훈련 버튼: 선택지가 필요한 팝업 예시
        _btnTraining.onClick.AddListener(() =>
        {
            var buttons = new List<PopupButtonInfo>
            {
                new PopupButtonInfo("기초 훈련", () => Debug.Log("기초 훈련 시작")),
                new PopupButtonInfo("전술 훈련", () => Debug.Log("전술 훈련 시작")),
                new PopupButtonInfo("휴식", () => Debug.Log("휴식 진행")),
                new PopupButtonInfo("닫기", null)
            };

            UIManager.Instance.ShowPopup(new PopupData(
                "일과 선택",
                "진행할 훈련 스케줄을 선택해주세요.\n체력이 부족하면 부상 위험이 있습니다.",
                buttons
            ));
        });

        // 3. 학생 관리 버튼: 화면 이동 예시 (Yes/No)
        _btnStudent.onClick.AddListener(() =>
        {
            var buttons = new List<PopupButtonInfo>
            {
                new PopupButtonInfo("이동", () => Debug.Log("학생 관리 씬/패널로 이동 로직")),
                new PopupButtonInfo("취소", null)
            };

            UIManager.Instance.ShowPopup(new PopupData("학생 관리", "학생 관리 메뉴로 이동하시겠습니까?", buttons));
        });

        // 4. 미구현 기능들
        _btnFacility.onClick.AddListener(() => ShowNotImplemented("시설"));
        _btnCoach.onClick.AddListener(() => ShowNotImplemented("감독 노드"));
        _btnShop.onClick.AddListener(() => ShowNotImplemented("상점"));
    }

    private void ShowNotImplemented(string feature)
    {
        UIManager.Instance.ShowPopup("알림", $"{feature} 기능은 아직 개발 중입니다.");
    }

    public void UpdateUI()
    {
        // TODO: 실제 데이터 매니저 연동 필요
        if (_txtSchoolName) _txtSchoolName.text = "한울고등학교";
        if (_txtDate) _txtDate.text = "2000.03.02";
        if (_txtDDay) _txtDDay.text = "D-100";
        if (_txtMoney) _txtMoney.text = "5000 G";
        if (_txtFame) _txtFame.text = "150";
        if (_txtMessage) _txtMessage.text = "감독님, 훈련 일정을 정해주세요.";
    }
}