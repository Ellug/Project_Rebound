using System;
using System.Collections.Generic;
using UnityEngine;

// 학생 영입 흐름 전체 관리
// 영입 발생 시점 2가지:
//   1. 게임 시작 시 (TriggerInitialRecruitment 외부 호출) → 최초 선수 모집
//   2. 입학 이벤트 시 (HandleEnrollmentTriggered / TriggerEnrollment) → 새 학기 신입생 모집
// 흐름: ConfirmPopup(완전 범용) → 확인 시 RecruitmentPopup 열기
//       이후 확인/취소/최대인원/합류 팝업 → ConfirmPopup
public class RecruitmentManager : MonoBehaviour
{
    [Header("Popup Prefabs")]
    [SerializeField] private RecruitmentPopup _recruitmentPopupPrefab;  // 카드 선택 팝업

    [Header("Settings")]
    [SerializeField] private int _maxRecruitCount = 7;        // 최대 영입 가능 인원
    [SerializeField] private int _recruitCandidateCount = 10; // 영입 후보 학생 생성 수

    private TurnManager _turnManager;

    private Transform CanvasRoot => UIManager.Instance != null
        ? UIManager.Instance.GetCanvasRoot()
        : null;

    public event Action<List<Student>> OnRecruitmentCompleted;

    void Start()
    {
        _turnManager = FindFirstObjectByType<TurnManager>();
        SubscribeDateEvents();
    }

    void OnDestroy()
    {
        UnsubscribeDateEvents();
    }

    
    // 외부 호출 API
    // 게임 시작 시 GameManager에서 호출 (기존 API 유지)
    public void TriggerInitialRecruitment()
    {
        GenerateCandidateStudents();
        ShowRecruitmentEventConfirm(RecruitmentContext.GameStart);
    }

    // LobbyUI 호환용 엔트리포인트 (기존 호출부 수정 없이 유지)
    public void TryStartRecruitment()
    {
        TriggerInitialRecruitment();
    }

    // 새 학기 입학 이벤트 트리거가 외부에 있는 경우를 대비한 API (선택)
    public void TriggerEnrollmentRecruitment()
    {
        GenerateCandidateStudents();
        ShowRecruitmentEventConfirm(RecruitmentContext.NewSemester);
    }

    
    // DateManager 이벤트 구독

    private void SubscribeDateEvents()
    {
        if (_turnManager == null) return;

        _turnManager.DateManager.OnEnrollmentTriggered -= HandleEnrollmentTriggered;
        _turnManager.DateManager.OnEnrollmentTriggered += HandleEnrollmentTriggered;
    }

    private void UnsubscribeDateEvents()
    {
        if (_turnManager == null) return;

        _turnManager.DateManager.OnEnrollmentTriggered -= HandleEnrollmentTriggered;
    }

    private void HandleEnrollmentTriggered()
    {
        GenerateCandidateStudents();
        ShowRecruitmentEventConfirm(RecruitmentContext.NewSemester);
    }

    
    // 1단계: 이벤트 안내 ConfirmPopup
    
    private void ShowRecruitmentEventConfirm(RecruitmentContext context)
    {
        if (UIManager.Instance == null)
        {
            Debug.LogWarning("[RecruitmentManager] UIManager가 없어 영입 안내 팝업을 띄울 수 없습니다.");
            return;
        }

        ConfirmPopupRequest request = new ConfirmPopupRequest(
            title: "학생 영입",
            message: BuildEventMessage(context),
            primaryLabel: "확인",
            primaryAction: OpenRecruitmentPopup,
            secondaryLabel: "포기",
            secondaryAction: HandleRecruitmentSkipped,
            previewSprite: null
        );

        request.IsModal = true;
        UIManager.Instance.ShowConfirm(request);
    }

    
    // 2단계: 카드 선택 팝업

    private void OpenRecruitmentPopup()
    {
        if (_recruitmentPopupPrefab == null)
        {
            Debug.LogWarning("[RecruitmentManager] RecruitmentPopup 프리팝이 없습니다.");
            return;
        }

        Transform canvasRoot = CanvasRoot;
        if (canvasRoot == null)
        {
            Debug.LogWarning("[RecruitmentManager] CanvasRoot를 찾을 수 없습니다.");
            return;
        }

        RecruitmentPopup popup = Instantiate(_recruitmentPopupPrefab, canvasRoot);
        popup.transform.SetAsLastSibling();
        popup.SetMaxRecruitCount(_maxRecruitCount);
        popup.Init();
        popup.Open();

        popup.OnRecruitmentConfirmed -= HandleRecruitmentConfirmed;
        popup.OnRecruitmentConfirmed += HandleRecruitmentConfirmed;

        popup.OnCancelled -= HandleRecruitmentSkipped;
        popup.OnCancelled += HandleRecruitmentSkipped;
    }

    // 영입 확정 → StudentManager에 확정 학생만 재등록
    private void HandleRecruitmentConfirmed(List<Student> recruits)
    {
        if (recruits == null || recruits.Count == 0) return;

        if (StudentManager.Instance != null)
        {
            StudentManager.Instance.ClearAllStudents();

            foreach (Student student in recruits)
                StudentManager.Instance.AddStudent(student);
        }

        Debug.Log($"[RecruitmentManager] 영입 완료: {recruits.Count}명");
        OnRecruitmentCompleted?.Invoke(recruits);
    }

    // 영입 포기 → 후보 목록 정리
    private void HandleRecruitmentSkipped()
    {
        StudentManager.Instance?.ClearAllStudents();
        Debug.Log("[RecruitmentManager] 영입 포기");
    }

    
    // 후보 생성
    private void GenerateCandidateStudents()
    {
        if (StudentManager.Instance == null) return;

        StudentFactory.ResetUsedNames();
        StudentManager.Instance.ClearAllStudents();

        for (int i = 0; i < _recruitCandidateCount; i++)
            StudentManager.Instance.AddStudent(StudentFactory.CreateStudent(grade: 1));

        Debug.Log($"[RecruitmentManager] 영입 후보 {_recruitCandidateCount}명 생성 완료");
    }

    
    // 메시지
    private static string BuildEventMessage(RecruitmentContext context)
    {
        return context switch
        {
            RecruitmentContext.GameStart =>
                "학기가 시작되었습니다.\n합격된 신입 학생을 영입할 수 있습니다.\n" +
                "영입은 한 번에 단 한 번만 진행되며,\n선택 후 변경할 수 없습니다.",

            RecruitmentContext.NewSemester =>
                "새 학기가 시작되었습니다.\n3학년 선배들이 졸업하고 신입생이 입학했습니다.\n" +
                "영입은 한 번에 단 한 번만 진행되며,\n선택 후 변경할 수 없습니다.",

            _ => "학생 영입을 진행합니다."
        };
    }

#if UNITY_EDITOR
    [ContextMenu("Debug - Trigger Initial Recruitment")]
    private void DebugTriggerInitial() => TriggerInitialRecruitment();

    [ContextMenu("Debug - Trigger Enrollment Recruitment")]
    private void DebugTriggerEnrollment() => TriggerEnrollmentRecruitment();
#endif
}

// 영입 발생 상황 구분
public enum RecruitmentContext
{
    GameStart,   // 게임 시작 시 최초 영입
    NewSemester  // 새 학기 시작 시 신입생 영입
}