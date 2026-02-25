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

    // UIManager 기준 Canvas 루트 참조
    private Transform CanvasRoot => UIManager.Instance != null
        ? UIManager.Instance.GetCanvasRoot()
        : null;

    public event Action<List<Student>> OnRecruitmentCompleted; // 영입 완료 콜백

    // 후보 학생을 StudentManager(보유 학생)와 분리해서 관리
    // 후보는 RecruitmentPopup에만 주입해서 UI/선택에만 사용
    private readonly List<Student> _candidateStudents = new();

    void Start()
    {
        _turnManager = FindFirstObjectByType<TurnManager>();
        SubscribeDateEvents(); // 날짜 이벤트 연결
    }

    void OnDestroy()
    {
        UnsubscribeDateEvents(); // 이벤트 해제
    }

    // 외부 호출 API
    // 게임 시작 시 최초 영입 트리거
    public void TriggerInitialRecruitment()
    {
        // 새 게임 시작 시점에만 보유 학생 전체 초기화
        // 입학 영입(신입생 추가)에서는 기존 보유 학생이 유지되어야 함
        if (StudentManager.Instance != null)
        {
            StudentManager.Instance.ClearAllStudents(); // 새 게임 시작 시에만 전체 초기화
        }

        GenerateCandidateStudents();
        ShowRecruitmentEventConfirm(RecruitmentContext.GameStart);
    }

    // 기존 LobbyUI 호출 유지용 래퍼
    public void TryStartRecruitment()
    {
        TriggerInitialRecruitment();
    }

    // 새 학기 신입생 영입 트리거
    public void TriggerEnrollmentRecruitment()
    {
        // 입학 영입에서는 StudentManager(보유 학생) 초기화 금지
        // 후보는 _candidateStudents로만 생성/관리
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

    // 입학 이벤트 발생 시 자동 영입 흐름 시작
    private void HandleEnrollmentTriggered()
    {
        // 자동 입학 영입도 동일하게 "후보만 생성" (보유 학생 삭제 금지)
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

        bool canSkip = context != RecruitmentContext.GameStart;

        ConfirmPopupRequest request = new ConfirmPopupRequest(
            title: "학생 영입",
            message: BuildEventMessage(context),
            primaryLabel: "확인",
            primaryAction: OpenRecruitmentPopup,
            secondaryLabel: canSkip ? "포기" : null,
            secondaryAction: canSkip ? HandleRecruitmentSkipped : null,
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

        // StudentManager가 아닌 "후보 리스트"를 팝업에 주입
        popup.SetCandidates(_candidateStudents);

        popup.Init();
        popup.Open();

        popup.OnRecruitmentConfirmed -= HandleRecruitmentConfirmed;
        popup.OnRecruitmentConfirmed += HandleRecruitmentConfirmed;

        popup.OnCancelled -= HandleRecruitmentSkipped;
        popup.OnCancelled += HandleRecruitmentSkipped;
    }

    // 영입 결과 처리
    // 영입 확정 → 선택된 학생만 팀에 등록
    private void HandleRecruitmentConfirmed(List<Student> recruits)
    {
        if (recruits == null || recruits.Count == 0) return;

        if (StudentManager.Instance != null)
        {
            // 영입 확정 시 "추가(Add)"만 수행 (입학 때 기존 학생 유지)
            // 기존 로직의 ClearAllStudents() 제거 (새게임 시작에서만 초기화)
            foreach (Student student in recruits)
            {
                StudentManager.Instance.AddStudent(student);
            }
        }

        Debug.Log($"[RecruitmentManager] 영입 완료: {recruits.Count}명");
        OnRecruitmentCompleted?.Invoke(recruits);

        // 후보는 한 번 쓰고 버리는 성격이므로 정리
        _candidateStudents.Clear();
    }

    // 영입 포기 → 후보 초기화
    private void HandleRecruitmentSkipped()
    {
        // 포기 시에도 StudentManager(보유 학생) 건드리지 않음
        // 후보만 폐기
        _candidateStudents.Clear();
        Debug.Log("[RecruitmentManager] 영입 포기");
    }

    // 후보 생성
    private void GenerateCandidateStudents()
    {
        // 후보는 StudentManager가 아니라 _candidateStudents로만 생성
        _candidateStudents.Clear();

        for (int i = 0; i < _recruitCandidateCount; i++)
        {
            _candidateStudents.Add(StudentFactory.CreateStudent(grade: 1));
        }

        Debug.Log($"[RecruitmentManager] 영입 후보 {_recruitCandidateCount}명 생성 완료");
    }

    // 메시지 빌드
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
    // 디버그용 컨텍스트 메뉴
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