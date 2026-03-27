using System;

// 토너먼트 결과 데이터 (씬 간 전달용)
[Serializable]
public struct TournamentData
{
    public int PendingMySchoolReachedRoundTeamCount; // 1~4위 또는 탈락 라운드 팀 수(8,16,32) 담음

    // 초기값
    public static TournamentData Default => new()
    {
        PendingMySchoolReachedRoundTeamCount = 0
    };

    public bool HasPendingResult => PendingMySchoolReachedRoundTeamCount > 0;

    // 데이터 초기화
    public void Clear()
    {
        PendingMySchoolReachedRoundTeamCount = 0;
    }

    // 토너먼트 결과 설정 (Tournament 씬에서 호출)
    public void SetResult(int mySchoolReachedRoundTeamCount)
    {
        PendingMySchoolReachedRoundTeamCount = mySchoolReachedRoundTeamCount;
    }

    // 토너먼트 결과 전체 소비 (Lobby 씬에서 한 번만 읽고 버림)
    public bool TryConsumeResult(out TournamentData resultData)
    {
        resultData = this;

        if (!resultData.HasPendingResult)
        {
            resultData = Default;
            return false;
        }

        Clear();
        return true;
    }

    // 표시용 순위 텍스트 계산 (1,2위는 명확 / 그 외는 몇 강 기준)
    public static string BuildPlacementText(int reachedRoundTeamCount)
    {
        if (reachedRoundTeamCount == 1)
            return "1위";

        if (reachedRoundTeamCount == 2)
            return "2위";

        if (reachedRoundTeamCount == 3)
            return "3위";

        if (reachedRoundTeamCount == 4)
            return "4위";

        if (reachedRoundTeamCount <= 0)
            return "결과 없음";

        return $"{reachedRoundTeamCount}강";
    }
}
