using System.Collections.Generic;
using UnityEngine;

// 공방 1개 책임: 참여자 선택, 선공/공수 판정, 점수 반영, 컨디션 처리
public readonly struct PlayTurnSimulationResult
{
    public readonly IReadOnlyList<QuarterLogEntry> logs;
    public readonly MatchContextVisualCue contextVisualCue; // 공방 결과 상황 연출 타입

    public PlayTurnSimulationResult(IReadOnlyList<QuarterLogEntry> logs, MatchContextVisualCue contextVisualCue)
    {
        this.logs = logs;
        this.contextVisualCue = contextVisualCue;
    }
}

public sealed class RandomPlayTurnSimulator
{
    // 공방 1회를 시뮬레이션하고 점수/로그를 반영
    public PlayTurnSimulationResult SimulatePlayTurn(MatchContext context, QuarterPodSession session)
    {
        List<QuarterLogEntry> logs = new(10);
        MatchContextVisualCue contextVisualCue = MatchContextVisualCue.PlayTurnDefault;

        Student myPlayer = PickRandom(context.FieldPlayers);

        if (myPlayer == null)
        {
            logs.Add(Normal($"[{context.MySchoolName}] 출전 선수가 없어 공방을 진행할 수 없었다."));
            return new PlayTurnSimulationResult(logs, contextVisualCue);
        }

        // 3-3 나레이션: 선수 출전
        logs.Add(Normal($"[{context.MySchoolName}] {myPlayer.studentName}이(가) 코트에 나섰다."));

        PrePlayTurnEvents();

        // 선공 판정
        bool myOffense = ResolveOffenseFirst(
            myPlayer,
            context.OpponentStat,
            context.MySchoolName,
            context.OpponentTeamName,
            logs);

        // 선공 이미지
        contextVisualCue = myOffense
            ? MatchContextVisualCue.PlayTurnAttackFirst
            : MatchContextVisualCue.PlayTurnAttackSecond;

        // 공방 판정 및 득점 처리
        MatchContextVisualCue resolvedVisualCue = ResolvePlayTurn(
            myPlayer,
            context.OpponentStat,
            myOffense,
            session,
            logs,
            context.MySchoolName,
            context.OpponentTeamName);

        if (resolvedVisualCue != MatchContextVisualCue.None)
            contextVisualCue = resolvedVisualCue;

        PostPlayTurnEvents();

        // 3-1 스탯 가감: [SYSTEM] 소속 이름 스탯 가감치
        int conditionLoss = Random.Range(1, 6);
        myPlayer.condition = Student.ClampCondition(myPlayer.condition - conditionLoss);
        logs.Add(System($"[{context.MySchoolName}] {myPlayer.studentName} 컨디션 -{conditionLoss}"));
        logs.Add(Normal(string.Empty));

        return new PlayTurnSimulationResult(logs, contextVisualCue);
    }

    private static void PrePlayTurnEvents()
    {
        // TODO(Event): 이벤트 효과 적용
    }

    private static void PostPlayTurnEvents()
    {
        // TODO(Event): 이벤트 후처리 적용
    }

    // 3-3 나레이션으로 선공 판정 출력
    private static bool ResolveOffenseFirst(
        Student myPlayer,
        EnemyStatRow enemy,
        string myTeam,
        string opponentTeam,
        List<QuarterLogEntry> logs)
    {
        bool myOffense = myPlayer.speed >= enemy.speed;

        logs.Add(Normal(myOffense
            ? $"[{myTeam}] {myPlayer.studentName}이(가) 빠르게 치고 나갔다."
            : $"[{opponentTeam}] 상대가 먼저 움직임을 잡았다."));

        return myOffense;
    }

    // 공격/수비/리바운드 판정 후 득점 반영, 결과 연출 타입 반환
    private static MatchContextVisualCue ResolvePlayTurn(
        Student myPlayer,
        EnemyStatRow enemy,
        bool myOffense,
        QuarterPodSession session,
        List<QuarterLogEntry> logs,
        string myTeam,
        string opponentTeam)
    {
        int attackStat = myOffense ? myPlayer.shoot : enemy.shoot;
        int defenseStat = myOffense ? enemy.jump : myPlayer.jump;
        string attackerTag = myOffense ? $"[{myTeam}]" : $"[{opponentTeam}]";
        string attackerName = myOffense ? myPlayer.studentName : "상대";

        // 3-3 나레이션: 슛 시도
        bool attackSuccess = (attackStat - defenseStat) > Random.Range(1, 101);
        logs.Add(Normal(attackSuccess
            ? $"{attackerTag} {attackerName}의 슛이 터졌다."
            : $"{attackerTag} {attackerName}의 슛이 막혔다."));

        bool myWin;

        if (attackSuccess)
        {
            myWin = myOffense;

            float threePointFieldGoal;

            if (myOffense)
            {
                threePointFieldGoal = 30f;

                switch (myPlayer.positionId)
                {
                    case "guard":
                        threePointFieldGoal += (myPlayer.shoot * 3 + myPlayer.speed * 2 + myPlayer.jump) / 40f;
                        break;

                    case "forward":
                        threePointFieldGoal += (myPlayer.shoot + myPlayer.speed * 3 + myPlayer.jump * 2) / 40f;
                        break;

                    case "center":
                        threePointFieldGoal += (myPlayer.shoot * 2 + myPlayer.speed + myPlayer.jump * 3) / 40f;
                        break;
                }
            }
            else
            {
                threePointFieldGoal = 35f;
            }


                // 0~100 사이의 값을 랜덤 뽑아서 그 값보다 3점슛 확률이 높으면 3점슛
            bool isThree = Random.Range(0f, 100f) < threePointFieldGoal;
            int score = isThree ? 3 : 2;

            if (myWin)
                session.AddMyScore(score);
            else
                session.AddOpponentScore(score);

            logs.Add(Normal(myWin
                ? $"[{myTeam}] {(isThree ? "3점" : "2점")} 득점에 성공했다."
                : $"[{opponentTeam}] 상대가 {(isThree ? "3점" : "2점")} 득점했다."));
        }
        else
        {
            // 리바운드 판정
            int reboundStat = myOffense ? myPlayer.condition : enemy.condition;
            bool reboundSuccess = reboundStat > Random.Range(1, 101);

            logs.Add(Normal(reboundSuccess
                ? $"{attackerTag} {attackerName}이(가) 리바운드를 잡아냈다."
                : $"{attackerTag} {attackerName}이(가) 리바운드를 놓쳤다."));

            myWin = myOffense == reboundSuccess;

            float threePointFieldGoal = 30f;

            if (myOffense)
            {
                threePointFieldGoal = 30f;

                // 리바운드 성공시 10% 추가
                if (reboundSuccess)
                    threePointFieldGoal += 10f;

                switch (myPlayer.positionId)
                {
                    case "guard":
                        threePointFieldGoal += (myPlayer.shoot * 3 + myPlayer.speed * 2 + myPlayer.jump) / 40f;
                        break;

                    case "forward":
                        threePointFieldGoal += (myPlayer.shoot + myPlayer.speed * 3 + myPlayer.jump * 2) / 40f;
                        break;

                    case "center":
                        threePointFieldGoal += (myPlayer.shoot * 2 + myPlayer.speed + myPlayer.jump * 3) / 40f;
                        break;
                }
            }

            else
            {
                threePointFieldGoal = 35f;
            }

            // 0~100 사이의 값을 랜덤 뽑아서 그 값보다 3점슛 확률이 높으면 3점슛
            bool isThree = Random.Range(0f, 100f) < threePointFieldGoal;
            int score = isThree ? 3 : 2;

            if (myWin)
                session.AddMyScore(score);
            else
                session.AddOpponentScore(score);

            logs.Add(Normal(myWin
                ? $"[{myTeam}] {(isThree ? "3점" : "2점")} 득점에 성공했다."
                : $"[{opponentTeam}] 상대가 {(isThree ? "3점" : "2점")} 득점했다."));
        }

        // 최종 공방 결과 기준 이미지 결정
        if (myOffense)
            return myWin ? MatchContextVisualCue.PlayTurnAttackSuccess : MatchContextVisualCue.PlayTurnAttackFailed;

        return myWin ? MatchContextVisualCue.PlayTurnDefenseSuccess : MatchContextVisualCue.PlayTurnDefenseFailed;
    }

    private static Student PickRandom(List<Student> players)
    {
        if (players == null || players.Count == 0)
            return null;

        return players[Random.Range(0, players.Count)];
    }

    private static QuarterLogEntry Normal(string message)
    {
        return new QuarterLogEntry(message, isSystem: false);
    }

    private static QuarterLogEntry System(string message)
    {
        return new QuarterLogEntry(message, isSystem: true);
    }
}
