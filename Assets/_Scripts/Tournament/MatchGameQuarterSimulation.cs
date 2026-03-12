using System.Collections.Generic;
using UnityEngine;

// 쿼터 1개 책임: 쿼터 시작, 공방 루프 진행, 쿼터 종료 결과 조합
public sealed class QuarterPodSimulator
{
    private const string Divider = "-------------------------------------";
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

    // 쿼터 세션과 시작 로그를 생성 (컨디션 0인 출전 선수 교체 포함)
    public QuarterPodBeginResult BeginQuarter(MatchContext context, int quarter)
    {
        QuarterPodSession session = new(
            quarter,
            _maxPlayTurns,
            _scorePerPlayTurnWin,
            _benchRecoverAmount);

        // 3-4 진행 로그: 쿼터 시작
        List<QuarterLogEntry> logs = new(8)
        {
            CreateNormalLog(Divider),
            CreateNormalLog(ApplyAnnouncementBold($"{quarter}쿼터 시작")),
            CreateNormalLog(BuildQuarterStartScoreLog(context, quarter)),
            CreateNormalLog(Divider),
        };

        SubstituteExhaustedPlayers(context, logs);

        return new QuarterPodBeginResult(session, logs);
    }

    // 컨디션 0인 출전 선수를 벤치 컨디션 순으로 교체
    private static void SubstituteExhaustedPlayers(MatchContext context, List<QuarterLogEntry> logs)
    {
        // 벤치를 컨디션 내림차순으로 정렬
        context.BenchPlayers.Sort((a, b) => b.condition.CompareTo(a.condition));

        for (int i = 0; i < context.FieldPlayers.Count; i++)
        {
            Student player = context.FieldPlayers[i];
            if (player.condition > 0) continue;

            // 교체 가능한 벤치 선수 탐색
            Student sub = context.BenchPlayers.Find(b => b.condition > 0);
            if (sub == null)
            {
                logs.Add(CreateNormalLog($"[{context.MySchoolName}] {player.studentName}이(가) 탈진했으나 교체할 선수가 없다."));
                continue;
            }

            context.FieldPlayers[i] = sub;
            context.BenchPlayers.Remove(sub);
            context.BenchPlayers.Add(player);
            logs.Add(CreateNormalLog($"[{context.MySchoolName}] {player.studentName}이(가) 빠지고 {sub.studentName}이(가) 들어왔다."));
            logs.Add(CreateSystemLog($"[{context.MySchoolName}] {player.studentName} 컨디션 0 → 교체 아웃"));
        }
    }

    // 공방 1스텝을 진행하고 필요 시 쿼터 종료 결과를 반환
    public QuarterPodStepResult ProgressPlayTurn(MatchContext context, QuarterPodSession session)
    {
        List<QuarterLogEntry> logs = new(8);

        if (session.PlayTurnCount >= session.MaxPlayTurns)
        {
            QuarterSimulationResult quarterResult = BuildQuarterResult(context, session, logs);
            session.Complete();
            return new QuarterPodStepResult(true, quarterResult, logs);
        }

        PlayTurnSimulationResult playTurnResult = _playTurnSimulator.SimulatePlayTurn(context, session);
        AppendLogs(logs, playTurnResult.logs);

        session.IncrementPlayTurnCount();

        if (session.PlayTurnCount >= session.MaxPlayTurns)
        {
            QuarterSimulationResult quarterResult = BuildQuarterResult(context, session, logs);
            session.Complete();
            return new QuarterPodStepResult(true, quarterResult, logs);
        }

        return new QuarterPodStepResult(false, default, logs);
    }

    // 세션의 최종 득점을 집계하고 벤치 컨디션 회복 후 QuarterSimulationResult를 생성
    private static QuarterSimulationResult BuildQuarterResult(MatchContext context, QuarterPodSession session, List<QuarterLogEntry> logs)
    {
        int expectedMyScore = context.MySchoolScore + Mathf.Max(0, session.MyQuarterScore);
        int expectedOpponentScore = context.OpponentScore + Mathf.Max(0, session.OpponentQuarterScore);
        int scoreDiff = expectedMyScore - expectedOpponentScore;

        logs.Add(CreateNormalLog(Divider));
        logs.Add(CreateNormalLog(ApplyAnnouncementBold($"{session.Quarter}쿼터 종료")));
        logs.Add(CreateNormalLog($"{context.MySchoolName} {expectedMyScore} - {expectedOpponentScore} {context.OpponentTeamName} ({BuildFlowText(scoreDiff)})"));
        logs.Add(CreateNormalLog(Divider));

        foreach (Student s in context.BenchPlayers)
        {
            int recover = Random.Range(1, 7);
            s.condition = Student.ClampCondition(s.condition + recover);
            logs.Add(CreateSystemLog($"[{context.MySchoolName}] {s.studentName} 컨디션 +{recover}"));
        }

        return new QuarterSimulationResult(expectedMyScore - context.MySchoolScore, expectedOpponentScore - context.OpponentScore, logs);
    }

    // 쿼터 시작 시점 스코어 로그를 생성 (1쿼터 0:0은 흐름 문구 생략)
    private static string BuildQuarterStartScoreLog(MatchContext context, int quarter)
    {
        int myScore = context.MySchoolScore;
        int opponentScore = context.OpponentScore;

        // 1쿼터 시작 직후 0:0은 접전 문구 생략
        if (quarter == 1 && myScore == 0 && opponentScore == 0)
            return $"{context.MySchoolName} {myScore} - {opponentScore} {context.OpponentTeamName}";

        return $"{context.MySchoolName} {myScore} - {opponentScore} {context.OpponentTeamName} ({BuildFlowText(myScore - opponentScore)})";
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

    private static string ApplyAnnouncementBold(string message)
    {
        return $"<b>{message}</b>";
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
