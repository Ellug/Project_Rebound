using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class TournamentManager : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private ScrollRect _scrollRect;
    [SerializeField] private RectTransform _contentRoot;
    [SerializeField] private GameObject _matchupContainerPrefab;

    [Header("Tournament")]
    [SerializeField] private int _teamCount = 32;
    [SerializeField] private string _mySchoolName = "한울고등학교";

    // 나중에 데이터 테이블 들어올 거임 그때 SO 참조하도록
    private static readonly string[] TempSchoolPrefixes =
    {
        "가람", "나래", "다온", "라온", "마루", "보람", "서림", "아람",
        "자람", "차온", "하람", "고운", "누리", "도담", "로하", "모아",
        "소담", "오름", "유진", "주원", "채움", "태강", "푸름", "하늘",
        "다솔", "별빛", "초원", "청운", "해솔", "백운", "동해", "남강",
    };

    // 토너먼트 진행 데이터
    private readonly List<List<Matchup>> _allRounds = new(); // 라운드별 매치업 리스트 (32강, 16강, 8강, 4강, 결승)
    private int _currentRoundIndex;

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
        GenerateTemporaryTournament();
    }

    public void GenerateTemporaryTournament()
    {
        // 학교 목록 생성 및 셔플
        List<string> schools = BuildSchoolList();
        Shuffle(schools);

        // 토너먼트 초기화 - 32강부터 결승까지 구조 생성
        _allRounds.Clear();
        _currentRoundIndex = 0;

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

        RefreshUI();
    }

    // 매치 승자 처리 (외부에서 호출 가능)
    public void SetMatchWinner(int matchIndex, string winnerTeamName)
    {
        List<Matchup> currentRound = _allRounds[_currentRoundIndex];
        currentRound[matchIndex].Winner = winnerTeamName;

        // 현재 라운드의 모든 매치가 끝났는지 확인
        if (IsCurrentRoundComplete())
        {
            AdvanceToNextRound();
        }
    }

    // 현재 라운드 완료 여부 확인
    private bool IsCurrentRoundComplete()
    {
        List<Matchup> currentRound = _allRounds[_currentRoundIndex];
        foreach (Matchup matchup in currentRound)
        {
            if (matchup.Winner == null)
                return false;
        }
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
        {
            winners.Add(matchup.Winner);
        }

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
    }

    // 테스트용: 현재 라운드의 모든 매치를 랜덤으로 진행
    public void AutoProgressCurrentRound()
    {
        List<Matchup> currentRound = _allRounds[_currentRoundIndex];
        for (int i = 0; i < currentRound.Count; i++)
        {
            Matchup matchup = currentRound[i];
            if (matchup.Winner == null)
            {
                // 랜덤으로 승자 선택
                string winner = Random.value > 0.5f ? matchup.UpTeam : matchup.DownTeam;
                SetMatchWinner(i, winner);
            }
        }
    }

    private void RefreshUI()
    {
        ClearOldMatchups();

        // 현재 라운드의 매치업만 UI에 표시
        List<Matchup> currentRound = _allRounds[_currentRoundIndex];
        for (int i = 0; i < currentRound.Count; i++)
        {
            Matchup matchup = currentRound[i];
            GameObject matchupObject = Instantiate(_matchupContainerPrefab, _contentRoot);
            matchupObject.name = $"MatchupContainer ({i + 1})";
            matchupObject.SetActive(true);

            matchupObject.GetComponent<MatchupContainerUI>().SetData(
                matchup.UpTeam,
                matchup.DownTeam,
                matchup.IncludeMySchool
            );
        }

        // UI 갱신 및 스크롤 최상단 이동
        LayoutRebuilder.ForceRebuildLayoutImmediate(_contentRoot);
        _scrollRect.verticalNormalizedPosition = 1f;
    }

    private List<string> BuildSchoolList()
    {
        List<string> schools = new(_teamCount) { _mySchoolName };

        for (int i = 0; i < _teamCount - 1; i++)
            schools.Add($"{TempSchoolPrefixes[i]} 고등학교");

        return schools;
    }

    private static void Shuffle(List<string> list)
    {
        // Fisher-Yates 셔플
        for (int i = list.Count - 1; i > 0; i--)
        {
            int randomIndex = Random.Range(0, i + 1);
            (list[randomIndex], list[i]) = (list[i], list[randomIndex]);
        }
    }

    private void ClearOldMatchups()
    {
        // 기존 매치업 UI 전부 제거
        for (int i = _contentRoot.childCount - 1; i >= 0; i--)
        {
            GameObject childObject = _contentRoot.GetChild(i).gameObject;
            childObject.SetActive(false);
            Destroy(childObject);
        }
    }
}
