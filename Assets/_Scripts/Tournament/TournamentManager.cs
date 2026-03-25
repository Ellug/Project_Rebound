using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

// 토너먼트 전체 흐름을 관리: 대진 생성 → 라운드 진행 → 결과 판정 → 로비 복귀
public class TournamentManager : MonoBehaviour
{
    private const string LobbyScene = "Lobby";
    private const string DefaultFriendlyOpponentName = "친선고등학교";

    [Header("References")]
    [SerializeField] private TournamentUI _tournamentUi;
    [SerializeField] private MatchGameUI _matchGameUi;
    [SerializeField] private MatchGameManager _matchGameManager;

    [Header("Tournament")]
    [SerializeField] private int _teamCount = 32;
    [SerializeField] private string _mySchoolName = "한울고등학교";

    // 라운드별 매치업 전체: _allRounds[0]=32강, [1]=16강 ... [n]=결승
    private readonly List<List<Matchup>> _allRounds = new();
    private int _currentRoundIndex;
    private bool _isWaitingForResultNext;            // 결과 패널 표시 중 다음 버튼 중복 클릭 방지
    private int _mySchoolReachedRoundTeamCount;      // 토너먼트 종료 시 로비에 전달할 성적 (팀 수 기준)
    private bool _mySchoolDefeatedThisMatch;         // 직전 우리 학교 경기에서 탈락했는지 여부
    private bool _isFriendlyMatchMode;               // 친선전 모드 여부
    private string _friendlyOpponentName;            // 친선전 상대 학교명

    // 매치업 데이터
    private class Matchup
    {
        public string UpTeam;
        public string DownTeam;
        public string Winner; // null이면 아직 진행 안함
        public bool IncludeMySchool;
    }

    void Start()
    {
        _matchGameManager.OnMatchFinished += HandleMySchoolMatchFinished;

        if (TournamentSceneBridge.TryConsumeRequest(out TournamentSceneMode sceneMode, out string opponentName)
            && sceneMode == TournamentSceneMode.FriendlyMatch)
        {
            StartFriendlyMatch(opponentName);
            return;
        }

        // 이어하기 복원: SaveManager에 토너먼트 진행 데이터가 있으면 복원 후 리턴
        if (SaveManager.Instance != null)
        {
            SavedTournamentData savedTournament = SaveManager.Instance.CurrentData?.tournament;
            if (savedTournament != null && savedTournament.isInProgress)
            {
                RestoreSaveData(savedTournament);

                SavedMatchSimData savedMatchSim = SaveManager.Instance.CurrentData?.matchSim;
                if (savedMatchSim != null && savedMatchSim.isMatchRunning)
                    _matchGameManager.RestoreSaveData(savedMatchSim);

                return;
            }
        }
        GenerateTournament();
    }

    private void OnDestroy()
    {
        _matchGameManager.OnMatchFinished -= HandleMySchoolMatchFinished;
    }

    // 토너먼트 대진표 생성
    public void GenerateTournament()
    {
        _mySchoolName = (_mySchoolName ?? string.Empty).Trim();
        _isFriendlyMatchMode = false;
        _friendlyOpponentName = string.Empty;

        // 학교 목록 생성 및 셔플
        List<string> schools = BuildSchoolList();
        ShuffleKeepingMySchoolFirst(schools);

        // 토너먼트 초기화 - 32강부터 결승까지 구조 생성
        _allRounds.Clear();
        _currentRoundIndex = 0;
        _isWaitingForResultNext = false;
        _mySchoolReachedRoundTeamCount = _teamCount;
        _mySchoolDefeatedThisMatch = false;

        // 첫 라운드 매치업 생성 (32강)
        List<Matchup> firstRound = new();
        int matchupCount = _teamCount / 2;
        
        for (int i = 0; i < matchupCount; i++)
            firstRound.Add(CreateMatchup(schools[i * 2], schools[i * 2 + 1]));

        _allRounds.Add(firstRound);

        // 다음 라운드들 빈 구조만 생성 (16강, 8강, 4강, 결승)
        int nextRoundCount = matchupCount / 2;
        while (nextRoundCount > 0)
        {
            _allRounds.Add(new List<Matchup>(nextRoundCount));
            nextRoundCount /= 2;
        }

        _matchGameUi.HideMatchGamePanel();
        _matchGameUi.HideMatchResultPanel();
        _matchGameManager.AbortCurrentMatch();

        RefreshUI();
    }

    // 친선전 모드 초기화 및 단일 경기 시작
    private void StartFriendlyMatch(string opponentName)
    {
        _isFriendlyMatchMode = true;
        _mySchoolName = (_mySchoolName ?? string.Empty).Trim();
        _friendlyOpponentName = string.IsNullOrWhiteSpace(opponentName)
            ? DefaultFriendlyOpponentName
            : opponentName.Trim();

        _allRounds.Clear();
        _currentRoundIndex = 0;
        _isWaitingForResultNext = false;
        _mySchoolDefeatedThisMatch = false;
        _mySchoolReachedRoundTeamCount = 0;

        // 친선전에서는 브래킷 패널을 숨기고 경기 UI만 사용한다.
        _tournamentUi.HideTournamentPanels();
        _matchGameUi.HideMatchResultPanel();
        _matchGameManager.AbortCurrentMatch();
        // 우리 학교 vs 상대 학교 단일 매치를 바로 시작한다.
        _matchGameManager.StartMatch(_mySchoolName, _friendlyOpponentName, _mySchoolName);
    }

    // 매치 승자 처리 (외부에서 호출 가능)
    public void SetMatchWinner(int matchIndex, string winnerTeamName, bool advanceWhenRoundComplete = true)
    {
        List<Matchup> currentRound = _allRounds[_currentRoundIndex];
        currentRound[matchIndex].Winner = winnerTeamName;

        // 현재 라운드의 모든 매치가 끝났는지 확인
        if (advanceWhenRoundComplete && IsCurrentRoundComplete())
            AdvanceToNextRound();
    }

    // 현재 라운드 완료 여부 확인
    private bool IsCurrentRoundComplete()
    {
        List<Matchup> currentRound = _allRounds[_currentRoundIndex];

        foreach (Matchup matchup in currentRound)
            if (matchup.Winner == null) return false;

        return true;
    }

    // 다음 라운드로 진행
    private void AdvanceToNextRound()
    {
        // 마지막 라운드(결승)면 토너먼트 종료
        if (_currentRoundIndex >= _allRounds.Count - 1)
        {
            OnTournamentComplete();
            return;
        }

        // 현재 라운드 승자들 수집
        List<Matchup> currentRound = _allRounds[_currentRoundIndex];
        List<string> winners = new();

        foreach (Matchup matchup in currentRound)
            winners.Add(matchup.Winner);

        // 다음 라운드 매치업 생성
        _currentRoundIndex++;
        List<Matchup> nextRound = _allRounds[_currentRoundIndex];
        nextRound.Clear();

        int nextMatchupCount = winners.Count / 2;
        for (int i = 0; i < nextMatchupCount; i++)
            nextRound.Add(CreateMatchup(winners[i * 2], winners[i * 2 + 1]));

        RefreshUI();
    }

    // 토너먼트 종료 처리
    private void OnTournamentComplete()
    {
        // 우승 결과 캐싱 후 로비로 복귀
        GameManager.Instance.SetPendingTournamentResult(_mySchoolReachedRoundTeamCount);

        if (_mySchoolReachedRoundTeamCount == 1)
        {
            if (GameManager.Instance.TryEnterFirstWinterChampionStory())
                return;
        }

        SceneTransitionManager.Instance.LoadScene(LobbyScene);
    }

    private void RefreshUI()
    {
        int maxRoundIndex = Mathf.Clamp(_currentRoundIndex, 0, Mathf.Max(0, _allRounds.Count - 1));
        List<List<TournamentMatchViewData>> allRoundViewData = new(maxRoundIndex + 1);

        for (int roundIndex = 0; roundIndex <= maxRoundIndex; roundIndex++)
        {
            List<Matchup> round = _allRounds[roundIndex];
            List<TournamentMatchViewData> roundViewData = new(round.Count);

            for (int matchupIndex = 0; matchupIndex < round.Count; matchupIndex++)
            {
                Matchup matchup = round[matchupIndex];
                bool isResolved = !string.IsNullOrEmpty(matchup.Winner);
                bool isUpTeamWinner = isResolved && string.Equals(matchup.Winner, matchup.UpTeam, StringComparison.Ordinal);

                roundViewData.Add(new TournamentMatchViewData(
                    FormatSchoolName(matchup.UpTeam),
                    FormatSchoolName(matchup.DownTeam),
                    matchup.IncludeMySchool,
                    isResolved,
                    isUpTeamWinner));
            }

            allRoundViewData.Add(roundViewData);
        }

        _tournamentUi.RenderRounds(allRoundViewData, maxRoundIndex, FormatSchoolName(_mySchoolName));
    }

    // CachedSOData 에서 참조해 학교 리스트 출력
    private List<string> BuildSchoolList()
    {
        var schoolTable = CachedSOData.Get<SchoolNameTableSO>();
        List<string> schools = new(_teamCount);
        var usedNames = new HashSet<string>(System.StringComparer.Ordinal);

        string mySchoolName = (_mySchoolName ?? "").Trim();
        if (!string.IsNullOrEmpty(mySchoolName) && usedNames.Add(mySchoolName))
            schools.Add(mySchoolName);

        var rows = schoolTable.Rows;
        for (int i = 0; i < rows.Count && schools.Count < _teamCount; i++)
        {
            var row = rows[i];
            if (row == null) continue;

            string schoolName = (row.name ?? "").Trim();
            if (string.IsNullOrEmpty(schoolName)) continue;
            if (!usedNames.Add(schoolName)) continue;

            schools.Add(schoolName);
        }

        return schools;
    }

    // "한울고등학교" → "한울 고등학교" 형태로 변환 (비교용 원본은 건드리지 않음)
    public static string FormatSchoolName(string name)
    {
        if (string.IsNullOrEmpty(name))
            return name;

        const string suffix = "고등학교";
        int idx = name.IndexOf(suffix, StringComparison.Ordinal);
        if (idx > 0 && name[idx - 1] != ' ')
            return name[..idx] + " " + name[idx..];

        return name;
    }

    private void ShuffleKeepingMySchoolFirst(List<string> list)
    {
        if (list == null || list.Count <= 1) return;

        int shuffleStartIndex = 0;
        int mySchoolIndex = list.FindIndex(IsMySchool);
        if (mySchoolIndex >= 0)
        {
            if (mySchoolIndex != 0)
                (list[0], list[mySchoolIndex]) = (list[mySchoolIndex], list[0]);

            shuffleStartIndex = 1;
        }

        for (int i = list.Count - 1; i > shuffleStartIndex; i--)
        {
            int randomIndex = UnityEngine.Random.Range(shuffleStartIndex, i + 1);
            (list[randomIndex], list[i]) = (list[i], list[randomIndex]);
        }
    }

    private Matchup CreateMatchup(string firstTeam, string secondTeam, string winner = null)
    {
        return new Matchup
        {
            UpTeam = firstTeam,
            DownTeam = secondTeam,
            Winner = winner,
            IncludeMySchool = IsMySchool(firstTeam) || IsMySchool(secondTeam)
        };
    }

    private bool IsMySchool(string teamName)
    {
        return string.Equals(teamName, _mySchoolName, StringComparison.Ordinal);
    }

    // 확인 버튼에 인스펙터에서 연결. 현재 라운드 진행 (내 학교 제외)
    public void ProgressCurrentRound()
    {
        // 친선전 모드에서는 라운드 진행 버튼 무시
        if (_isFriendlyMatchMode) return;

        List<Matchup> currentRound = _allRounds[_currentRoundIndex];
        for (int i = 0; i < currentRound.Count; i++)
        {
            Matchup matchup = currentRound[i];
            if (matchup.Winner == null && !matchup.IncludeMySchool)
            {
                // 내 학교 아닌 매치만 랜덤으로 승자 선택
                string winner = UnityEngine.Random.value > 0.5f ? matchup.UpTeam : matchup.DownTeam;
                SetMatchWinner(i, winner);
            }
        }

        // 내 학교 매치가 있으면 매치 시뮬레이션 시작
        if (TryGetPendingMySchoolMatch(out Matchup mySchoolMatchup))
            _matchGameManager.StartMatch(mySchoolMatchup.UpTeam, mySchoolMatchup.DownTeam, _mySchoolName);
        else
            Debug.Log("[TournamentManager] 내 학교 매치가 없어서 자동 진행");
    }

    // 경기 진행 버튼에 연결. 쿼터/공방/하프타임을 한 버튼으로 순차 진행
    public void OnClickProgressMySchoolMatch()
    {
        _matchGameManager.ProgressMatchStep();
    }

    private bool TryGetPendingMySchoolMatch(out Matchup mySchoolMatchup)
    {
        return TryGetPendingMySchoolMatch(out _, out mySchoolMatchup);
    }

    private bool TryGetPendingMySchoolMatch(out int matchIndex, out Matchup mySchoolMatchup)
    {
        List<Matchup> currentRound = _allRounds[_currentRoundIndex];

        for (int i = 0; i < currentRound.Count; i++)
        {
            Matchup matchup = currentRound[i];
            if (matchup.IncludeMySchool && matchup.Winner == null)
            {
                matchIndex = i;
                mySchoolMatchup = matchup;
                return true;
            }
        }

        matchIndex = -1;
        mySchoolMatchup = null;
        return false;
    }

    // 내 학교 승리 처리
    public void OnMySchoolWin()
    {
        // 친선전 모드에서는 대진 갱신 없이 결과 패널만 노출
        if (_isFriendlyMatchMode)
        {
            ShowFriendlyMatchResult(true);
            return;
        }

        ResolveMySchoolMatchAndShowResult(true);
    }

    // 내 학교 패배 처리
    public void OnMySchoolLose()
    {
        // 친선전 모드에서는 대진 갱신 없이 결과 패널만 노출
        if (_isFriendlyMatchMode)
        {
            ShowFriendlyMatchResult(false);
            return;
        }

        ResolveMySchoolMatchAndShowResult(false);
    }

    // 내 학교 매치 승자 처리
    private bool SetMySchoolMatchResult(bool didWin)
    {
        if (!TryGetPendingMySchoolMatch(out int matchIndex, out Matchup matchup))
            return false;

        string winner = didWin ? _mySchoolName : matchup.DownTeam;
        SetMatchWinner(matchIndex, winner, advanceWhenRoundComplete: false);
        return true;
    }

    // 결과 패널의 다음 버튼에 연결
    public void OnClickNextAfterMatchResult()
    {
        if (!_isWaitingForResultNext) return;

        _isWaitingForResultNext = false;

        // 친선전 결과 확인 후 즉시 로비로 복귀
        if (_isFriendlyMatchMode)
        {
            SceneTransitionManager.Instance.LoadScene(LobbyScene);
            return; 
        }

        // 우리 학교가 직전 경기에서 패배한 경우에만 토너먼트 종료
        if (_mySchoolDefeatedThisMatch)
        {
            GameManager.Instance.SetPendingTournamentResult(_mySchoolReachedRoundTeamCount);
            SceneTransitionManager.Instance.LoadScene(LobbyScene);
            return;
        }

        if (IsCurrentRoundComplete())
        {            
            _matchGameUi.HideMatchResultPanel();
            AdvanceToNextRound();            
        }
    }

    private void ResolveMySchoolMatchAndShowResult(bool didWin)
    {
        if (!SetMySchoolMatchResult(didWin))
        {
            Debug.LogWarning("[TournamentManager] 진행 중인 우리 학교 매치를 찾지 못했습니다.");
            return;
        }

        if (didWin) SoundManager.Instance.PlayBGM(108);

        _matchGameManager.AbortCurrentMatch();

        UpdateMySchoolTournamentProgress(didWin);
        _mySchoolDefeatedThisMatch = !didWin;

        _matchGameUi.HideMatchGamePanel();
        _matchGameUi.ShowMatchResultPanel(didWin);

        _isWaitingForResultNext = true;
        Debug.Log(didWin ? "[TournamentManager] 우리 학교 승리!" : "[TournamentManager] 우리 학교 패배...");
    }

    private void UpdateMySchoolTournamentProgress(bool didWin)
    {
        int currentRoundTeamCount = GetCurrentRoundTeamCount();
        if (currentRoundTeamCount <= 0)
            return;

        if (didWin)
        {
            if (currentRoundTeamCount <= 2)
                _mySchoolReachedRoundTeamCount = 1;
            else
                _mySchoolReachedRoundTeamCount = currentRoundTeamCount / 2;
        }
        else
        {
            _mySchoolReachedRoundTeamCount = currentRoundTeamCount;
        }
    }

    private int GetCurrentRoundTeamCount()
    {
        if (_currentRoundIndex < 0 || _currentRoundIndex >= _allRounds.Count)
            return 0;

        return _allRounds[_currentRoundIndex].Count * 2;
    }

    private void ShowFriendlyMatchResult(bool didWin)
    {
        _matchGameManager.AbortCurrentMatch();
        _mySchoolDefeatedThisMatch = !didWin;
        _isWaitingForResultNext = true;

        if (didWin) SoundManager.Instance.PlayBGM(108);

        GameManager.Instance.SetPendingFriendlyMatchResult(didWin, _friendlyOpponentName);

        // 친선전은 토너먼트 성적 대신 승패 화면만 표시한다.
        _matchGameUi.HideMatchGamePanel();
        _matchGameUi.ShowMatchResultPanel(didWin);
        Debug.Log(didWin ? "[TournamentManager] 친선전 승리!" : "[TournamentManager] 친선전 패배...");
    }

    private void HandleMySchoolMatchFinished(MatchResult matchResult)
    {
        if (matchResult == null)
            return;

        bool didWin = string.Equals(matchResult.winnerTeamName, _mySchoolName, StringComparison.Ordinal);
        Debug.Log($"[TournamentManager] MatchResult 수신 - Winner: {matchResult.winnerTeamName}, Score: {matchResult.finalScore.mySchoolScore}:{matchResult.finalScore.opponentScore}");

        // 친선전 모드에서는 승패만 표시하고 토너먼트 라운드를 건드리지 않음
        if (_isFriendlyMatchMode)
        {
            ShowFriendlyMatchResult(didWin);
            return;
        }

        if (didWin)     OnMySchoolWin();
        else            OnMySchoolLose();
    }

    // SaveManager.CollectTournamentData()에서 호출 — 현재 상태를 SavedTournamentData로 직렬화
    public SavedTournamentData CollectSaveData()
    {
        SavedTournamentData data = new()
        {
            // _isMatchRunning은 MatchGameManager 소유 — 대진표가 존재하면 진행 중으로 판단
            isInProgress = _allRounds.Count > 0,
            teamCount = _teamCount,
            currentRoundIndex = _currentRoundIndex,
            mySchoolReachedRoundTeamCount = _mySchoolReachedRoundTeamCount,
        };

        foreach (List<Matchup> round in _allRounds)
        {
            SavedRoundData roundData = new();

            foreach (Matchup matchup in round)
            {
                roundData.matchups.Add(new SavedMatchupData
                {
                    upTeam = matchup.UpTeam,
                    downTeam = matchup.DownTeam,
                    winner = matchup.Winner ?? string.Empty,
                    includeMySchool = matchup.IncludeMySchool,
                });
            }

            data.allRounds.Add(roundData);
        }
        return data;
    }

    // SaveManager.ApplyTournamentData()에서 호출 — SavedTournamentData를 런타임 상태로 복원
    public void RestoreSaveData(SavedTournamentData data)
    {
        if (data == null || !data.isInProgress)
            return;

        _isFriendlyMatchMode = false;
        _friendlyOpponentName = string.Empty;
        _mySchoolName = (_mySchoolName ?? string.Empty).Trim();
        _teamCount = data.teamCount > 0 ? data.teamCount : _teamCount;
        _currentRoundIndex = data.currentRoundIndex;
        _mySchoolReachedRoundTeamCount = data.mySchoolReachedRoundTeamCount;
        _isWaitingForResultNext = false;
        _mySchoolDefeatedThisMatch = false;

        _allRounds.Clear();

        foreach (SavedRoundData roundData in data.allRounds)
        {
            List<Matchup> round = new();

            foreach (SavedMatchupData matchupData in roundData.matchups)
            {
                round.Add(CreateMatchup(
                    matchupData.upTeam,
                    matchupData.downTeam,
                    string.IsNullOrEmpty(matchupData.winner) ? null : matchupData.winner));
            }

            _allRounds.Add(round);
        }

        _matchGameUi.HideMatchGamePanel();
        _matchGameUi.HideMatchResultPanel();
        _matchGameManager.AbortCurrentMatch();

        RefreshUI();
        Debug.Log($"[TournamentManager] 대진표 복원 완료 — 라운드 {_currentRoundIndex}, 팀 수 {_teamCount}");
    }
}
