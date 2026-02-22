using System.Collections.Generic;
using UnityEngine;

// 쿼터 1개 책임: 쿼터 시작, 공방 루프 진행, 쿼터 종료 결과 조합
public sealed class QuarterPodSimulator
{
    private readonly int _maxPlayTurns;
    private readonly int _scorePerPlayTurnWin;
    private readonly int _benchRecoverAmount;
    private readonly RandomPlayTurnSimulator _playTurnSimulator;

    // 쿼터/공방 기본 설정으로 시뮬레이터를 생성
    public QuarterPodSimulator(int maxPlayTurns, int scorePerPlayTurnWin, int benchRecoverAmount)
    {
        _maxPlayTurns = Mathf.Max(0, maxPlayTurns);
        _scorePerPlayTurnWin = Mathf.Max(1, scorePerPlayTurnWin);
        _benchRecoverAmount = Mathf.Max(0, benchRecoverAmount);
        _playTurnSimulator = new RandomPlayTurnSimulator();
    }

    // 쿼터 세션과 시작 로그를 생성
    public QuarterPodBeginResult BeginQuarter(MatchContext context, int quarter)
    {
        QuarterPodSession session = new(
            quarter,
            _maxPlayTurns,
            _scorePerPlayTurnWin,
            _benchRecoverAmount);

        List<QuarterLogEntry> logs = new(2)
        {
            CreateNormalLog($"{quarter}쿼터 시작 연출: 누적 스코어 우리 {context.MySchoolScore} - 상대 {context.OpponentScore}, 흐름 {BuildFlowText(context.MySchoolScore - context.OpponentScore)}"),
            CreateSystemLog($"공방 루프 시작: 공방 횟수 {session.PlayTurnCount}, 최대 공방 횟수 {session.MaxPlayTurns}")
        };

        return new QuarterPodBeginResult(session, logs);
    }

    // 공방 1스텝을 진행하고 필요 시 쿼터 종료 결과를 반환
    public QuarterPodStepResult ProgressPlayTurn(MatchContext context, QuarterPodSession session)
    {
        List<QuarterLogEntry> logs = new(8);

        if (session.PlayTurnCount >= session.MaxPlayTurns)
        {
            logs.Add(CreateSystemLog($"공방 종료 조건 충족: {session.PlayTurnCount} >= {session.MaxPlayTurns}"));
            QuarterSimulationResult quarterResult = BuildQuarterResult(context, session, logs);
            session.Complete();
            return new QuarterPodStepResult(true, quarterResult, logs);
        }

        PlayTurnSimulationResult playTurnResult = _playTurnSimulator.SimulatePlayTurn(context, session);
        AppendLogs(logs, playTurnResult.logs);

        session.IncrementPlayTurnCount();
        logs.Add(CreateSystemLog($"공방 횟수 증가: {session.PlayTurnCount}/{session.MaxPlayTurns}"));

        if (session.PlayTurnCount >= session.MaxPlayTurns)
        {
            logs.Add(CreateSystemLog($"공방 종료 조건 충족: {session.PlayTurnCount} >= {session.MaxPlayTurns}"));
            QuarterSimulationResult quarterResult = BuildQuarterResult(context, session, logs);
            session.Complete();
            return new QuarterPodStepResult(true, quarterResult, logs);
        }

        return new QuarterPodStepResult(false, default, logs);
    }

    // 세션의 최종 득점을 집계하고 벤치 컨디션 회복 후 QuarterSimulationResult를 생성
    private static QuarterSimulationResult BuildQuarterResult(MatchContext context, QuarterPodSession session, List<QuarterLogEntry> logs)
    {
        int myQuarterScore = Mathf.Max(0, session.MyQuarterScore);
        int opponentQuarterScore = Mathf.Max(0, session.OpponentQuarterScore);

        int expectedMyScore = context.MySchoolScore + myQuarterScore;
        int expectedOpponentScore = context.OpponentScore + opponentQuarterScore;
        int scoreDiff = expectedMyScore - expectedOpponentScore;

        logs.Add(CreateNormalLog($"{session.Quarter}쿼터 결과: 우리 {myQuarterScore} - 상대 {opponentQuarterScore}"));
        logs.Add(CreateNormalLog($"쿼터 종료 예상 누적: 우리 {expectedMyScore} - 상대 {expectedOpponentScore}"));
        logs.Add(CreateNormalLog($"현재 경기 흐름: {BuildFlowText(scoreDiff)}"));

        // TODO(StudentData): 실제 학생/벤치 데이터 연동 후 벤치 회복 수치 적용 필요
        if (session.BenchRecoverAmount > 0)
        {
            logs.Add(CreateSystemLog($"{context.MySchoolName} 벤치 컨디션 +{session.BenchRecoverAmount} (Stub)"));
            logs.Add(CreateSystemLog($"{context.OpponentTeamName} 벤치 컨디션 +{session.BenchRecoverAmount} (Stub)"));
        }

        logs.Add(CreateNormalLog($"{session.Quarter}쿼터 종료"));

        return new QuarterSimulationResult(myQuarterScore, opponentQuarterScore, logs);
    }

    // 점수 차이를 텍스트 흐름 표현으로 변환 (±4점 기준으로 우세/열세/경합/접전)
    private static string BuildFlowText(int scoreDiff)
    {
        if (scoreDiff >= 4)
            return "우세";
        if (scoreDiff <= -4)
            return "열세";
        if (Mathf.Abs(scoreDiff) < 2)
            return "접전";
        return "경합";
    }

    // 소스 로그를 대상 로그 리스트에 순서대로 추가한다.
    private static void AppendLogs(List<QuarterLogEntry> target, IReadOnlyList<QuarterLogEntry> source)
    {
        for (int i = 0; i < source.Count; i++)
            target.Add(source[i]);
    }

    // 일반 로그 엔트리를 생성한다.
    private static QuarterLogEntry CreateNormalLog(string message)
    {
        return new QuarterLogEntry(message, isSystem: false);
    }

    // 시스템 로그 엔트리를 생성한다.
    private static QuarterLogEntry CreateSystemLog(string message)
    {
        return new QuarterLogEntry(message, isSystem: true);
    }
}
