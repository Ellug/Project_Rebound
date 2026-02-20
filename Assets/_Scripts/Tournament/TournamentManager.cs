using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class TournamentManager : MonoBehaviour
{
    private const string LobbyScene = "Lobby";

    [Header("References")]
    [SerializeField] private TournamentUI _tournamentUi;

    [Header("Tournament")]
    [SerializeField] private int _teamCount = 32;
    [SerializeField] private string _mySchoolName = "한울고등학교";

    // 토너먼트 진행 데이터
    private readonly List<List<Matchup>> _allRounds = new(); // 라운드별 매치업 리스트 (32강, 16강, 8강, 4강, 결승)
    private int _currentRoundIndex;
    private bool _isWaitingForResultNext;

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
        GenerateTournament();
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

        if (_tournamentUi != null)
        {
            _tournamentUi.HideMatchGamePanel();
            _tournamentUi.HideMatchResultPanel();
        }

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
            GameManager.Instance.SetPendingTournamentChampion(champion);
        else
            Debug.LogWarning("[TournamentManager] GameManager가 없어 우승 결과를 저장하지 못했습니다.");

        SceneManager.LoadScene(LobbyScene);
    }

    private void RefreshUI()
    {
        if (_tournamentUi == null)
            return;

        List<Matchup> currentRound = _allRounds[_currentRoundIndex];
        List<TournamentMatchViewData> matchViewData = new(currentRound.Count);
        for (int i = 0; i < currentRound.Count; i++)
        {
            Matchup matchup = currentRound[i];
            matchViewData.Add(new TournamentMatchViewData(matchup.UpTeam, matchup.DownTeam, matchup.IncludeMySchool));
        }

        _tournamentUi.RenderRound(matchViewData);
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
            int randomIndex = Random.Range(0, i + 1);
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
                string winner = Random.value > 0.5f ? matchup.UpTeam : matchup.DownTeam;
                SetMatchWinner(i, winner);
            }
        }

        // 내 학교 매치가 있으면 매치 게임 패널 활성화
        if (TryGetPendingMySchoolMatch(out Matchup mySchoolMatchup))
        {
            _tournamentUi.ShowMatchGamePanel(mySchoolMatchup.UpTeam, mySchoolMatchup.DownTeam);
        }
        else
            Debug.Log("[TournamentManager] 내 학교 매치가 없어서 자동 진행됨");
    }

    private bool TryGetPendingMySchoolMatch(out Matchup mySchoolMatchup)
    {
        List<Matchup> currentRound = _allRounds[_currentRoundIndex];

        foreach (Matchup matchup in currentRound)
        {
            if (matchup.IncludeMySchool && matchup.Winner == null)
            {
                mySchoolMatchup = matchup;
                return true;
            }
        }

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
        List<Matchup> currentRound = _allRounds[_currentRoundIndex];
        for (int i = 0; i < currentRound.Count; i++)
        {
            Matchup matchup = currentRound[i];
            if (matchup.IncludeMySchool && matchup.Winner == null)
            {
                string winner = didWin ? _mySchoolName : (matchup.UpTeam == _mySchoolName ? matchup.DownTeam : matchup.UpTeam);
                SetMatchWinner(i, winner, advanceWhenRoundComplete: false);
                return true;
            }
        }

        return false;
    }

    // 결과 패널의 다음 버튼에 연결
    public void OnClickNextAfterMatchResult()
    {
        if (!_isWaitingForResultNext)
            return;

        _isWaitingForResultNext = false;
        if (_tournamentUi != null)
            _tournamentUi.HideMatchResultPanel();

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

        if (_tournamentUi != null)
        {
            _tournamentUi.HideMatchGamePanel();
            _tournamentUi.ShowMatchResultPanel(didWin ? "승리!" : "패배...");
        }

        _isWaitingForResultNext = true;
        Debug.Log(didWin ? "[TournamentManager] 우리 학교 승리!" : "[TournamentManager] 우리 학교 패배...");
    }

}
