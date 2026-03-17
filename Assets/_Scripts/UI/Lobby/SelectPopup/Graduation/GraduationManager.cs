using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;


// 졸업 이벤트 전체 흐름을 관리
// AlwaysEventManager의 roster_graduate 활성화를 구독
// Confirm → 목록 → 완료 → 실제 졸업 처리
// 테스트용 F12 트리거
public class GraduationManager : MonoBehaviour
{
    [Header("Scene References")]
    [SerializeField] private GraduationStudentsPopup _graduationPopup;
    [SerializeField] private TurnManager _turnManager;

    private AlwaysEventManager _alwaysEventManager;

#if UNITY_EDITOR || 테스트용
    [Header("Dev Test (F12)")]
    [SerializeField] private bool _enableF12Test = true;        // F12 테스트 활성화
    [SerializeField] private bool _f12ClearBeforeCreate = false;// 기존 학생 삭제 여부
    [SerializeField] private int _f12CreateGrade3Count = 8;     // 생성할 3학년 수
#endif

    // 이번 졸업 처리 대상(완료 버튼 누르기 전까지 유지)
    private List<Student> _pendingGraduates = new();

    void Start()
    {
        if (_turnManager == null)
            _turnManager = FindFirstObjectByType<TurnManager>();

        if (_alwaysEventManager == null)
            _alwaysEventManager = FindFirstObjectByType<AlwaysEventManager>();

        SubscribeAlwaysEvents();
    }

    void OnDestroy()
    {
        UnsubscribeAlwaysEvents();
    }

    private void SubscribeAlwaysEvents()
    {
        if (_alwaysEventManager == null) return;

        _alwaysEventManager.OnEventActivated -= HandleAlwaysEventActivated;
        _alwaysEventManager.OnEventActivated += HandleAlwaysEventActivated;
    }

    private void UnsubscribeAlwaysEvents()
    {
        if (_alwaysEventManager == null) return;

        _alwaysEventManager.OnEventActivated -= HandleAlwaysEventActivated;
    }

    private void HandleAlwaysEventActivated(AlwaysEventRow row)
    {
        if (row == null) return;
        if (row.id != "roster_graduate") return;

        HandleGraduationTriggered();
    }


    // 졸업 Confirm 팝업 표시
    private void HandleGraduationTriggered()
    {
        if (UIManager.Instance == null)
            return;

        UIPopupRequest request = new UIPopupRequest
        {
            Type = UIPopupRequest.PanelType.Default,
            Title = "졸업",
            Message =
                "3학년의 마지막 날입니다.\n" +
                "3학년 학생들이 졸업합니다.\n\n" +
                "졸업생 목록을 확인하시겠습니까?",
            ShowCancel = true,
            PreviewImageId = "EventPopup_graduate_img",
            OnPrimary = OpenGraduationStudentsPopup,
            OnCancel = null,
            AutoCloseOnPrimary = true,
            AutoCloseOnCancel = true
        };

        UIManager.Instance.ShowPopup(request);
    }


    // 졸업생 목록 팝업 오픈
    private void OpenGraduationStudentsPopup()
    {
        if (_graduationPopup == null)
            return;

        _pendingGraduates = CollectGraduates();

        _graduationPopup.Init();
        _graduationPopup.Setup(_pendingGraduates, ShowCompletionPopup);
        _graduationPopup.transform.SetAsLastSibling();
        _graduationPopup.Open();
    }

    // 현재 3학년 수집
    private List<Student> CollectGraduates()
    {
        List<Student> result = new();

        if (StudentManager.Instance == null)
            return result;

        foreach (var s in StudentManager.Instance.Students)
        {
            if (s != null && s.grade == 3)
                result.Add(s);
        }

        return result;
    }


    // 완료 팝업 표시
    private void ShowCompletionPopup()
    {
        if (UIManager.Instance == null)
            return;

        UIManager.Instance.ShowPopup(new PopupData(
            title: "졸업식",
            content: "졸업을 축하합니다!\n졸업 처리가 완료되었습니다.",
            buttons: new List<PopupButtonInfo>
            {
                // 수정: 버튼 텍스트 제거, 액션 기반으로만 구성
                new PopupButtonInfo(FinalizeGraduation)
            }
        ));
    }


    //실제 졸업 처리
    //기록 저장
    //학생 제거
    //턴 1회 진행
    private void FinalizeGraduation()
    {
        if (_turnManager == null)
            _turnManager = FindFirstObjectByType<TurnManager>();

        SaveGraduationRecord(_pendingGraduates);

        if (StudentManager.Instance != null)
        {
            StudentManager.Instance.GraduateSeniors(); // 3학년 삭제 + 슬롯 자동 비우기
            StudentManager.Instance.PromoteStudents(); // 남은 재학생 진급
        }



        // 페이즈 전환
        if (_turnManager != null)
            _turnManager.SetPhase(GamePhase.Graduation);

        // 하루 진행 (입학/학기 전환 트리거 목적)
        if (_turnManager != null && !_turnManager.IsTurnRunning)
            _turnManager.ExecuteTurn(TurnActionType.Rest);

        // 로비 상태 복귀
        if (_turnManager != null)
            _turnManager.SetPhase(GamePhase.DailyTraining);

        if (_graduationPopup != null)
            _graduationPopup.Close();

        _pendingGraduates.Clear();

        Debug.Log("[GraduationManager] Graduation finalized.");
    }


    // 졸업 기록 로그 출력
    // (추후 SaveData 연동 지점)
    private void SaveGraduationRecord(List<Student> graduates)
    {
        if (_turnManager == null)
            return;

        Debug.Log($"[Graduation] Date: {_turnManager.DateManager.FormattedDate}");

        foreach (var s in graduates)
        {
            Debug.Log($"졸업생: {s.studentName} / {s.positionName}");
        }
    }


    // -----테스트용-----

    void Update()
    {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
        if (!_enableF12Test)
            return;

        // New Input System 방식
        if (Keyboard.current != null && Keyboard.current.f12Key.wasPressedThisFrame)
        {
            Debug_TriggerGraduationByF12();
        }
#endif
    }


#if UNITY_EDITOR || DEVELOPMENT_BUILD
    // F12 테스트 트리거
    // 필요 시 3학년 생성
    // 실제 졸업 플로우 시작
    private void Debug_TriggerGraduationByF12()
    {
        Debug.Log("[GraduationManager][F12] Triggered");

        if (UIManager.Instance == null || StudentManager.Instance == null)
            return;

        if (_f12ClearBeforeCreate)
            StudentManager.Instance.ClearAllStudents();

        int created = 0;
        for (int i = 0; i < _f12CreateGrade3Count; i++)
        {
            Student s = TryCreateGrade3FallbackSafe(i);
            if (s == null)
                continue;

            StudentManager.Instance.AddStudent(s);
            created++;
        }

        Debug.Log($"[GraduationManager][F12] Created Grade3: {created}");

        HandleGraduationTriggered();
    }

    // StudentFactory 실패 시 더미 학생 생성
    private Student TryCreateGrade3FallbackSafe(int index)
    {
        try
        {
            return StudentFactory.CreateStudent(grade: 3);
        }
        catch
        {
            Student s = new Student
            {
                id = 900000 + index,
                studentName = $"TEST_졸업생_{index + 1}",
                positionName = "TEST",
                grade = 3
            };
            return s;
        }
    }
#endif
}
