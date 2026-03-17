// Tournament 씬 진입 시 실행 모드를 전달
public enum TournamentSceneMode
{
    Tournament,
    FriendlyMatch
}

// 씬 전환 사이에서 토너먼트/친선전 요청 데이터 1회 전달
public static class TournamentSceneBridge
{
    private static bool _hasPendingRequest;
    private static TournamentSceneMode _pendingMode = TournamentSceneMode.Tournament;
    private static string _pendingOpponentName = string.Empty;

    // 기본 토너먼트 모드 요청 등록
    public static void RequestTournament()
    {
        _hasPendingRequest = true;
        _pendingMode = TournamentSceneMode.Tournament;
        _pendingOpponentName = string.Empty;
    }

    // 친선전 모드 요청과 상대 학교명 등록
    public static void RequestFriendlyMatch(string opponentName)
    {
        _hasPendingRequest = true;
        _pendingMode = TournamentSceneMode.FriendlyMatch;
        _pendingOpponentName = (opponentName ?? string.Empty).Trim();
    }

    // 등록된 요청을 1회 소비 후 내부 상태 초기화
    public static bool TryConsumeRequest(out TournamentSceneMode mode, out string opponentName)
    {
        if (!_hasPendingRequest)
        {
            mode = TournamentSceneMode.Tournament;
            opponentName = string.Empty;
            return false;
        }

        mode = _pendingMode;
        opponentName = _pendingOpponentName;

        _hasPendingRequest = false;
        _pendingMode = TournamentSceneMode.Tournament;
        _pendingOpponentName = string.Empty;
        return true;
    }

    // 대기 중인 요청 클리어
    public static void Clear()
    {
        _hasPendingRequest = false;
        _pendingMode = TournamentSceneMode.Tournament;
        _pendingOpponentName = string.Empty;
    }
}
