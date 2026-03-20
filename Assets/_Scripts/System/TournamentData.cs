using System;

// 토너먼트 결과 데이터 (씬 간 전달용)
[Serializable]
public struct TournamentData
{
    public int PendingMySchoolReachedRoundTeamCount; // 내가 진입한 라운드 팀 수 (32,16,8,4,2,1)

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

        if (reachedRoundTeamCount <= 0)
            return "결과 없음";

            // 나중에 3,4위전도 추가되나? 기획서상으론 < 4 일때만 성공, 나머지 탈락

        return $"{reachedRoundTeamCount}강";
    }
}
