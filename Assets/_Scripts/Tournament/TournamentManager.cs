using System;
using System.Collections.Generic;
using UnityEngine;

// 토너먼트 전체 흐름 관리.
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
    private int _mySchoolReachedRoundTeamCount;      // 토너먼트 종료 시 로비에 전달할 성적(1~4위 또는 8/16/32강)
    private bool _mySchoolDefeatedThisMatch;         // 직전 우리 학교 경기에서 탈락했는지 여부
    private bool _isFriendlyMatchMode;               // 친선전 모드 여부
    private string _friendlyOpponentName;            // 친선전 상대 학교명
    private bool _isWaitingThirdPlaceMatch;          // 4강 탈락 후 3/4위전 진입 대기
    private bool _isThirdPlaceMatchInProgress;       // 3/4위전 진행 중 여부
    private string _thirdPlaceOpponentName;          // 3/4위전 상대 학교명

    // 매치업 데이터 보관
    private class Matchup
    {
        public string UpTeam;
        public string DownTeam;
        public string Winner; // null이면 아직 진행 안함
        public bool IncludeMySchool;
    }

    // 시작 시 모드 판별 및 저장 데이터 복원 수행
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

    // 종료 시 매치 완료 이벤트 구독 해제 수행
    void OnDestroy()
    {
        _matchGameManager.OnMatchFinished -= HandleMySchoolMatchFinished;
    }

    // 일반 토너먼트 대진표 초기화 및 생성 수행
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
        _isWaitingThirdPlaceMatch = false;
        _isThirdPlaceMatchInProgress = false;
        _thirdPlaceOpponentName = string.Empty;

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

    // 친선전 모드 초기화 및 단일 경기 시작 수행
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
        _isWaitingThirdPlaceMatch = false;
        _isThirdPlaceMatchInProgress = false;
        _thirdPlaceOpponentName = string.Empty;

        // 친선전에서는 브래킷 패널을 숨기고 경기 UI만 사용한다.
        _tournamentUi.HideTournamentPanels();
        _matchGameUi.HideMatchResultPanel();
        _matchGameManager.AbortCurrentMatch();
        // 우리 학교 vs 상대 학교 단일 매치를 바로 시작한다.
        _matchGameManager.StartMatch(_mySchoolName, _friendlyOpponentName, _mySchoolName, rollQuarterInjury: false);
        _matchGameManager.QueueFriendlyStartInjury();
    }

    // 현재 라운드 매치 승자 반영 수행
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

    // 다음 라운드 매치업 생성 및 UI 갱신 수행
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

    // 토너먼트 종료 결과 전달 및 씬 전환 수행
    private void OnTournamentComplete()
    {
        // 우승 결과 캐싱 후 로비로 복귀
        _matchGameManager.ApplyPendingAbnormals();
        GameManager.Instance.SetPendingTournamentResult(_mySchoolReachedRoundTeamCount);

        // 탈락 처리 완료 후 세이브 (isInProgress=false 상태로 저장)
        SaveManager.Instance?.AutoSaveByBranch("토너먼트 탈락 처리");

        if (_mySchoolReachedRoundTeamCount == 1)
        {
            if (GameManager.Instance.TryEnterFirstWinterChampionStory())
                return;
        }

        SceneTransitionManager.Instance.LoadScene(LobbyScene);
    }

    // 브래킷 뷰 데이터 구성 및 UI 렌더링 수행
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

    // 학교 테이블 기반 참가 학교 목록 구성 수행
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

    // 학교명 표시 포맷 보정 수행
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

    // 우리 학교 고정 후 나머지 학교 셔플 수행
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

    // 단일 매치업 데이터 생성 수행
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

    // 전달된 학교명이 우리 학교인지 비교 수행
    private bool IsMySchool(string teamName)
    {
        return string.Equals(teamName, _mySchoolName, StringComparison.Ordinal);
    }

    // 현재 라운드에서 우리 학교 제외 경기 자동 진행 수행
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
            _matchGameManager.StartMatch(mySchoolMatchup.UpTeam, mySchoolMatchup.DownTeam, _mySchoolName, rollQuarterInjury: true);
        else
            Debug.Log("[TournamentManager] 내 학교 매치가 없어서 자동 진행");
    }

    // 경기 진행 버튼 입력을 매치 매니저로 전달 수행
    public void OnClickProgressMySchoolMatch()
    {
        _matchGameManager.ProgressMatchStep();
    }

    // 진행 대기 중인 우리 학교 매치 탐색 수행
    private bool TryGetPendingMySchoolMatch(out Matchup mySchoolMatchup)
    {
        return TryGetPendingMySchoolMatch(out _, out mySchoolMatchup);
    }

    // 진행 대기 중인 우리 학교 매치와 인덱스 탐색 수행
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

    // 우리 학교 승리 결과 분기 처리
    public void OnMySchoolWin()
    {
        // 친선전 모드에서는 대진 갱신 없이 결과 패널만 노출
        if (_isFriendlyMatchMode)
        {
            ShowFriendlyMatchResult(true);
            return;
        }

        // 3/4위전 진행 중에는 브래킷 매치 갱신 없이 3/4위전 결과만 처리
        if (_isThirdPlaceMatchInProgress)
        {
            ResolveThirdPlaceMatchAndShowResult(true);
            return;
        }

        ResolveMySchoolMatchAndShowResult(true);
    }

    // 우리 학교 패배 결과 분기 처리
    public void OnMySchoolLose()
    {
        // 친선전 모드에서는 대진 갱신 없이 결과 패널만 노출
        if (_isFriendlyMatchMode)
        {
            ShowFriendlyMatchResult(false);
            return;
        }

        // 3/4위전 진행 중에는 브래킷 매치 갱신 없이 3/4위전 결과만 처리
        if (_isThirdPlaceMatchInProgress)
        {
            ResolveThirdPlaceMatchAndShowResult(false);
            return;
        }

        ResolveMySchoolMatchAndShowResult(false);
    }

    // 우리 학교 매치 승자 반영 수행
    private bool SetMySchoolMatchResult(bool didWin)
    {
        if (!TryGetPendingMySchoolMatch(out int matchIndex, out Matchup matchup))
            return false;

        string winner = didWin ? _mySchoolName : matchup.DownTeam;
        SetMatchWinner(matchIndex, winner, advanceWhenRoundComplete: false);
        return true;
    }

    // 결과 패널 다음 버튼 후속 흐름 처리
    public void OnClickNextAfterMatchResult()
    {
        if (!_isWaitingForResultNext) return;

        _isWaitingForResultNext = false;

        // 친선전 결과 확인 후 즉시 로비로 복귀
        if (_isFriendlyMatchMode)
        {
            _matchGameManager.ApplyPendingAbnormals();
            SceneTransitionManager.Instance.LoadScene(LobbyScene);
            return; 
        }

        // 우리 학교가 직전 경기에서 패배한 경우에만 토너먼트 종료
        if (_mySchoolDefeatedThisMatch)
        {
            if (_isWaitingThirdPlaceMatch)
            {
                if (TryStartThirdPlaceMatch())
                    return;
            }

            _matchGameManager.ApplyPendingAbnormals();
            GameManager.Instance.SetPendingTournamentResult(_mySchoolReachedRoundTeamCount);
            SaveManager.Instance?.AutoSaveByBranch("토너먼트 탈락 처리");
            SceneTransitionManager.Instance.LoadScene(LobbyScene);
            return;
        }

        if (IsCurrentRoundComplete())
        {            
            _matchGameUi.HideMatchResultPanel();
            AdvanceToNextRound();            
        }
    }

    // 우리 학교 매치 결과 반영 및 결과 패널 표시 수행
    private void ResolveMySchoolMatchAndShowResult(bool didWin)
    {
        if (!SetMySchoolMatchResult(didWin))
        {
            Debug.LogWarning("[TournamentManager] 진행 중인 우리 학교 매치를 찾지 못했습니다.");
            return;
        }

        if (didWin) SoundManager.Instance.PlayBGM(106);

        _matchGameManager.AbortCurrentMatch();

        UpdateMySchoolTournamentProgress(didWin);
        PrepareThirdPlaceMatchIfNeeded(didWin);
        _mySchoolDefeatedThisMatch = !didWin;

        // 세이브 전에 결과를 미리 GameManager에 등록
        // pendingTournamentReachedCount가 세이브에 포함됨
        if (!didWin || IsAllRoundsComplete())
        {
            GameManager.Instance.SetPendingTournamentResult(_mySchoolReachedRoundTeamCount);
        }

        // winner 기록 완료 후 즉시 세이브
        // 이 시점에 IsAllRoundsComplete()가 true면 isInProgress=false로 저장됨
        SaveManager.Instance?.AutoSaveByBranch("경기 결과 확정");

        _matchGameUi.HideMatchGamePanel();
        _matchGameUi.ShowMatchResultPanel(didWin);

        _isWaitingForResultNext = true;
        Debug.Log(didWin ? "[TournamentManager] 우리 학교 승리!" : "[TournamentManager] 우리 학교 패배...");
    }

    // 4강 탈락 시 3/4위전 진입 대기 상태 설정 수행
    private void PrepareThirdPlaceMatchIfNeeded(bool didWin)
    {
        _isWaitingThirdPlaceMatch = false;
        _thirdPlaceOpponentName = string.Empty;

        if (didWin) return;

        // 4강에서 탈락했을 때만 3/4위전을 진행
        if (GetCurrentRoundTeamCount() != 4) return;

        if (!TryResolveThirdPlaceOpponent(out string opponentName))
        {
            Debug.LogWarning("[TournamentManager] 3/4위전 상대를 찾지 못해 4위 처리로 종료합니다.");
            return;
        }

        _thirdPlaceOpponentName = opponentName;
        _isWaitingThirdPlaceMatch = true;
    }

    // 반대 4강 경기 패배팀을 3/4위전 상대팀으로 탐색 수행
    private bool TryResolveThirdPlaceOpponent(out string opponentName)
    {
        opponentName = string.Empty;

        if (_currentRoundIndex < 0 || _currentRoundIndex >= _allRounds.Count)
            return false;

        List<Matchup> currentRound = _allRounds[_currentRoundIndex];
        if (currentRound == null)
            return false;

        for (int i = 0; i < currentRound.Count; i++)
        {
            Matchup matchup = currentRound[i];
            if (matchup == null || matchup.IncludeMySchool)
                continue;

            if (string.IsNullOrEmpty(matchup.Winner))
                continue;

            if (string.Equals(matchup.Winner, matchup.UpTeam, StringComparison.Ordinal))
                opponentName = matchup.DownTeam;
            else if (string.Equals(matchup.Winner, matchup.DownTeam, StringComparison.Ordinal))
                opponentName = matchup.UpTeam;
            else
                continue;

            return !string.IsNullOrWhiteSpace(opponentName);
        }

        return false;
    }

    // 3/4위전 대기 상태 확인 후 경기 시작 수행
    private bool TryStartThirdPlaceMatch()
    {
        if (!_isWaitingThirdPlaceMatch)
            return false;

        if (string.IsNullOrWhiteSpace(_thirdPlaceOpponentName))
            return false;

        _isWaitingThirdPlaceMatch = false;
        _isThirdPlaceMatchInProgress = true;
        _mySchoolDefeatedThisMatch = false;

        _matchGameUi.HideMatchResultPanel();
        _matchGameManager.StartMatch(_mySchoolName, _thirdPlaceOpponentName, _mySchoolName, rollQuarterInjury: true);
        return true;
    }

    // 3/4위전 결과 확정 및 결과 패널 표시 수행
    private void ResolveThirdPlaceMatchAndShowResult(bool didWin)
    {
        _isThirdPlaceMatchInProgress = false;
        _isWaitingThirdPlaceMatch = false;
        _thirdPlaceOpponentName = string.Empty;

        _matchGameManager.AbortCurrentMatch();

        // 3/4위전 결과를 로비 결과 화면 전달값으로 확정
        _mySchoolReachedRoundTeamCount = didWin ? 3 : 4;
        _mySchoolDefeatedThisMatch = true;
        _isWaitingForResultNext = true;

        _matchGameUi.HideMatchGamePanel();
        _matchGameUi.ShowMatchResultPanel(didWin);
    }

    // 라운드 기준 토너먼트 성적 값 갱신 수행
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

    // 현재 라운드 팀 수 계산 수행
    private int GetCurrentRoundTeamCount()
    {
        if (_currentRoundIndex < 0 || _currentRoundIndex >= _allRounds.Count)
            return 0;

        return _allRounds[_currentRoundIndex].Count * 2;
    }

    // 친선전 결과 처리 및 결과 패널 표시 수행
    private void ShowFriendlyMatchResult(bool didWin)
    {
        _matchGameManager.ApplyPendingAbnormals();
        _matchGameManager.AbortCurrentMatch();
        _mySchoolDefeatedThisMatch = !didWin;
        _isWaitingForResultNext = true;

        if (didWin) SoundManager.Instance.PlayBGM(106);

        GameManager.Instance.SetPendingFriendlyMatchResult(didWin, _friendlyOpponentName);

        // 친선전은 토너먼트 성적 대신 승패 화면만 표시한다.
        _matchGameUi.HideMatchGamePanel();
        _matchGameUi.ShowMatchResultPanel(didWin);
        Debug.Log(didWin ? "[TournamentManager] 친선전 승리!" : "[TournamentManager] 친선전 패배...");
    }

    // 매치 완료 이벤트 수신 후 승패 분기 처리
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

        if (_isThirdPlaceMatchInProgress)
        {
            ResolveThirdPlaceMatchAndShowResult(didWin);
            return;
        }

        if (didWin)     OnMySchoolWin();
        else            OnMySchoolLose();
    }

    // 현재 토너먼트 상태 직렬화 데이터 생성 수행
    public SavedTournamentData CollectSaveData()
    {
        // 결승전까지 모든 라운드의 winner가 확정된 경우 → 토너먼트 종료로 판단
        bool allRoundsComplete = _allRounds.Count > 0 && IsAllRoundsComplete();

        SavedTournamentData data = new()
        {
            // _isMatchRunning은 MatchGameManager 소유 — 대진표가 존재하면 진행 중으로 판단
            isInProgress = _allRounds.Count > 0 && !allRoundsComplete,
            teamCount = _teamCount,
            currentRoundIndex = _currentRoundIndex,
            mySchoolReachedRoundTeamCount = _mySchoolReachedRoundTeamCount,
            isWaitingThirdPlaceMatch = _isWaitingThirdPlaceMatch,
            isThirdPlaceMatchInProgress = _isThirdPlaceMatchInProgress,
            thirdPlaceOpponentName = _thirdPlaceOpponentName ?? string.Empty,
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

    // 저장된 토너먼트 상태를 런타임으로 복원 수행
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
        _isWaitingThirdPlaceMatch = data.isWaitingThirdPlaceMatch;
        _isThirdPlaceMatchInProgress = data.isThirdPlaceMatchInProgress;
        _thirdPlaceOpponentName = data.thirdPlaceOpponentName ?? string.Empty;

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

    // 모든 라운드의 모든 매치에 winner가 기록됐는지 확인
    private bool IsAllRoundsComplete()
    {
        foreach (List<Matchup> round in _allRounds)
        {
            foreach (Matchup matchup in round)
            {
                if (string.IsNullOrEmpty(matchup.Winner))
                    return false;
            }
        }
        return true;
    }

#if UNITY_EDITOR
    [ContextMenu("Debug - Skip To Final Win")]
    private void DebugSkipToFinalWin()
    {
        for (int roundIdx = 0; roundIdx < _allRounds.Count; roundIdx++)
        {
            List<Matchup> round = _allRounds[roundIdx];
            bool isLastRound = (roundIdx == _allRounds.Count - 1);

            for (int i = 0; i < round.Count; i++)
            {
                Matchup matchup = round[i];
                if (matchup.Winner != null) continue;
                matchup.Winner = matchup.IncludeMySchool ? _mySchoolName : matchup.UpTeam;
            }

            if (!isLastRound)
            {
                List<string> winners = new();
                foreach (Matchup m in round) winners.Add(m.Winner);

                _currentRoundIndex = roundIdx + 1;
                List<Matchup> nextRound = _allRounds[_currentRoundIndex];
                nextRound.Clear();

                int nextMatchupCount = winners.Count / 2;
                for (int i = 0; i < nextMatchupCount; i++)
                    nextRound.Add(CreateMatchup(winners[i * 2], winners[i * 2 + 1]));
            }
        }

        RefreshUI();

        _currentRoundIndex = _allRounds.Count - 1;
        UpdateMySchoolTournamentProgress(true);
        _mySchoolDefeatedThisMatch = false;

        _matchGameManager.AbortCurrentMatch();
        _matchGameUi.HideMatchGamePanel();

        GameManager.Instance.SetPendingTournamentResult(_mySchoolReachedRoundTeamCount);
        SaveManager.Instance?.AutoSaveByBranch("경기 결과 확정");

        _matchGameUi.ShowMatchResultPanel(true);
        _isWaitingForResultNext = true;

        Debug.Log($"[Debug] 결승 승리 스킵 완료 | reachedCount={_mySchoolReachedRoundTeamCount}");
    }
#endif
}
