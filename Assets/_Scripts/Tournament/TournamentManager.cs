using System.Collections.Generic;
using TMPro;
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

    [Header("Match")]
    [SerializeField] private GameObject _matchGamePanel;
    [SerializeField] private TMP_Text _leftSchoolText;
    [SerializeField] private TMP_Text _rightSchoolText;

    // 나중에 데이터 테이블 들어올 거임 그때 SO 참조하도록
    private static readonly string[] TempSchoolPrefixes =
    {
        "가람", "나래", "다온", "라온", "마루", "보람", "서림", "아람",
        "자람", "차온", "하람", "고운", "누리", "도담", "로하", "모아",
        "소담", "오름", "유진", "주원", "채움", "태강", "푸름", "하늘",
        "다솔", "별빛", "초원", "청운", "해솔", "백운", "동해", "남강",
    };
    private const string LeftSchoolTextPath = "Conainer/Score Panel/LeftTeam Panel/School Text (TMP)";
    private const string RightSchoolTextPath = "Conainer/Score Panel/RightTeam Panel/School Text (TMP)";

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
        CacheMatchGameSchoolTexts();
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
            UpdateMatchGameSchoolTexts(mySchoolMatchup);

            if (_matchGamePanel != null)
                _matchGamePanel.SetActive(true);
            else
                Debug.LogWarning("[TournamentManager] MatchGame Panel 참조가 비어 있습니다.");
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
        SetMySchoolMatchResult(true);
        _matchGamePanel.SetActive(false);
        Debug.Log("[TournamentManager] 우리 학교 승리!");
    }

    // 내 학교 패배 처리
    public void OnMySchoolLose()
    {
        SetMySchoolMatchResult(false);
        _matchGamePanel.SetActive(false);
        Debug.Log("[TournamentManager] 우리 학교 패배...");
    }

    // 내 학교 매치 승자 처리
    private void SetMySchoolMatchResult(bool didWin)
    {
        List<Matchup> currentRound = _allRounds[_currentRoundIndex];
        for (int i = 0; i < currentRound.Count; i++)
        {
            Matchup matchup = currentRound[i];
            if (matchup.IncludeMySchool && matchup.Winner == null)
            {
                string winner = didWin ? _mySchoolName : (matchup.UpTeam == _mySchoolName ? matchup.DownTeam : matchup.UpTeam);
                SetMatchWinner(i, winner);
                return;
            }
        }
    }

    private void CacheMatchGameSchoolTexts()
    {
        if (_matchGamePanel == null) return;

        if (_leftSchoolText == null)
            _leftSchoolText = FindSchoolText("LeftTeam Panel", LeftSchoolTextPath);

        if (_rightSchoolText == null)
            _rightSchoolText = FindSchoolText("RightTeam Panel", RightSchoolTextPath);
    }

    private TMP_Text FindSchoolText(string teamPanelName, string relativePath)
    {
        Transform schoolTextTransform = _matchGamePanel.transform.Find(relativePath);
        if (schoolTextTransform != null)
            return schoolTextTransform.GetComponent<TMP_Text>();

        TMP_Text[] allTexts = _matchGamePanel.GetComponentsInChildren<TMP_Text>(true);
        foreach (TMP_Text text in allTexts)
        {
            Transform parent = text.transform.parent;
            if (parent != null && parent.name == teamPanelName && text.gameObject.name == "School Text (TMP)")
                return text;
        }

        return null;
    }

    private void UpdateMatchGameSchoolTexts(Matchup mySchoolMatchup)
    {
        CacheMatchGameSchoolTexts();

        if (_leftSchoolText != null)
            _leftSchoolText.text = mySchoolMatchup.UpTeam;

        if (_rightSchoolText != null)
            _rightSchoolText.text = mySchoolMatchup.DownTeam;

        if (_leftSchoolText == null || _rightSchoolText == null)
            Debug.LogWarning("[TournamentManager] Score Panel School Text 참조를 찾지 못했습니다.");
    }
}
