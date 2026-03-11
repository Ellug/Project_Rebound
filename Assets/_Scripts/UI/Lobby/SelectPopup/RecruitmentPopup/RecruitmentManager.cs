using System;
using System.Collections.Generic;
using UnityEngine;

// 학생 영입 흐름 전체 관리
// AlwaysEvent 기반으로 영입 이벤트를 수신하고
// ConfirmPopup → RecruitmentPopup 흐름을 제어
public class RecruitmentManager : MonoBehaviour
{
    // 신뢰도 붕괴 퇴부로 정원이 내려갈 때 보장할 최소 정원
    private const int MinRecruitCapacity = 5;

    [Header("Popup Prefabs")]
    [SerializeField] private RecruitmentPopup _recruitmentPopupPrefab;  // 카드 선택 팝업

    [Header("Settings")]
    [SerializeField] private int _maxRecruitCount = 7;        // 최대 영입 가능 인원
    [SerializeField] private int _recruitCandidateCount = 10; // 영입 후보 학생 생성 수

    private AlwaysEventManager _alwaysEventManager;

    // UIManager 기준 Canvas 루트 참조
    private Transform CanvasRoot => UIManager.Instance != null
        ? UIManager.Instance.GetCanvasRoot()
        : null;

    public event Action<List<Student>> OnRecruitmentCompleted; // 영입 완료 콜백

    // 후보 학생을 StudentManager(보유 학생)와 분리해서 관리
    // 후보는 RecruitmentPopup에만 주입해서 UI/선택에만 사용
    private readonly List<Student> _candidateStudents = new();

    // 현재 팀 최대 보유 인원(모집 정원)
    public int MaxRecruitCount => _maxRecruitCount;

    // 신뢰도 0 퇴부 시 정원 감소 처리
    public bool TryDecreaseMaxRecruitCountByTrustExpulsion()
    {
        // 최소 정원(5명) 이하로는 내려가지 않도록 보호
        if (_maxRecruitCount <= MinRecruitCapacity)
        {
            // TODO: 정원이 최소치(5명)일 때의 별도 게임 규칙이 확정되면 이 분기에서 처리
            Debug.LogWarning($"[RecruitmentManager] 최소 정원({MinRecruitCapacity})에 도달해 정원 감소를 막았습니다. current={_maxRecruitCount}");
            return false;
        }

        // 일단 감소는 5 아래로는 안 내려가게
        _maxRecruitCount = Mathf.Max(MinRecruitCapacity, _maxRecruitCount - 1);
        GameManager.Instance.SetRecruitmentMaxCount(_maxRecruitCount);
        Debug.Log($"[RecruitmentManager] 신뢰도 붕괴 퇴부로 최대 정원이 감소했습니다. current={_maxRecruitCount}");
        return true;
    }

    // 저장된 모집 정원 값을 런타임에 복원
    public void ApplyPersistentMaxRecruitCount(int maxRecruitCount)
    {
        _maxRecruitCount = Mathf.Max(MinRecruitCapacity, maxRecruitCount);
    }

    void Start()
    {
        // AlwaysEventManager 탐색 후 이벤트 구독
        _alwaysEventManager = FindFirstObjectByType<AlwaysEventManager>();
        SubscribeDateEvents();
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

        GenerateCandidateStudents(RecruitmentContext.GameStart); // 후보 생성 (랜덤 학년)
        ShowRecruitmentEventConfirm(RecruitmentContext.GameStart);
    }

    // AlwaysEvent 구독 처리
    private void SubscribeDateEvents()
    {
        if (_alwaysEventManager == null) return;

        _alwaysEventManager.OnEventActivated -= HandleAlwaysEventActivated;
        _alwaysEventManager.OnEventActivated += HandleAlwaysEventActivated;
    }

    // 이벤트 해제
    private void UnsubscribeDateEvents()
    {
        if (_alwaysEventManager == null) return;

        _alwaysEventManager.OnEventActivated -= HandleAlwaysEventActivated;
    }

    // AlwaysEvent 활성화 시 호출
    // roster_recruit 분기 처리
    private void HandleAlwaysEventActivated(AlwaysEventRow row)
    {
        if (row == null) return;
        if (row.id != "roster_recruit") return;

        // 3/2(1학기)와 8/10(2학기) 이벤트를 분리해서 문구 적용
        RecruitmentContext context = ResolveSemesterRecruitmentContext(row);

        // 입학(신입생 영입) — 기존 보유 학생 유지
        GenerateCandidateStudents(context); // 1학년만 생성
        ShowRecruitmentEventConfirm(context);
    }

    private static RecruitmentContext ResolveSemesterRecruitmentContext(AlwaysEventRow row)
    {
        string recruitName = (row.name ?? string.Empty).Trim();
        if (string.Equals(recruitName, "second_recruit", StringComparison.OrdinalIgnoreCase))
            return RecruitmentContext.SecondSemester;

        if (string.Equals(recruitName, "first_recruit", StringComparison.OrdinalIgnoreCase))
            return RecruitmentContext.FirstSemester;

        // name 값이 비어있는 예외 데이터 대비: 날짜 기준으로 폴백
        if (AlwaysEventDateUtil.TryParseTableDate(row.termStart, out DateTime startDate))
            return startDate.Month >= 8 ? RecruitmentContext.SecondSemester : RecruitmentContext.FirstSemester;

        return RecruitmentContext.FirstSemester;
    }

    // 1단계: 이벤트 안내 팝업
    private void ShowRecruitmentEventConfirm(RecruitmentContext context)
    {
        if (UIManager.Instance == null)
        {
            Debug.LogWarning("[RecruitmentManager] UIManager가 없어 영입 안내 팝업을 띄울 수 없습니다.");
            return;
        }

        int capacity = _maxRecruitCount; // 팀 정원
        int ownedCount = StudentManager.Instance != null ? StudentManager.Instance.GetStudentCount() : 0;
        bool isFull = ownedCount >= capacity;

        bool isForced = context == RecruitmentContext.GameStart;
        bool canSkip = context != RecruitmentContext.GameStart;
        string messageBody = BuildEventMessage(context);


        // 정원 가득 찼을 때는 확인 버튼이 팝업 닫기만 수행하도록 처리
        Action onPrimary = isFull ? () => { } : () => OpenRecruitmentPopup(isForced);
        Action onCancel = canSkip ? HandleRecruitmentSkipped : null;

        var req = UIPopupRequest.Default(
            title: "학생 영입",
            message: BuildEventMessage(context),
            onPrimary: onPrimary,
            onCancel: onCancel,
            subMessage: isFull ? "현재 보유 학생이 정원에 도달해 영입을 진행할 수 없습니다." : null,
            previewSprite: null,
            showCancel: canSkip,
            primaryKind: UIPopupRequest.PrimaryButtonKind.Confirm
        );

        req.AutoCloseOnPrimary = true;
        req.AutoCloseOnCancel = true;

        UIPopup confirmPopup = UIManager.Instance.ShowPopup(req);

        if (isForced && confirmPopup != null)
        {
            confirmPopup.DisableBackKey = true;
        }

        if (MessengerManager.Instance != null)
        {
            ChatMessage logMsg = new ChatMessage(MessageSenderType.Them, messageBody, MessageEventType.NormalText);
            MessengerManager.Instance.ReceiveMessage("sys_scout", "공지", logMsg);
        }

    }

    // 2단계: 카드 선택 팝업
    private void OpenRecruitmentPopup(bool isForced)
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

        int ownedCount = StudentManager.Instance != null ? StudentManager.Instance.GetStudentCount() : 0;
        int capacity = _maxRecruitCount;
        int remaining = Mathf.Max(0, capacity - ownedCount);

        RecruitmentPopup popup = Instantiate(_recruitmentPopupPrefab, canvasRoot);
        popup.transform.SetAsLastSibling(); // 최상단 정렬

        if (isForced)
        {
            popup.SetForceRecruitMode();
        }

        // 팝업 자체 제한 = min(설정 최대치, 남은 정원)
        popup.SetMaxRecruitCount(Mathf.Min(_maxRecruitCount, remaining));

        // 정원 정보도 넘겨서 버튼/선택을 더 명확히 제어
        popup.SetRosterCapacity(capacity, ownedCount);

        popup.SetCandidates(_candidateStudents);

        popup.Init();
        popup.Open();

        if (UIManager.Instance != null)
        {
            UIManager.Instance.PushUI(popup);
        }

        // 이벤트 중복 방지
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
    private void GenerateCandidateStudents(RecruitmentContext context)
    {
        _candidateStudents.Clear();

        bool recruitFreshmanOnly = context == RecruitmentContext.FirstSemester || context == RecruitmentContext.SecondSemester;
        int gradeParam = recruitFreshmanOnly ? 1 : 0; // 0 전달 시 StudentFactory 내부 랜덤(1~3)

        for (int i = 0; i < _recruitCandidateCount; i++)
            _candidateStudents.Add(StudentFactory.CreateStudent(gradeParam));
    }

    // 메시지 빌드
    private static string BuildEventMessage(RecruitmentContext context)
    {
        return context switch
        {
            RecruitmentContext.GameStart =>
                "학기가 시작되었습니다.\n함께할 학생들을 영입할 수 있습니다.\n\n" +
                "영입은 학기에 단 한 번만 진행되며,\n선택 후에는 변경할 수 없습니다.",

            RecruitmentContext.FirstSemester =>
                "1학기가 시작되었습니다.\n함께할 신입 학생을 영입할 수 있습니다.\n\n" +
                "영입은 학기에 단 한 번만 진행되며,\n선택 후에는 변경할 수 없습니다.",

            RecruitmentContext.SecondSemester =>
                "2학기가 시작되었습니다.\n함께할 신입 학생을 영입할 수 있습니다.\n\n" +
                "영입은 학기에 단 한 번만 진행되며,\n선택 후에는 변경할 수 없습니다.",

            _ => "학생 영입을 진행합니다."
        };
    }

    public enum RecruitmentContext
    {
        GameStart,      // 게임 시작 시 최초 영입
        FirstSemester,  // 1학기 시작 영입 (3월)
        SecondSemester  // 2학기 시작 영입 (8월)
    }
}
