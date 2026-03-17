public static class VNBridge
{
    public const string VNSceneName = "VN";
    public const string DefaultReturnSceneName = "Lobby";

    private static int _pendingStoryId = -1;
    private static string _pendingReturnScene = DefaultReturnSceneName;

    // 대기 중인 VN 요청이 있는지 확인
    public static bool HasPendingRequest => _pendingStoryId > 0;

    // VN 진입 전에 스토리 ID와 복귀 씬을 설정
    public static void RequestStory(int storyId, string returnSceneName = DefaultReturnSceneName)
    {
        _pendingStoryId = storyId;
        _pendingReturnScene = string.IsNullOrWhiteSpace(returnSceneName)
            ? DefaultReturnSceneName
            : returnSceneName.Trim();
    }

    // VN 씬에서 요청을 1회 소비하고 자동으로 초기화
    public static bool TryConsumeRequest(out int storyId, out string returnSceneName)
    {
        if (!HasPendingRequest)
        {
            storyId = -1;
            returnSceneName = DefaultReturnSceneName;
            return false;
        }

        storyId = _pendingStoryId;
        returnSceneName = _pendingReturnScene;

        _pendingStoryId = -1;
        _pendingReturnScene = DefaultReturnSceneName;
        return true;
    }

    // 대기 요청을 강제로 초기화
    public static void Clear()
    {
        _pendingStoryId = -1;
        _pendingReturnScene = DefaultReturnSceneName;
    }
}
