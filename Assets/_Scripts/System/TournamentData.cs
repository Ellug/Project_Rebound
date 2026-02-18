using System;

// 토너먼트 결과 데이터 (씬 간 전달용)
[Serializable]
public struct TournamentData
{
    public string PendingChampion; // 로비로 전달할 우승팀 이름

    // 초기값
    public static TournamentData Default => new()
    {
        PendingChampion = null
    };

    // 데이터 초기화
    public void Clear()
    {
        PendingChampion = null;
    }

    // 우승팀 설정 (Tournament 씬에서 호출)
    public void SetChampion(string champion)
    {
        PendingChampion = champion;
    }

    // 우승팀 소비 (Lobby 씬에서 한 번만 읽고 버림)
    public bool TryConsumeChampion(out string champion)
    {
        champion = PendingChampion;

        if (string.IsNullOrWhiteSpace(champion))
            return false;

        PendingChampion = null;
        return true;
    }
}
