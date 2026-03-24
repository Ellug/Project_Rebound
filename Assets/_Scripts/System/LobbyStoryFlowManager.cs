using System;
using UnityEngine.SceneManagement;

// 로비 스토리(10002/10003) 트리거 흐름을 담당
public class LobbyStoryFlowManager
{
    private const string LobbyScene = "Lobby";
    private const int PreWinterStoryId = 10002;
    private const int WinterChampionStoryId = 10003;
    private const int PreWinterStoryOffsetMonths = 2;

    private readonly GameManager _gameManager;
    private TurnManager _turnManager;
    private DateTime _firstWinterStartDate;
    private DateTime _firstWinterPreStoryDate;
    private bool _hasFirstWinterSchedule;

    public LobbyStoryFlowManager(GameManager gameManager)
    {
        _gameManager = gameManager;
    }

    // 로비 컨텍스트(TurnManager)를 바인딩
    public void BindLobbyContext(TurnManager turnManager)
    {
        _turnManager = turnManager;
    }

    // 씬 이탈 시 로비 컨텍스트를 정리
    public void ClearLobbyContext()
    {
        _turnManager = null;
        _firstWinterStartDate = default;
        _firstWinterPreStoryDate = default;
        _hasFirstWinterSchedule = false;
    }

    // 첫 겨울방학 시작/종료일과 10002 트리거 날짜를 테이블 기준으로 캐싱
    public void CacheFirstWinterSchedule()
    {
        if (!TryGetFirstWinterDates(out DateTime firstWinterStart, out DateTime firstWinterEnd))
        {
            _hasFirstWinterSchedule = false;
            _firstWinterStartDate = default;
            _firstWinterPreStoryDate = default;
            return;
        }

        _hasFirstWinterSchedule = true;
        _firstWinterStartDate = firstWinterStart;
        _firstWinterPreStoryDate = _firstWinterStartDate.AddMonths(-PreWinterStoryOffsetMonths).Date;
    }

    // 첫 겨울방학 2개월 전 날짜에 10002를 1회 실행
    public bool TryTriggerPreWinterStory()
    {
        if (_turnManager == null) return false;
        if (_gameManager.HasPlayedVn10002) return false;

        if (!_hasFirstWinterSchedule)
            CacheFirstWinterSchedule();

        if (!_hasFirstWinterSchedule)
            return false;

        DateTime today = _turnManager.DateManager.CurrentDate.Date;
        if (today < _firstWinterPreStoryDate || today >= _firstWinterStartDate)
            return false;

        VNBridge.RequestStory(PreWinterStoryId, LobbyScene);
        SceneTransitionManager.Instance.LoadScene(VNBridge.VNSceneName);
        return true;
    }

    // 첫 겨울방학 우승 VN(10003) 진입 조건을 확인하고 씬 전환
    public bool TryEnterFirstWinterChampionStory()
    {
        if (_gameManager.HasPlayedVn10003)
            return false;

        if (!TryGetFirstWinterDates(out DateTime firstWinterStart, out DateTime firstWinterEnd))
            return false;

        DateTime today = _gameManager.CurrentDate.Date;
        if (today < firstWinterStart || today > firstWinterEnd)
            return false;

        VNBridge.RequestStory(WinterChampionStoryId, LobbyScene);
        SceneTransitionManager.Instance.LoadScene(VNBridge.VNSceneName);
        return true;
    }

    // AlwaysEventTable에서 첫 겨울방학 termStart/termEnd를 조회한다.
    private static bool TryGetFirstWinterDates(out DateTime termStartDate, out DateTime termEndDate)
    {
        return AlwaysEventDateUtil.TryGetFirstWinterVacationTerm(out termStartDate, out termEndDate);
    }
}
