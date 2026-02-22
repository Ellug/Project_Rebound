using System;
using System.Collections.Generic;
using UnityEngine;

// 학생 영입 흐름 전체 관리
// 영입 발생 시점 2가지:
//   1. 게임 시작 시 (TriggerInitialRecruitment 외부 호출) → 최초 선수 모집
//   2. 입학 이벤트 시 (OnEnrollmentTriggered) → 새 학기 신입생 모집
// 흐름: TrainingConfirmPopup(이벤트 팝업) → 확인 시 RecruitmentPopup 열기
//       이후 확인/취소/최대인원/합류 팝업 → UIPopup
public class RecruitmentManager : MonoBehaviour
{
    [Header("Popup Prefabs")]
    [SerializeField] private TrainingConfirmPopup _eventPopupPrefab;    // 영입 안내 이벤트 팝업
    [SerializeField] private RecruitmentPopup _recruitmentPopupPrefab;  // 카드 선택 팝업

    [Header("Settings")]
    [SerializeField] private int _maxRecruitCount = 7;        // 최대 영입 가능 인원
    [SerializeField] private int _recruitCandidateCount = 10; // 영입 후보 학생 생성 수

    private TurnManager _turnManager;

    // UIManager._canvasRoot를 사용하므로 별도 Canvas 참조 불필요
    private Transform CanvasRoot => UIManager.Instance != null
        ? UIManager.Instance.GetCanvasRoot()
        : null;

    // 영입 완료 이벤트 (외부 시스템 연동용)
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

    // ── 외부 호출 API ───────────────────────────────────────────────

    // 게임 시작 시 GameManager에서 호출
    public void TriggerInitialRecruitment()
    {
        GenerateCandidateStudents();
        ShowEventPopup(RecruitmentContext.GameStart);
    }

    // ── DateManager 이벤트 구독 ─────────────────────────────────────

    private void SubscribeDateEvents()
    {
        if (_turnManager == null) return;
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
        ShowEventPopup(RecruitmentContext.NewSemester);
    }

    // ── 영입 흐름 ───────────────────────────────────────────────────

    // 1단계: TrainingConfirmPopup을 이벤트 팝업으로 사용 (학생 이미지 + 메시지 + 포기/확인)
    // 확인 → RecruitmentPopup 열기 / 포기 → 후보 정리
    private void ShowEventPopup(RecruitmentContext context)
    {
        if (_eventPopupPrefab == null)
        {
            Debug.LogWarning("[RecruitmentManager] EventPopup 프리팹이 없습니다.");
            return;
        }

        Transform canvasRoot = CanvasRoot;
        if (canvasRoot == null)
        {
            Debug.LogWarning("[RecruitmentManager] UIManager Canvas Root를 찾을 수 없습니다.");
            return;
        }

        TrainingConfirmPopup popup = Instantiate(_eventPopupPrefab, canvasRoot);
        popup.transform.SetAsLastSibling();
        popup.SetupAsEvent(new EventPopupData(
            message: BuildEventMessage(context),
            title: "학생 영입"
        ));
        popup.Init();
        popup.Open();

        popup.OnConfirmed += OpenRecruitmentPopup;
        popup.OnCancelled += HandleRecruitmentSkipped;
    }

    // 2단계: RecruitmentPopup 열기 (카드 선택)
    // 이후 확인/취소/최대인원/합류 팝업은 UIPopup으로 처리
    private void OpenRecruitmentPopup()
    {
        if (_recruitmentPopupPrefab == null)
        {
            Debug.LogWarning("[RecruitmentManager] RecruitmentPopup 프리팹이 없습니다.");
            return;
        }

        RecruitmentPopup popup = Instantiate(_recruitmentPopupPrefab, CanvasRoot);
        popup.transform.SetAsLastSibling();
        popup.SetMaxRecruitCount(_maxRecruitCount);
        popup.Init();
        popup.Open();

        popup.OnRecruitmentConfirmed += HandleRecruitmentConfirmed;
        popup.OnCancelled += HandleRecruitmentSkipped;
    }

    // 영입 확정 → StudentManager에 확정 학생만 재등록
    private void HandleRecruitmentConfirmed(List<Student> recruits)
    {
        if (recruits == null || recruits.Count == 0) return;

        StudentManager.Instance.ClearAllStudents();

        foreach (Student student in recruits)
            StudentManager.Instance.AddStudent(student);

        Debug.Log($"[RecruitmentManager] 영입 완료: {recruits.Count}명");

        OnRecruitmentCompleted?.Invoke(recruits);
    }

    // 영입 포기 → 후보 목록 정리
    private void HandleRecruitmentSkipped()
    {
        StudentManager.Instance?.ClearAllStudents();
        Debug.Log("[RecruitmentManager] 영입 포기");
    }

    // ── 후보 생성 ───────────────────────────────────────────────────

    private void GenerateCandidateStudents()
    {
        if (StudentManager.Instance == null) return;

        StudentFactory.ResetUsedNames();
        StudentManager.Instance.ClearAllStudents();

        for (int i = 0; i < _recruitCandidateCount; i++)
            StudentManager.Instance.AddStudent(StudentFactory.CreateStudent(grade: 1));

        Debug.Log($"[RecruitmentManager] 영입 후보 {_recruitCandidateCount}명 생성 완료");
    }

    // ── 메시지 ──────────────────────────────────────────────────────

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

    [ContextMenu("Debug - Trigger Enrollment")]
    private void DebugTriggerEnrollment() => HandleEnrollmentTriggered();
#endif
}

// 영입 발생 상황 구분
public enum RecruitmentContext
{
    GameStart,   // 게임 시작 시 최초 영입
    NewSemester  // 새 학기 시작 시 신입생 영입
}