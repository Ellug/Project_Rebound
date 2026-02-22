// 경기 진행 단계 레이블 정의 (쿼터 > 하프타임 > 종료 순서)
public static class MatchGameStages
{
    // UI 진행 표시바 및 MatchGameManager.ProgressMatchStep()의 switch 분기와 인덱스가 1:1 매핑됨
    public static readonly string[] Default =
    {
        "1쿼터",
        "하프타임",
        "2쿼터",
        "하프타임",
        "3쿼터",
        "하프타임",
        "경기 종료"
    };

    // 쿼터 종료 후 이동할 하프타임 스테이지 인덱스 반환
    public static int GetHalfTimeStageIndex(int quarter)
    {
        return quarter switch
        {
            1 => 1,
            2 => 3,
            _ => 5
        };
    }
}
