using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;
using GameData = CachedSOData;

// 경기 전체 흐름을 조율하는 컨트롤러 : 쿼터 > 공방 > 하프타임 > 경기 종료 순서를 관리
public class MatchGameManager : MonoBehaviour
{
    private const string SystemLogPrefix = "[SYSTEM]";
    private const string DefaultMatchImageId = "EventGame00_image01";
    private const string QuarterWhistleImageId = "EventGame00_img_whistle";
    private const string HalfTimeImageId = "EventGame00_img_halftime";

    // 경기 종료 시 MatchResult를 전달. TournamentManager가 구독해 승패 처리
    public event Action<MatchResult> OnMatchFinished;

    [Header("References")]
    [SerializeField] private MatchGameUI _matchGameUi;
    [SerializeField] private TournamentHalfTimeSelectionUI _halfTimeSelectionUi;

    [Header("Quarter Pod Config")]
    [FormerlySerializedAs("_maxPossessionsPerQuarter")]
    [SerializeField][Min(0)] private int _maxPlayTurnsPerQuarter = 5;
    [FormerlySerializedAs("_scorePerPossessionWin")]
    [SerializeField][Min(1)] private int _scorePerPlayTurnWin = 2;
    [SerializeField][Min(0)] private int _benchRecoverCondition = 3;    
    [SerializeField][Min(0f)] private float _logTypingCharactersPerSecond = 45f; // 로그 타이핑 속도

    private readonly List<QuarterScore> _quarterScores = new(4);
    private readonly Dictionary<int, StudentStatSnapshot> _studentStatSnapshots = new();
    private readonly Dictionary<int, PendingAbnormalInfo> _pendingAbnormals = new();
    private readonly SuddenEvent _suddenEvent = new SuddenEvent();
    private static readonly AbnormalStatusEffect _abnormalStatusEffect = new AbnormalStatusEffect();

    private QuarterPodSimulator _quarterSimulator;
    private MatchGameLogPresenter _matchLogPresenter;
    private MatchContext _context;
    private QuarterPodSession _activeQuarterSession;     // null이면 현재 쿼터 공방 진행 없음
    private int _activeQuarterNumber;
    private bool _isMatchRunning;
    private bool _isWaitingHalfTime;                     // 하프타임 선택 대기 중
    private int _halfTimeNextStage;                      // 하프타임 완료 후 이동할 스테이지
    private int _progressStageIndex;                     // MatchGameStages.Default 배열 인덱스
    private bool _hasStudentSnapshot;
    private bool _rollQuarterInjury;

    private struct StudentStatSnapshot
    {
        public int mental;
        public int shoot;
        public int speed;
        public int jump;
        public int stamina;
    }

    private struct PendingAbnormalInfo
    {
        public Student.AbnormalType abnormalType;
        public int remainTurn;

        public PendingAbnormalInfo(Student.AbnormalType abnormalType, int remainTurn)
        {
            this.abnormalType = abnormalType;
            this.remainTurn = remainTurn;
        }
    }

    // MatchGameStages.Default의 래퍼: 스테이지 배열을 읽기 전용으로 노출
    private static IReadOnlyList<string> ProgressStages => MatchGameStages.Default;

    void Awake()
    {
        _quarterSimulator = CreateDefaultQuarterSimulator();
        // 매치 로그 모듈 초기화
        _matchLogPresenter = new MatchGameLogPresenter(this, _matchGameUi, _logTypingCharactersPerSecond, SystemLogPrefix, PlayQuarterEndCue);
        if (_halfTimeSelectionUi != null)
            _halfTimeSelectionUi.OnSelectionMade += HandleHalfTimeSelection;
    }

    void OnDestroy()
    {
        _matchLogPresenter?.ResetUiLogTypingState();
        RestoreStudentStatsFromSnapshot();

        if (_halfTimeSelectionUi != null)
            _halfTimeSelectionUi.OnSelectionMade -= HandleHalfTimeSelection;
    }

    public void StartMatch(string upTeam, string downTeam, string mySchoolName, bool rollQuarterInjury = false)
    {
        if (string.IsNullOrWhiteSpace(upTeam) || string.IsNullOrWhiteSpace(downTeam) || string.IsNullOrWhiteSpace(mySchoolName))
        {
            _matchLogPresenter.WriteSystemLog("StartMatch 입력이 유효하지 않습니다.");
            return;
        }

        CaptureStudentStatSnapshot();

        List<Student> field = BuildFieldPlayers();
        List<Student> bench = BuildBenchPlayers(field);
        int currentDay = GameManager.Instance != null ? GameManager.Instance.DayIndex : 1;
        EnemyStatTableSO enemyTable = GameData.Get<EnemyStatTableSO>();
        EnemyStatRow enemyStat = enemyTable.GetOrNull(currentDay) ?? new EnemyStatRow();
        _context = new MatchContext(upTeam, downTeam, mySchoolName, field, bench, enemyStat);
        _isMatchRunning = true;
        _isWaitingHalfTime = false;
        _halfTimeNextStage = 0;
        _progressStageIndex = 0;
        _rollQuarterInjury = rollQuarterInjury;
        _pendingAbnormals.Clear();
        _activeQuarterSession = null;
        _activeQuarterNumber = 0;
        _quarterScores.Clear();
        _matchLogPresenter.ClearAll();
        _matchLogPresenter.SetTeamNames(_context.MySchoolName, _context.OpponentTeamName);

        _matchGameUi.PrepareMatchGameUi(FormatTeamNameForDisplay(upTeam), FormatTeamNameForDisplay(downTeam), ProgressStages);
        UpdateProgressUi();
        RefreshLiveScoreUi();

        // 경기 시작 기본 이미지
        _matchGameUi.SetMatchContextImage(DefaultMatchImageId);
        
        SoundManager.Instance.PlayBGM(103);

        _matchLogPresenter.WriteLog(MatchGameLogTokens.Divider, MatchLogStyle.Divider);
        _matchLogPresenter.WriteLog($"{upTeam} VS {downTeam}");
        _matchLogPresenter.WriteLog(MatchGameLogTokens.Divider, MatchLogStyle.Divider);

        // 경기 시뮬레이션 시작 세이브
        if (SaveManager.Instance != null)
            SaveManager.Instance.AutoSaveByBranch("경기 시뮬레이션 시작");
    }

    // UI 버튼 1회 = 공방 1회 또는 다음 스테이지로 이동 (TournamentManager.OnClickProgressMySchoolMatch에서 호출)
    public void ProgressMatchStep()
    {
        // 진행 버튼 1회 입력으로 현재 로그 타이핑 우선 완료
        if (_matchLogPresenter.TryCompletePendingUiLogs()) return;
        if (_matchLogPresenter.TryRunPendingFlowActionsAfterUiLogs()) return;

        if (!_isMatchRunning)
        {
            _matchLogPresenter.WriteSystemLog("진행 중인 경기가 없습니다.");
            return;
        }

        // 하프타임 선택 대기 중이면 진행 차단
        if (_isWaitingHalfTime) return;

        // 공방 세션이 활성화 중이면 쿼터 내부 스텝만 진행
        if (_activeQuarterSession != null)
        {
            ProgressActiveQuarterPlayTurn();
            return;
        }

        switch (_progressStageIndex)
        {
            case 0:
                BeginQuarter(1);
                break;
            case 1:
                RunHalfTime(afterQuarter: 1, nextStage: 2);
                break;
            case 2:
                BeginQuarter(2);
                break;
            case 3:
                RunHalfTime(afterQuarter: 2, nextStage: 4);
                break;
            case 4:
                BeginQuarter(3);
                break;
            case 5:
                RunHalfTime(afterQuarter: 3, nextStage: 6);
                break;
            case 6:
                BeginQuarter(4);
                break;
            default:
                FinishMatch();
                break;
        }
    }

    private void ProgressActiveQuarterPlayTurn()
    {
        QuarterPodStepResult stepResult = _quarterSimulator.ProgressPlayTurn(_context, _activeQuarterSession);
        int completedQuarter = _activeQuarterNumber;
        _matchLogPresenter.WriteQuarterLogs(stepResult.logs);
        RefreshLiveScoreUi();

        // 공방 결과 상황 이미지 교체
        if (TryResolvePlayTurnContextImageId(stepResult.contextVisualCue, out string contextImageId))
        {
            _matchGameUi.SetMatchContextImage(contextImageId);
        }

        if (!stepResult.isQuarterCompleted)
            return;

        QuarterSimulationResult completedQuarterResult = stepResult.quarterResult;

        // 쿼터 종료 대기 중 동일 세션 재진입 차단
        _activeQuarterSession = null;
        _activeQuarterNumber = 0;

        _matchLogPresenter.EnqueueFlowActionAfterUiLogs(() =>
        {
            if (!_isMatchRunning) return;

            ApplyQuarterResult(completedQuarter, completedQuarterResult);
            CompleteQuarter(completedQuarter);
        });
    }

    public void AbortCurrentMatch()
    {
        RestoreStudentStatsFromSnapshot();

        _isMatchRunning = false;
        _isWaitingHalfTime = false;
        _halfTimeNextStage = 0;
        _progressStageIndex = 0;
        _activeQuarterSession = null;
        _activeQuarterNumber = 0;
        _rollQuarterInjury = false;
        _pendingAbnormals.Clear();
        _quarterScores.Clear();
        _matchLogPresenter.ClearAll();
    }

    // 친선전 부상 판정
    public void QueueFriendlyStartInjury()
    {
        if (_context == null)
            return;

        QueueInjuryForStudents(_context.FieldPlayers, isFriendlyStart: true);
    }

    // 경기 종료 후 부상 반영
    public void ApplyPendingAbnormals()
    {
        if (StudentManager.Instance == null || StudentManager.Instance.Students == null)
            return;

        foreach (Student student in StudentManager.Instance.Students)
        {
            if (student == null)
                continue;

            if (!_pendingAbnormals.TryGetValue(student.id, out PendingAbnormalInfo pending))
                continue;

            _suddenEvent.ApplyAbnormal(student, pending.abnormalType, pending.remainTurn);
            StudentManager.Instance.NotifyStudentModified(student);
        }

        _pendingAbnormals.Clear();
    }

    private void BeginQuarter(int quarter)
    {
        SoundManager.Instance.PlayBGM(103);

        if (_rollQuarterInjury)
            QueueInjuryForStudents(_context.FieldPlayers, isFriendlyStart: false);

        QuarterPodBeginResult beginResult = _quarterSimulator.BeginQuarter(_context, quarter);
        _activeQuarterSession = beginResult.session;
        _activeQuarterNumber = quarter;
        _matchLogPresenter.WriteQuarterLogs(beginResult.logs);

        // 쿼터 시작 이미지
        _matchGameUi.SetMatchContextImage(QuarterWhistleImageId);
        SoundManager.Instance.PlayEffect(301);
    }

    private void ApplyQuarterResult(int quarter, QuarterSimulationResult quarterResult)
    {
        int myQuarterScore = Mathf.Max(0, quarterResult.mySchoolScore);
        int opponentQuarterScore = Mathf.Max(0, quarterResult.opponentScore);

        _context.AddQuarterScore(myQuarterScore, opponentQuarterScore);
        _quarterScores.Add(new QuarterScore(quarter, myQuarterScore, opponentQuarterScore));
        _matchGameUi.SetMatchScore(_context.GetLeftTeamScore(), _context.GetRightTeamScore());

        // 쿼터별 결과 세이브
        if (SaveManager.Instance != null)
            SaveManager.Instance.AutoSaveByBranch($"{quarter}쿼터 결과");
    }

    // 공방 진행 중에도 현재 쿼터 점수를 누적 합산해 실시간으로 스코어보드에 반영
    private void RefreshLiveScoreUi()
    {
        int myScore = _context.MySchoolScore;
        int opponentScore = _context.OpponentScore;

        if (_activeQuarterSession != null)
        {
            myScore += Mathf.Max(0, _activeQuarterSession.MyQuarterScore);
            opponentScore += Mathf.Max(0, _activeQuarterSession.OpponentQuarterScore);
        }

        _matchGameUi.SetMatchScore(myScore, opponentScore);
    }

    private void CompleteQuarter(int quarter)
    {
        _activeQuarterSession = null;
        _activeQuarterNumber = 0;

        if (quarter >= 4)
        {
            FinishMatch();
            return;
        }

        MoveToStage(MatchGameStages.GetHalfTimeStageIndex(quarter));
    }

    private void RunHalfTime(int afterQuarter, int nextStage)
    {
        _isWaitingHalfTime = true;
        _halfTimeNextStage = nextStage;
        _matchLogPresenter.WriteLog(MatchGameLogTokens.Divider, MatchLogStyle.Divider);
        _matchLogPresenter.WriteLog($"{afterQuarter}쿼터 종료 후 작전타임", MatchLogStyle.Announcement);
        _matchLogPresenter.WriteLog(MatchGameLogTokens.Divider, MatchLogStyle.Divider);

        // 하프타임 선택지 노출 전 세이브
        if (SaveManager.Instance != null)
            SaveManager.Instance.AutoSaveByBranch("하프타임 선택 전");

        // 하프타임 이미지
        _matchGameUi.SetMatchContextImage(HalfTimeImageId);

        _halfTimeSelectionUi.Open();
        _matchGameUi.ScrollMatchLogToBottom();
    }

    // 하프타임 선택에 대한 이벤트
    // 여기서 하프타임 데이터 테이블 id 받으면 브릿지 연결해야할듯
    private void HandleHalfTimeSelection(string selectionText)
    {
        _matchLogPresenter.WriteLog(selectionText);
        _matchLogPresenter.WriteLog(MatchGameLogTokens.Divider, MatchLogStyle.Divider);
        _matchLogPresenter.WriteLog("작전타임 종료", MatchLogStyle.Announcement);
        _matchLogPresenter.WriteLog(MatchGameLogTokens.Divider, MatchLogStyle.Divider);
        _isWaitingHalfTime = false;

        MoveToStage(_halfTimeNextStage); // 먼저 스테이지 이동 후 세이브

        if (SaveManager.Instance != null)
            SaveManager.Instance.AutoSaveByBranch("하프타임 선택 후");
    }

    private void MoveToStage(int stageIndex)
    {
        _progressStageIndex = Mathf.Clamp(stageIndex, 0, ProgressStages.Count - 1);
        UpdateProgressUi();
    }

    private void UpdateProgressUi()
    {
        _matchGameUi.SetQuarterProgress(ProgressStages, _progressStageIndex);
        _matchGameUi.SetCurrentMatchState(ProgressStages[_progressStageIndex]);
    }

    private void FinishMatch()
    {
        if (!_isMatchRunning) return;

        RestoreStudentStatsFromSnapshot();
        _isMatchRunning = false;

        string winnerTeamName = ResolveWinnerTeamName();
        _matchLogPresenter.WriteLog(MatchGameLogTokens.Divider, MatchLogStyle.Divider);
        _matchLogPresenter.WriteLog("경기 종료");
        _matchLogPresenter.WriteLog($"최종 스코어: {_context.MySchoolName} {_context.MySchoolScore} - {_context.OpponentScore} {_context.OpponentTeamName}");
        _matchLogPresenter.WriteLog($"승자: {winnerTeamName}");
        _matchLogPresenter.WriteLog(MatchGameLogTokens.Divider, MatchLogStyle.Divider);


        if (FriendlyMatchManager.Instance != null && _context != null)
        {
            FriendlyMatchManager.Instance.UnlockSchool(_context.OpponentTeamName);
        }
        MatchResult result = new()
        {
            winnerTeamName = winnerTeamName,
            finalScore = new MatchScore(_context.MySchoolScore, _context.OpponentScore),
            quarterScores = new List<QuarterScore>(_quarterScores),
            logs = new List<string>(_matchLogPresenter.Logs)
        };

        // 경기 시뮬레이션 종료 세이브
        if (SaveManager.Instance != null)
            SaveManager.Instance.AutoSaveByBranch("경기 시뮬레이션 종료");

        OnMatchFinished?.Invoke(result);
    }

    // 단순 점수 비교로 승자를 판정하고 동점이면 랜덤으로 결정한다.
    private string ResolveWinnerTeamName()
    {
        if (_context.MySchoolScore > _context.OpponentScore)
            return _context.MySchoolName;

        if (_context.MySchoolScore < _context.OpponentScore)
            return _context.OpponentTeamName;

        string tieWinner = UnityEngine.Random.value < 0.5f ? _context.MySchoolName : _context.OpponentTeamName;
        _matchLogPresenter.WriteSystemLog($"동점 판정(Stub 랜덤): {tieWinner}");
        return tieWinner;
    }

    private static bool TryResolvePlayTurnContextImageId(MatchContextVisualCue cue, out string imageId)
    {
        switch (cue)
        {
            case MatchContextVisualCue.PlayTurnDefault:
                imageId = "EventGameTable04_img";
                return true;
            case MatchContextVisualCue.PlayTurnAttackFirst:
                imageId = "EventGame00_img_attack_first";
                return true;
            case MatchContextVisualCue.PlayTurnAttackSecond:
                imageId = "EventGame00_img_attack_second";
                return true;
            case MatchContextVisualCue.PlayTurnAttackSuccess:
                imageId = "EventGame00_img_attack_success";
                return true;
            case MatchContextVisualCue.PlayTurnAttackFailed:
                imageId = "EventGame00_img_attack_failed";
                return true;
            case MatchContextVisualCue.PlayTurnDefenseSuccess:
                imageId = "EventGame00_img_defense_success";
                return true;
            case MatchContextVisualCue.PlayTurnDefenseFailed:
                imageId = "EventGame00_img_defense_failed";
                return true;
            default:
                imageId = null;
                return false;
        }
    }

    private void PlayQuarterEndCue()
    {
        _matchGameUi.SetMatchContextImage(QuarterWhistleImageId);
        SoundManager.Instance.PlayEffect(302);
        SoundManager.Instance.FadeOutBGM(0.2f);
    }

    // 슬롯에 배치된 학생을 출전 선수로 반환
    private static List<Student> BuildFieldPlayers()
    {
        var result = new List<Student>();
        if (StudentManager.Instance == null) return result;

        foreach (var pair in StudentManager.Instance.SlotAssignments)
        {
            if (pair.Value != null && !_abnormalStatusEffect.IsMatchBlocked(pair.Value))
                result.Add(pair.Value);
        }
        return result;
    }

    // 전체 학생 중 출전 선수를 제외한 나머지를 벤치로 반환
    private static List<Student> BuildBenchPlayers(List<Student> fieldPlayers)
    {
        var result = new List<Student>();
        if (StudentManager.Instance == null) return result;

        foreach (Student s in StudentManager.Instance.Students)
        {
            if (_abnormalStatusEffect.IsMatchBlocked(s))
                continue;

            if (!fieldPlayers.Contains(s))
                result.Add(s);
        }
        return result;
    }

    private QuarterPodSimulator CreateDefaultQuarterSimulator()
    {
        return new QuarterPodSimulator(_maxPlayTurnsPerQuarter, _scorePerPlayTurnWin, _benchRecoverCondition);
    }

    // UI 표시 전용으로 "고등학교" 앞에 공백을 보정
    private static string FormatTeamNameForDisplay(string teamName)
    {
        if (string.IsNullOrEmpty(teamName))
            return teamName;

        const string suffix = "고등학교";
        int idx = teamName.IndexOf(suffix, StringComparison.Ordinal);
        if (idx > 0 && teamName[idx - 1] != ' ')
            return teamName[..idx] + " " + teamName[idx..];

        return teamName;
    }

    // SaveManager.CollectMatchSimData()에서 호출
    public SavedMatchSimData CollectSaveData()
    {
        // 경기 진행 중이 아니면 빈 데이터 반환
        if (!_isMatchRunning)
            return new SavedMatchSimData { isMatchRunning = false };

        SavedMatchSimData data = new()
        {
            isMatchRunning = true,
            upTeam = _context.MySchoolName,
            downTeam = _context.OpponentTeamName,
            mySchoolName = _context.MySchoolName,
            progressStageIndex = _progressStageIndex,
            rollQuarterInjury = _rollQuarterInjury,
            logs = new List<string>(_matchLogPresenter.Logs),
        };

        // 저장
        foreach (KeyValuePair<int, PendingAbnormalInfo> pair in _pendingAbnormals)
        {
            data.pendingAbnormals.Add(new SavedPendingAbnormalData
            {
                studentId = pair.Key,
                abnormalState = (int)pair.Value.abnormalType,
                abnormalRemainTurn = pair.Value.remainTurn
            });
        }

        if (!_hasStudentSnapshot)
            CaptureStudentStatSnapshot();

        foreach (KeyValuePair<int, StudentStatSnapshot> pair in _studentStatSnapshots)
        {
            StudentStatSnapshot snapshot = pair.Value;
            data.studentStatSnapshots.Add(new SavedMatchStudentStatSnapshot
            {
                studentId = pair.Key,
                mental = snapshot.mental,
                shoot = snapshot.shoot,
                speed = snapshot.speed,
                jump = snapshot.jump,
                stamina = snapshot.stamina
            });
        }

        foreach (QuarterScore qs in _quarterScores)
        {
            data.quarterScores.Add(new SavedQuarterScore
            {
                quarter = qs.quarter,
                myScore = qs.mySchoolScore,
                opponentScore = qs.opponentScore,
            });
        }

        return data;
    }

    // SaveManager.ApplyMatchSimData()에서 호출
    // 복원 시점: 하프타임 선택 전 or 쿼터 결과 직후 — 쿼터 점수/진행도만 복원하고 UI 갱신
    public void RestoreSaveData(SavedMatchSimData data)
    {
        if (data == null || !data.isMatchRunning)
            return;

        // 기본 상태 복원
        _isMatchRunning = true;
        _isWaitingHalfTime = false;
        _halfTimeNextStage = 0;
        _activeQuarterSession = null;
        _activeQuarterNumber = 0;
        _progressStageIndex = data.progressStageIndex;
        _rollQuarterInjury = data.rollQuarterInjury;
        _pendingAbnormals.Clear();

        if (data.pendingAbnormals != null)
        {
            foreach (SavedPendingAbnormalData pending in data.pendingAbnormals)
            {
                if (pending == null)
                    continue;

                _pendingAbnormals[pending.studentId] = new PendingAbnormalInfo(
                    (Student.AbnormalType)pending.abnormalState,
                    pending.abnormalRemainTurn
                );
            }
        }

        // 쿼터 점수 복원
        _quarterScores.Clear();
        int myTotal = 0;
        int opponentTotal = 0;

        foreach (SavedQuarterScore qs in data.quarterScores)
        {
            _quarterScores.Add(new QuarterScore(qs.quarter, qs.myScore, qs.opponentScore));
            myTotal += qs.myScore;
            opponentTotal += qs.opponentScore;
        }

        // MatchContext 복원 — 선수 데이터는 StudentManager에서 재조회
        RestoreStudentStatSnapshotFromSaveData(data);
        List<Student> field = BuildFieldPlayers();
        List<Student> bench = BuildBenchPlayers(field);
        int currentDay = GameManager.Instance != null ? GameManager.Instance.DayIndex : 1;
        EnemyStatTableSO enemyTable = GameData.Get<EnemyStatTableSO>();
        EnemyStatRow enemyStat = enemyTable.GetOrNull(currentDay) ?? new EnemyStatRow();
        _context = new MatchContext(data.upTeam, data.downTeam, data.mySchoolName, field, bench, enemyStat);
        _context.RestoreScore(myTotal, opponentTotal);
        _matchLogPresenter.SetTeamNames(_context.MySchoolName, _context.OpponentTeamName);

        // UI 갱신
        _matchGameUi.PrepareMatchGameUi(
            FormatTeamNameForDisplay(_context.UpTeam),
            FormatTeamNameForDisplay(_context.DownTeam),
            ProgressStages);

        _matchLogPresenter.RestoreLogsAndRender(data.logs);

        UpdateProgressUi();
        _matchGameUi.SetMatchScore(_context.GetLeftTeamScore(), _context.GetRightTeamScore());

        // 복원 직후 기본 이미지
        _matchGameUi.SetMatchContextImage(DefaultMatchImageId);

        Debug.Log($"[MatchGameManager] 경기 복원 완료 — 스테이지 {_progressStageIndex}, 쿼터 {_quarterScores.Count}개 복원");
    }

    private void RestoreStudentStatSnapshotFromSaveData(SavedMatchSimData data)
    {
        _studentStatSnapshots.Clear();
        _hasStudentSnapshot = false;

        if (data?.studentStatSnapshots != null && data.studentStatSnapshots.Count > 0)
        {
            foreach (SavedMatchStudentStatSnapshot saved in data.studentStatSnapshots)
            {
                if (saved == null) continue;

                _studentStatSnapshots[saved.studentId] = new StudentStatSnapshot
                {
                    mental = saved.mental,
                    shoot = saved.shoot,
                    speed = saved.speed,
                    jump = saved.jump,
                    stamina = saved.stamina
                };
            }

            _hasStudentSnapshot = _studentStatSnapshots.Count > 0;
            return;
        }

        // 구버전 세이브 호환: 스냅샷 필드가 없으면 현재값을 기준으로 캡처
        CaptureStudentStatSnapshot();
    }

    private void CaptureStudentStatSnapshot()
    {
        _studentStatSnapshots.Clear();
        _hasStudentSnapshot = false;

        if (StudentManager.Instance == null || StudentManager.Instance.Students == null)
            return;

        foreach (Student student in StudentManager.Instance.Students)
        {
            if (student == null) continue;

            _studentStatSnapshots[student.id] = new StudentStatSnapshot
            {
                mental = student.mental,
                shoot = student.shoot,
                speed = student.speed,
                jump = student.jump,
                stamina = student.stamina
            };
        }

        _hasStudentSnapshot = _studentStatSnapshots.Count > 0;
    }

    private void RestoreStudentStatsFromSnapshot()
    {
        if (!_hasStudentSnapshot)
            return;

        if (StudentManager.Instance == null || StudentManager.Instance.Students == null)
        {
            _studentStatSnapshots.Clear();
            _hasStudentSnapshot = false;
            return;
        }

        bool hasModified = false;
        foreach (Student student in StudentManager.Instance.Students)
        {
            if (student == null) continue;
            if (!_studentStatSnapshots.TryGetValue(student.id, out StudentStatSnapshot snapshot))
                continue;

            if (student.mental != snapshot.mental ||
                student.shoot != snapshot.shoot ||
                student.speed != snapshot.speed ||
                student.jump != snapshot.jump ||
                student.stamina != snapshot.stamina)
            {
                student.mental = snapshot.mental;
                student.shoot = snapshot.shoot;
                student.speed = snapshot.speed;
                student.jump = snapshot.jump;
                student.stamina = snapshot.stamina;
                hasModified = true;
            }
        }

        if (hasModified && StudentManager.Instance.Students.Count > 0)
            StudentManager.Instance.NotifyStudentModified(StudentManager.Instance.Students[0]);

        _studentStatSnapshots.Clear();
        _hasStudentSnapshot = false;
    }

    // 경기 중 부상 판정
    private void QueueInjuryForStudents(List<Student> students, bool isFriendlyStart)
    {
        if (students == null || students.Count == 0)
            return;

        for (int i = 0; i < students.Count; i++)
        {
            Student student = students[i];
            if (student == null)
                continue;

            bool rolled;
            int duration;
            if (isFriendlyStart)
                rolled = _suddenEvent.TryRollFriendlyStartInjury(student, out duration);
            else
                rolled = _suddenEvent.TryRollQuarterStartInjury(student, out duration);

            if (!rolled)
                continue;

            _pendingAbnormals[student.id] = new PendingAbnormalInfo(Student.AbnormalType.Injury, duration);
        }
    }
}
