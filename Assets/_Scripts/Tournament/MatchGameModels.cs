using System;
using System.Collections.Generic;

// 한 경기의 최종 스코어 (우리 팀 / 상대 팀)
[Serializable]
public struct MatchScore
{
    public int mySchoolScore;
    public int opponentScore;

    public MatchScore(int mySchoolScore, int opponentScore)
    {
        this.mySchoolScore = mySchoolScore;
        this.opponentScore = opponentScore;
    }
}

// 특정 쿼터의 득점 기록
[Serializable]
public struct QuarterScore
{
    public int quarter;
    public int mySchoolScore;
    public int opponentScore;

    public QuarterScore(int quarter, int mySchoolScore, int opponentScore)
    {
        this.quarter = quarter;
        this.mySchoolScore = mySchoolScore;
        this.opponentScore = opponentScore;
    }
}

// 경기 종료 후 최종 결과 데이터 (승자, 최종 스코어, 쿼터별 기록, 로그)
[Serializable]
public sealed class MatchResult
{
    public string winnerTeamName;
    public MatchScore finalScore;
    public List<QuarterScore> quarterScores;
    public List<string> logs;
}

// 진행 중인 경기의 상태를 추적하는 컨텍스트 (누적 점수, 팀 구성 등)
public sealed class MatchContext
{
    public string UpTeam { get; }
    public string DownTeam { get; }
    public string MySchoolName { get; }
    public string OpponentTeamName { get; }

    public int MySchoolScore { get; private set; }
    public int OpponentScore { get; private set; }
    // 우리 학교가 상단(홈) 팀인지 여부
    public bool IsMySchoolUpTeam { get; }

    // 우리 팀 출전 선수(최대 5인) / 벤치 선수
    public List<Student> FieldPlayers { get; }
    public List<Student> BenchPlayers { get; }

    // 상대 팀 스탯
    public EnemyStatRow OpponentStat { get; }

    public MatchContext(string upTeam, string downTeam, string mySchoolName,
        List<Student> fieldPlayers, List<Student> benchPlayers, EnemyStatRow opponentStat)
    {
        UpTeam = upTeam;
        DownTeam = downTeam;
        MySchoolName = mySchoolName;
        IsMySchoolUpTeam = string.Equals(upTeam, mySchoolName, StringComparison.Ordinal);
        OpponentTeamName = IsMySchoolUpTeam ? downTeam : upTeam;
        MySchoolScore = 0;
        OpponentScore = 0;
        FieldPlayers = fieldPlayers ?? new List<Student>();
        BenchPlayers = benchPlayers ?? new List<Student>();
        OpponentStat = opponentStat;
    }

    // 쿼터 결과 누적 스코어에 합산
    public void AddQuarterScore(int mySchoolScore, int opponentScore)
    {
        MySchoolScore += mySchoolScore;
        OpponentScore += opponentScore;
    }

    // UI의 좌측(상단 팀) 점수 반환
    public int GetLeftTeamScore()
    {
        return IsMySchoolUpTeam ? MySchoolScore : OpponentScore;
    }

    // UI의 우측(하단 팀) 점수 반환
    public int GetRightTeamScore()
    {
        return IsMySchoolUpTeam ? OpponentScore : MySchoolScore;
    }
}

// 쿼터 시뮬레이션 결과 (득점 및 시스템 로그)
public readonly struct QuarterSimulationResult
{
    public readonly int mySchoolScore;
    public readonly int opponentScore;
    public readonly IReadOnlyList<QuarterLogEntry> logs;

    public QuarterSimulationResult(int mySchoolScore, int opponentScore, IReadOnlyList<QuarterLogEntry> logs)
    {
        this.mySchoolScore = mySchoolScore;
        this.opponentScore = opponentScore;
        this.logs = logs;
    }
}

public readonly struct QuarterLogEntry
{
    public readonly string message;
    public readonly bool isSystem;

    public QuarterLogEntry(string message, bool isSystem)
    {
        this.message = message;
        this.isSystem = isSystem;
    }
}

// BeginQuarter() 반환값: 새 세션 + 쿼터 시작 로그
public readonly struct QuarterPodBeginResult
{
    public readonly QuarterPodSession session;
    public readonly IReadOnlyList<QuarterLogEntry> logs;

    public QuarterPodBeginResult(QuarterPodSession session, IReadOnlyList<QuarterLogEntry> logs)
    {
        this.session = session;
        this.logs = logs;
    }
}

// ProgressPlayTurn() 반환값: 공방 1회 결과 + 쿼터 완료 여부
public readonly struct QuarterPodStepResult
{
    public readonly bool isQuarterCompleted;
    public readonly QuarterSimulationResult quarterResult;
    public readonly IReadOnlyList<QuarterLogEntry> logs;

    public QuarterPodStepResult(bool isQuarterCompleted, QuarterSimulationResult quarterResult, IReadOnlyList<QuarterLogEntry> logs)
    {
        this.isQuarterCompleted = isQuarterCompleted;
        this.quarterResult = quarterResult;
        this.logs = logs;
    }
}

// 쿼터 1개 동안 공방 루프를 관리하는 가변 상태 컨테이너 (세션이 완료되면 재사용하지 않음)
public sealed class QuarterPodSession
{
    public int Quarter { get; }
    public int PlayTurnCount { get; private set; }     // 현재까지 진행된 공방 횟수
    public int MaxPlayTurns { get; }                   // 쿼터 당 최대 공방 횟수
    public int ScorePerPlayTurnWin { get; }            // 공방 1회 승리 시 획득 점수
    public int BenchRecoverAmount { get; }             // 쿼터 종료 시 벤치 컨디션 회복량
    public int MyQuarterScore { get; private set; }
    public int OpponentQuarterScore { get; private set; }
    public bool IsCompleted { get; private set; }

    public QuarterPodSession(
        int quarter,
        int maxPlayTurns,
        int scorePerPlayTurnWin,
        int benchRecoverAmount)
    {
        Quarter = quarter;
        PlayTurnCount = 0;
        MaxPlayTurns = UnityEngine.Mathf.Max(0, maxPlayTurns);
        ScorePerPlayTurnWin = UnityEngine.Mathf.Max(1, scorePerPlayTurnWin);
        BenchRecoverAmount = UnityEngine.Mathf.Max(0, benchRecoverAmount);
        MyQuarterScore = 0;
        OpponentQuarterScore = 0;
        IsCompleted = false;
    }

    public void AddMyScore(int score)
    {
        MyQuarterScore = UnityEngine.Mathf.Max(0, MyQuarterScore + score);
    }

    public void AddOpponentScore(int score)
    {
        OpponentQuarterScore = UnityEngine.Mathf.Max(0, OpponentQuarterScore + score);
    }

    public void IncrementPlayTurnCount()
    {
        PlayTurnCount += 1;
    }

    public void Complete()
    {
        IsCompleted = true;
    }
}
