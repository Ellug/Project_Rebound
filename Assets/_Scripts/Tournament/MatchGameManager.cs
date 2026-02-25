using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;
using GameData = CachedSOData;

// 경기 전체 흐름을 조율하는 컨트롤러 : 쿼터 > 공방 > 하프타임 > 경기 종료 순서를 관리
public class MatchGameManager : MonoBehaviour
{
    private const string Divider = "----------------------------------------";

    // 경기 종료 시 MatchResult를 전달. TournamentManager가 구독해 승패 처리
    public event Action<MatchResult> OnMatchFinished;

    [Header("References")]
    [SerializeField] private MatchGameUI _matchGameUi;


    [Header("Quarter Pod Config")]
    [FormerlySerializedAs("_maxPossessionsPerQuarter")]
    [SerializeField] [Min(0)] private int _maxPlayTurnsPerQuarter = 5;
    [FormerlySerializedAs("_scorePerPossessionWin")]
    [SerializeField] [Min(1)] private int _scorePerPlayTurnWin = 2;
    [SerializeField] [Min(0)] private int _benchRecoverCondition = 3;

    private readonly List<QuarterScore> _quarterScores = new(4);
    private readonly List<string> _logs = new(64);  // 경기 종료 후 MatchResult.logs로 복사됨

    private QuarterPodSimulator _quarterSimulator;
    private MatchContext _context;
    private QuarterPodSession _activeQuarterSession;     // null이면 현재 쿼터 공방 진행 없음
    private int _activeQuarterNumber;
    private bool _isMatchRunning;
    private int _progressStageIndex;                     // MatchGameStages.Default 배열 인덱스

    // MatchGameStages.Default의 래퍼: 스테이지 배열을 읽기 전용으로 노출
    private static IReadOnlyList<string> ProgressStages => MatchGameStages.Default;


    void Awake()
    {
        _quarterSimulator = CreateDefaultQuarterSimulator();
    }

    public void StartMatch(string upTeam, string downTeam, string mySchoolName)
    {
        if (string.IsNullOrWhiteSpace(upTeam) || string.IsNullOrWhiteSpace(downTeam) || string.IsNullOrWhiteSpace(mySchoolName))
        {
            WriteSystemLog("StartMatch 입력이 유효하지 않습니다.");
            return;
        }

        if (string.Equals(upTeam, downTeam, StringComparison.Ordinal))
        {
            WriteSystemLog("같은 팀끼리는 경기를 시작할 수 없습니다.");
            return;
        }

        List<Student> field = BuildFieldPlayers();
        List<Student> bench = BuildBenchPlayers(field);
        int currentDay = GameManager.Instance != null ? GameManager.Instance.DayIndex : 1;
        EnemyStatRow enemyStat = GameData.EnemyStatTable.GetOrNull(currentDay) ?? new EnemyStatRow();
        _context = new MatchContext(upTeam, downTeam, mySchoolName, field, bench, enemyStat);
        _isMatchRunning = true;
        _progressStageIndex = 0;
        _activeQuarterSession = null;
        _activeQuarterNumber = 0;
        _quarterScores.Clear();
        _logs.Clear();

        _matchGameUi.PrepareMatchGameUi(upTeam, downTeam, ProgressStages);
        UpdateProgressUi();
        RefreshLiveScoreUi();

        WriteLog(Divider);
        WriteLog($"{upTeam} VS {downTeam}");
        WriteLog(Divider);
    }

    // UI 버튼 1회 = 공방 1회 또는 다음 스테이지로 이동 (TournamentManager.OnClickProgressMySchoolMatch에서 호출)
    public void ProgressMatchStep()
    {
        if (!_isMatchRunning)
        {
            WriteSystemLog("진행 중인 경기가 없습니다.");
            return;
        }

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
                RunHalfTime(afterQuarter: 1);
                MoveToStage(2);
                break;
            case 2:
                BeginQuarter(2);
                break;
            case 3:
                RunHalfTime(afterQuarter: 2);
                MoveToStage(4);
                break;
            case 4:
                BeginQuarter(3);
                break;
            case 5:
                RunHalfTime(afterQuarter: 3);
                MoveToStage(6);
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

        if (!stepResult.isQuarterCompleted)
            return;

        ApplyQuarterResult(_activeQuarterNumber, stepResult.quarterResult);
        CompleteQuarter(_activeQuarterNumber);
    }

    public void AbortCurrentMatch()
    {
        _isMatchRunning = false;
        _progressStageIndex = 0;
        _activeQuarterSession = null;
        _activeQuarterNumber = 0;
        _quarterScores.Clear();
        _logs.Clear();
    }

    private void BeginQuarter(int quarter)
    {
        QuarterPodBeginResult beginResult = _quarterSimulator.BeginQuarter(_context, quarter);
        _activeQuarterSession = beginResult.session;
        _activeQuarterNumber = quarter;
        WriteQuarterLogs(beginResult.logs);
    }

    private void ApplyQuarterResult(int quarter, QuarterSimulationResult quarterResult)
    {
        int myQuarterScore = Mathf.Max(0, quarterResult.mySchoolScore);
        int opponentQuarterScore = Mathf.Max(0, quarterResult.opponentScore);

        _context.AddQuarterScore(myQuarterScore, opponentQuarterScore);
        _quarterScores.Add(new QuarterScore(quarter, myQuarterScore, opponentQuarterScore));
        _matchGameUi.SetMatchScore(_context.GetLeftTeamScore(), _context.GetRightTeamScore());
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

        int leftScore = _context.IsMySchoolUpTeam ? myScore : opponentScore;
        int rightScore = _context.IsMySchoolUpTeam ? opponentScore : myScore;
        _matchGameUi.SetMatchScore(leftScore, rightScore);
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

    private void RunHalfTime(int afterQuarter)
    {
        WriteLog(Divider);
        WriteLog($"{afterQuarter}쿼터 종료 후 작전타임");
        WriteLog(Divider);
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
        _matchGameUi.AppendMatchLog(message);
    }

    private void WriteSystemLog(string message)
    {
        string formatted = $"[SYSTEM] {message}";
        _logs.Add(formatted);
        Debug.Log(formatted);
        _matchGameUi.AppendMatchLog(formatted);
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
}
