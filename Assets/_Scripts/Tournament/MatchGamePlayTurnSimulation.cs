using System.Collections.Generic;
using UnityEngine;

// 공방 1개 책임: 참여자 선택, 선공/공수 판정, 점수 반영, 컨디션 리스크 처리
public readonly struct PlayTurnSimulationResult
{
    public readonly IReadOnlyList<QuarterLogEntry> logs;

    // 공방 1회의 로그 묶음을 결과로 저장
    public PlayTurnSimulationResult(IReadOnlyList<QuarterLogEntry> logs)
    {
        this.logs = logs;
    }
}

public sealed class RandomPlayTurnSimulator
{
    // 공방 1회를 시뮬레이션하고 점수/로그를 반영
    public PlayTurnSimulationResult SimulatePlayTurn(MatchContext context, QuarterPodSession session)
    {
        List<QuarterLogEntry> logs = new(10);
        int playTurn = session.PlayTurnCount + 1;

        logs.Add(CreateNormalLog($"공방 {playTurn}: 참여자 선정 (Stub) - 학생 데이터 연동 TODO"));

        PrePlayTurnEvents(session, logs);

        bool myOffense = ResolveOffenseFirst(context, logs);

        // TODO(PlayTurn): 공방(공격/수비) 단위 판정은 여기에서 호출 (Step 3에서 IPlayTurnResolver로 대체)
        bool myWin = ResolvePlayTurnWinner(myOffense, logs);
        if (myWin)
            session.AddMyScore(session.ScorePerPlayTurnWin);
        else
            session.AddOpponentScore(session.ScorePerPlayTurnWin);

        logs.Add(CreateNormalLog(
            myWin
                ? $"공방 {playTurn} 결과: 우리 팀 우세, +{session.ScorePerPlayTurnWin}점"
                : $"공방 {playTurn} 결과: 상대 팀 우세, +{session.ScorePerPlayTurnWin}점"));

        PostPlayTurnEvents(session, logs);
        ApplyConditionRiskStub(context, session, logs);

        return new PlayTurnSimulationResult(logs);
    }

    // 공방 시작 전에 적용할 이벤트 훅 처리
    private static void PrePlayTurnEvents(QuarterPodSession session, List<QuarterLogEntry> logs)
    {
        // TODO(Event): 동일 이벤트 효과는 중첩하지 않고 가장 높은 조건의 이벤트 1개만 적용
        logs.Add(CreateNormalLog($"공방 전 이벤트 판정: {session.Quarter}쿼터 이벤트 훅 처리 (Stub)"));
    }

    // 공방 종료 직후 적용할 이벤트 훅 처리
    private static void PostPlayTurnEvents(QuarterPodSession session, List<QuarterLogEntry> logs)
    {
        // TODO(Event): 동일 이벤트 효과는 중첩하지 않고 가장 높은 조건의 이벤트 1개만 적용
        logs.Add(CreateNormalLog($"공방 후 이벤트 판정: {session.Quarter}쿼터 후처리 스텁 적용"));
    }

    // 속도 차이 기반으로 선공 팀 판정
    private static bool ResolveOffenseFirst(MatchContext context, List<QuarterLogEntry> logs)
    {
        // TODO(StudentData): 학생 speed 기반 선공 판정으로 교체
        bool myOffense = Random.value < 0.5f;

        logs.Add(CreateNormalLog(
            myOffense
                ? $"선공 판정: 우리 {context.MySchoolName} 선공 (Stub 랜덤)"
                : $"선공 판정: 상대 {context.OpponentTeamName} 선공 (Stub 랜덤)"));
        return myOffense;
    }

    // 공격/수비 스택 기반 공방 흐름 판정
    private static bool ResolvePlayTurnWinner(bool myOffense, List<QuarterLogEntry> logs)
    {
        // TODO(StudentData): 학생 shooting/jump/mental/condition 기반 공방 판정으로 교체
        bool offenseSuccess = Random.value < 0.5f;
        bool myWin = myOffense ? offenseSuccess : !offenseSuccess;

        logs.Add(CreateNormalLog(
            offenseSuccess
                ? "공방 판정: 공격 성공, 득점 발생 (Stub 랜덤)"
                : "공방 판정: 수비 성공, 득점 저지 (Stub 랜덤)"));
        logs.Add(CreateNormalLog(
            myWin
                ? "공방 나레이션: 우리 팀이 흐름을 가져왔습니다."
                : "공방 나레이션: 상대 팀이 흐름을 가져왔습니다."));

        return myWin;
    }

    // 공방 참여자의 컨디션 리스크를 반영하고 필요 시 교체
    private static void ApplyConditionRiskStub(MatchContext context, QuarterPodSession session, List<QuarterLogEntry> logs)
    {
        // TODO(StudentData): 컨디션 감소/0 교체 처리 구현 필요
        logs.Add(CreateSystemLog($"{context.MySchoolName} / {context.OpponentTeamName} 컨디션 리스크 판정 (Stub)"));
    }

    // 일반 로그 엔트리를 생성
    private static QuarterLogEntry CreateNormalLog(string message)
    {
        return new QuarterLogEntry(message, isSystem: false);
    }

    // 시스템 로그 엔트리를 생성
    private static QuarterLogEntry CreateSystemLog(string message)
    {
        return new QuarterLogEntry(message, isSystem: true);
    }
}
