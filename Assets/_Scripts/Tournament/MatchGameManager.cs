using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;
using GameData = CachedSOData;

// 경기 전체 흐름을 조율하는 컨트롤러 : 쿼터 > 공방 > 하프타임 > 경기 종료 순서를 관리
public class MatchGameManager : MonoBehaviour
{
    private const string Divider = "-------------------------------------";
    private const string MyTeamLogColorHex = "#1E90FF";
    private const string OpponentTeamLogColorHex = "#E80000";
    private const string SystemLogColorHex = "#A3A3A3";
    private const string SystemLogPrefix = "[SYSTEM]";
    private const string HighSchoolSuffix = "고등학교";
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

    private readonly List<QuarterScore> _quarterScores = new(4);
    private readonly List<string> _logs = new(64);  // 경기 종료 후 MatchResult.logs로 복사됨

    private QuarterPodSimulator _quarterSimulator;
    private MatchContext _context;
    private QuarterPodSession _activeQuarterSession;     // null이면 현재 쿼터 공방 진행 없음
    private int _activeQuarterNumber;
    private bool _isMatchRunning;
    private bool _isWaitingHalfTime;                     // 하프타임 선택 대기 중
    private int _halfTimeNextStage;                      // 하프타임 완료 후 이동할 스테이지
    private int _progressStageIndex;                     // MatchGameStages.Default 배열 인덱스

    // MatchGameStages.Default의 래퍼: 스테이지 배열을 읽기 전용으로 노출
    private static IReadOnlyList<string> ProgressStages => MatchGameStages.Default;

    void Awake()
    {
        _quarterSimulator = CreateDefaultQuarterSimulator();
        if (_halfTimeSelectionUi != null)
            _halfTimeSelectionUi.OnSelectionMade += HandleHalfTimeSelection;
    }

    void OnDestroy()
    {
        if (_halfTimeSelectionUi != null)
            _halfTimeSelectionUi.OnSelectionMade -= HandleHalfTimeSelection;
    }

    public void StartMatch(string upTeam, string downTeam, string mySchoolName)
    {
        if (string.IsNullOrWhiteSpace(upTeam) || string.IsNullOrWhiteSpace(downTeam) || string.IsNullOrWhiteSpace(mySchoolName))
        {
            WriteSystemLog("StartMatch 입력이 유효하지 않습니다.");
            return;
        }

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
        _activeQuarterSession = null;
        _activeQuarterNumber = 0;
        _quarterScores.Clear();
        _logs.Clear();

        _matchGameUi.PrepareMatchGameUi(FormatTeamNameForDisplay(upTeam), FormatTeamNameForDisplay(downTeam), ProgressStages);
        UpdateProgressUi();
        RefreshLiveScoreUi();

        // 경기 시작 기본 이미지
        _matchGameUi.SetMatchContextImage(DefaultMatchImageId);
        
        SoundManager.Instance.PlayBGM(103);
        SoundManager.Instance.PlayEffect(301);

        WriteLog(Divider);
        WriteLog($"{upTeam} VS {downTeam}");
        WriteLog(Divider);

        // 경기 시뮬레이션 시작 세이브
        if (SaveManager.Instance != null)
        {
            SaveManager.Instance.AutoSaveByBranch("경기 시뮬레이션 시작");
        }
    }

    // UI 버튼 1회 = 공방 1회 또는 다음 스테이지로 이동 (TournamentManager.OnClickProgressMySchoolMatch에서 호출)
    public void ProgressMatchStep()
    {
        if (!_isMatchRunning)
        {
            WriteSystemLog("진행 중인 경기가 없습니다.");
            return;
        }

        // 하프타임 선택 대기 중이면 진행 차단
        if (_isWaitingHalfTime)
            return;

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
        WriteQuarterLogs(stepResult.logs);
        RefreshLiveScoreUi();

        // 공방 결과 상황 이미지 교체
        if (!string.IsNullOrEmpty(stepResult.contextImageId))
        {
            _matchGameUi.SetMatchContextImage(stepResult.contextImageId);
        }

        if (!stepResult.isQuarterCompleted)
            return;

        ApplyQuarterResult(_activeQuarterNumber, stepResult.quarterResult);
        CompleteQuarter(_activeQuarterNumber);
    }

    public void AbortCurrentMatch()
    {
        _isMatchRunning = false;
        _isWaitingHalfTime = false;
        _halfTimeNextStage = 0;
        _progressStageIndex = 0;
        _activeQuarterSession = null;
        _activeQuarterNumber = 0;
        _quarterScores.Clear();
        _logs.Clear();
    }

    private void BeginQuarter(int quarter)
    {
        SoundManager.Instance.PlayBGM(103);
        if (quarter > 1) SoundManager.Instance.PlayEffect(301);

        QuarterPodBeginResult beginResult = _quarterSimulator.BeginQuarter(_context, quarter);
        _activeQuarterSession = beginResult.session;
        _activeQuarterNumber = quarter;
        WriteQuarterLogs(beginResult.logs);

        // 쿼터 시작 이미지
        _matchGameUi.SetMatchContextImage(QuarterWhistleImageId);
    }

    private void ApplyQuarterResult(int quarter, QuarterSimulationResult quarterResult)
    {
        int myQuarterScore = Mathf.Max(0, quarterResult.mySchoolScore);
        int opponentQuarterScore = Mathf.Max(0, quarterResult.opponentScore);

        _context.AddQuarterScore(myQuarterScore, opponentQuarterScore);
        _quarterScores.Add(new QuarterScore(quarter, myQuarterScore, opponentQuarterScore));
        _matchGameUi.SetMatchScore(_context.GetLeftTeamScore(), _context.GetRightTeamScore());

        // 쿼터 종료 이미지
        _matchGameUi.SetMatchContextImage(QuarterWhistleImageId);

        SoundManager.Instance.PlayEffect(302);
        SoundManager.Instance.FadeOutBGM(0.2f);

        // 쿼터별 결과 세이브
        if (SaveManager.Instance != null)
        {
            SaveManager.Instance.AutoSaveByBranch($"{quarter}쿼터 결과");
        }
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
        WriteLog(Divider);
        WriteLog(ApplyAnnouncementBold($"{afterQuarter}쿼터 종료 후 작전타임"));
        WriteLog(Divider);

        // 하프타임 선택지 노출 전 세이브
        if (SaveManager.Instance != null)
        {
            SaveManager.Instance.AutoSaveByBranch("하프타임 선택 전");
        }

        // 하프타임 이미지
        _matchGameUi.SetMatchContextImage(HalfTimeImageId);

        _halfTimeSelectionUi.Open();
        _matchGameUi.ScrollMatchLogToBottom();
    }

    // 하프타임 선택에 대한 이벤트
    // 여기서 하프타임 데이터 테이블 id 받으면 브릿지 연결해야할듯
    private void HandleHalfTimeSelection(string selectionText)
    {
        WriteLog(selectionText);
        WriteLog(ApplyAnnouncementBold("작전타임 종료"));
        _isWaitingHalfTime = false;

        MoveToStage(_halfTimeNextStage); // 먼저 스테이지 이동 후 세이브

        if (SaveManager.Instance != null)
        {
            SaveManager.Instance.AutoSaveByBranch("하프타임 선택 후");
        }
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
        if (!_isMatchRunning)
            return;

        _isMatchRunning = false;

        string winnerTeamName = ResolveWinnerTeamName();
        WriteLog(Divider);
        WriteLog("경기 종료");
        WriteLog($"최종 스코어: {_context.MySchoolName} {_context.MySchoolScore} - {_context.OpponentScore} {_context.OpponentTeamName}");
        WriteLog($"승자: {winnerTeamName}");
        WriteLog(Divider);

        MatchResult result = new()
        {
            winnerTeamName = winnerTeamName,
            finalScore = new MatchScore(_context.MySchoolScore, _context.OpponentScore),
            quarterScores = new List<QuarterScore>(_quarterScores),
            logs = new List<string>(_logs)
        };

        // 경기 시뮬레이션 종료 세이브
        if (SaveManager.Instance != null)
        {
            SaveManager.Instance.AutoSaveByBranch("경기 시뮬레이션 종료");
        }

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
        WriteSystemLog($"동점 판정(Stub 랜덤): {tieWinner}");
        return tieWinner;
    }

    private void WriteQuarterLogs(IReadOnlyList<QuarterLogEntry> logs)
    {
        for (int i = 0; i < logs.Count; i++)
        {
            QuarterLogEntry logEntry = logs[i];
            if (logEntry.isSystem)
                WriteSystemLog(logEntry.message);
            else
                WriteLog(logEntry.message);
        }
    }

    private void WriteLog(string message)
    {
        _logs.Add(message);
        Debug.Log(message);
        AppendLogToUi(message, isSystem: false);
    }

    private void WriteSystemLog(string message)
    {
        string formatted = $"{SystemLogPrefix} {message}";
        _logs.Add(formatted);
        Debug.Log(formatted);
        AppendLogToUi(formatted, isSystem: true);
    }

    private void AppendLogToUi(string rawMessage, bool isSystem)
    {
        _matchGameUi.AppendMatchLog(FormatLogForUi(rawMessage, isSystem));
    }

    private string FormatLogForUi(string rawMessage, bool isSystem)
    {
        if (string.IsNullOrEmpty(rawMessage))
            return rawMessage;

        string formatted = rawMessage;
        formatted = ReplaceTeamNameToken(formatted, _context != null ? _context.MySchoolName : null, isSystem ? null : MyTeamLogColorHex);
        formatted = ReplaceTeamNameToken(formatted, _context != null ? _context.OpponentTeamName : null, isSystem ? null : OpponentTeamLogColorHex);

        if (isSystem)
            formatted = WrapColor(formatted, SystemLogColorHex);

        return formatted;
    }

    private static string ReplaceTeamNameToken(string message, string fullTeamName, string colorHex)
    {
        if (string.IsNullOrEmpty(message) || string.IsNullOrWhiteSpace(fullTeamName))
            return message;

        string shortTeamName = ShortenSchoolName(fullTeamName);
        string plainTag = $"[{shortTeamName}]";
        string replacement = string.IsNullOrEmpty(colorHex) ? plainTag : WrapColor(plainTag, colorHex);

        message = message.Replace($"[{fullTeamName}]", replacement);
        message = message.Replace(fullTeamName, replacement);

        if (!string.IsNullOrEmpty(colorHex))
            message = message.Replace(plainTag, replacement);

        return message;
    }

    private static string ShortenSchoolName(string schoolName)
    {
        if (string.IsNullOrWhiteSpace(schoolName))
            return string.Empty;

        string trimmed = schoolName.Trim();
        if (trimmed.EndsWith(HighSchoolSuffix, StringComparison.Ordinal))
        {
            string withoutSuffix = trimmed[..^HighSchoolSuffix.Length].Trim();
            if (!string.IsNullOrEmpty(withoutSuffix))
                return withoutSuffix;
        }

        return trimmed;
    }

    private static string WrapColor(string message, string colorHex)
    {
        return $"<color={colorHex}>{message}</color>";
    }

    private static string ApplyAnnouncementBold(string message)
    {
        return $"<b>{message}</b>";
    }

    private static bool IsSystemLog(string message)
    {
        return !string.IsNullOrEmpty(message) && message.StartsWith(SystemLogPrefix, StringComparison.Ordinal);
    }

    // 슬롯에 배치된 학생을 출전 선수로 반환
    private static List<Student> BuildFieldPlayers()
    {
        var result = new List<Student>();
        if (StudentManager.Instance == null) return result;

        foreach (var pair in StudentManager.Instance.SlotAssignments)
        {
            if (pair.Value != null)
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
            logs = new List<string>(_logs),
        };

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

        // 로그 복원
        _logs.Clear();
        if (data.logs != null)
            _logs.AddRange(data.logs);

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
        List<Student> field = BuildFieldPlayers();
        List<Student> bench = BuildBenchPlayers(field);
        int currentDay = GameManager.Instance != null ? GameManager.Instance.DayIndex : 1;
        EnemyStatTableSO enemyTable = GameData.Get<EnemyStatTableSO>();
        EnemyStatRow enemyStat = enemyTable.GetOrNull(currentDay) ?? new EnemyStatRow();
        _context = new MatchContext(data.upTeam, data.downTeam, data.mySchoolName, field, bench, enemyStat);
        _context.RestoreScore(myTotal, opponentTotal);

        // UI 갱신
        _matchGameUi.PrepareMatchGameUi(
            FormatTeamNameForDisplay(_context.UpTeam),
            FormatTeamNameForDisplay(_context.DownTeam),
            ProgressStages);

        foreach (string log in _logs)
            AppendLogToUi(log, IsSystemLog(log));

        UpdateProgressUi();
        _matchGameUi.SetMatchScore(_context.GetLeftTeamScore(), _context.GetRightTeamScore());

        // 복원 직후 기본 이미지
        _matchGameUi.SetMatchContextImage(DefaultMatchImageId);

        Debug.Log($"[MatchGameManager] 경기 복원 완료 — 스테이지 {_progressStageIndex}, 쿼터 {_quarterScores.Count}개 복원");
    }
}
