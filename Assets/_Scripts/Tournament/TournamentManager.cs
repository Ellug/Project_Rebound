using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

// 토너먼트 전체 흐름을 관리: 대진 생성 → 라운드 진행 → 결과 판정 → 로비 복귀
public class TournamentManager : MonoBehaviour
{
    private const string LobbyScene = "Lobby";

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
        GenerateTournament();
    }

    private void OnDestroy()
    {
        _matchGameManager.OnMatchFinished -= HandleMySchoolMatchFinished;
    }

    // 토너먼트 대진표 생성
    public void GenerateTournament()
    {
        // 학교 목록 생성 및 셔플
        List<string> schools = BuildSchoolList();
        Shuffle(schools);

        // 토너먼트 초기화 - 32강부터 결승까지 구조 생성
        _allRounds.Clear();
        _currentRoundIndex = 0;
        _isWaitingForResultNext = false;
        _mySchoolReachedRoundTeamCount = _teamCount;

        // 첫 라운드 매치업 생성 (32강)
        List<Matchup> firstRound = new();
        int matchupCount = _teamCount / 2;
        for (int i = 0; i < matchupCount; i++)
        {
            string upTeam = schools[i * 2];
            string downTeam = schools[i * 2 + 1];
            firstRound.Add(new Matchup
            {
                UpTeam = upTeam,
                DownTeam = downTeam,
                Winner = null,
                IncludeMySchool = upTeam == _mySchoolName || downTeam == _mySchoolName
            });
        }
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
        {
            string upTeam = winners[i * 2];
            string downTeam = winners[i * 2 + 1];
            nextRound.Add(new Matchup
            {
                UpTeam = upTeam,
                DownTeam = downTeam,
                Winner = null,
                IncludeMySchool = upTeam == _mySchoolName || downTeam == _mySchoolName
            });
        }

        RefreshUI();
    }

    // 토너먼트 종료 처리
    private void OnTournamentComplete()
    {
        string champion = _allRounds[_currentRoundIndex][0].Winner;
        Debug.Log($"[TournamentManager] 토너먼트 우승: {champion}");

        // 우승 결과 캐싱 후 로비로 복귀
        if (GameManager.Instance != null)
            GameManager.Instance.SetPendingTournamentResult(champion, _mySchoolReachedRoundTeamCount);
        else
            Debug.LogWarning("[TournamentManager] GameManager가 없어 우승 결과를 저장하지 못했습니다.");

        SceneManager.LoadScene(LobbyScene);
    }

    private void RefreshUI()
    {
        List<Matchup> currentRound = _allRounds[_currentRoundIndex];
        List<TournamentMatchViewData> matchViewData = new(currentRound.Count);
        for (int i = 0; i < currentRound.Count; i++)
        {
            Matchup matchup = currentRound[i];
            matchViewData.Add(new TournamentMatchViewData(matchup.UpTeam, matchup.DownTeam, matchup.IncludeMySchool));
        }

        _tournamentUi.RenderRound(matchViewData, _mySchoolName);
    }

    // CachedSOData 에서 참조해 학교 리스트 출력
    private List<string> BuildSchoolList()
    {
        var schoolTable = CachedSOData.SchoolNameTable;
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

    private static void Shuffle(List<string> list)
    {
        for (int i = list.Count - 1; i > 0; i--)
        {
            int randomIndex = UnityEngine.Random.Range(0, i + 1);
            (list[randomIndex], list[i]) = (list[i], list[randomIndex]);
        }
    }

    // 확인 버튼에 인스펙터에서 연결. 현재 라운드 진행 (내 학교 제외)
    public void ProgressCurrentRound()
    {
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
        ResolveMySchoolMatchAndShowResult(true);
    }

    // 내 학교 패배 처리
    public void OnMySchoolLose()
    {
        ResolveMySchoolMatchAndShowResult(false);
    }

    // 내 학교 매치 승자 처리
    private bool SetMySchoolMatchResult(bool didWin)
    {
        if (!TryGetPendingMySchoolMatch(out int matchIndex, out Matchup matchup))
            return false;

        string winner = didWin ? _mySchoolName : (matchup.UpTeam == _mySchoolName ? matchup.DownTeam : matchup.UpTeam);
        SetMatchWinner(matchIndex, winner, advanceWhenRoundComplete: false);
        return true;
    }

    // 결과 패널의 다음 버튼에 연결
    public void OnClickNextAfterMatchResult()
    {
        if (!_isWaitingForResultNext)
            return;

        _isWaitingForResultNext = false;
        _matchGameUi.HideMatchResultPanel();

        // 우리 학교가 패배한 경우: 순위는 이미 기록됐으므로 바로 종료
        if (_mySchoolReachedRoundTeamCount > 1)
        {
            GameManager.Instance.SetPendingTournamentResult(string.Empty, _mySchoolReachedRoundTeamCount);
            SceneManager.LoadScene(LobbyScene);
            return;
        }

        if (IsCurrentRoundComplete())
            AdvanceToNextRound();
    }

    private void ResolveMySchoolMatchAndShowResult(bool didWin)
    {
        if (!SetMySchoolMatchResult(didWin))
        {
            Debug.LogWarning("[TournamentManager] 진행 중인 우리 학교 매치를 찾지 못했습니다.");
            return;
        }

        _matchGameManager.AbortCurrentMatch();

        UpdateMySchoolTournamentProgress(didWin);

        _matchGameUi.HideMatchGamePanel();
        _matchGameUi.ShowMatchResultPanel(didWin ? "승리!" : "패배...");

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

    private void HandleMySchoolMatchFinished(MatchResult matchResult)
    {
        if (matchResult == null)
            return;

        bool didWin = string.Equals(matchResult.winnerTeamName, _mySchoolName, StringComparison.Ordinal);
        Debug.Log($"[TournamentManager] MatchResult 수신 - Winner: {matchResult.winnerTeamName}, Score: {matchResult.finalScore.mySchoolScore}:{matchResult.finalScore.opponentScore}");

        if (didWin)
            OnMySchoolWin();
        else
            OnMySchoolLose();
    }

}
