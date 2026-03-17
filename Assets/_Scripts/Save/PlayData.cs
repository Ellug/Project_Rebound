using System;
using System.Collections.Generic;

[Serializable]
public class PlayData
{
    public int slotIndex;                                       // 저장 슬롯
    public string school;                                       // 학교 이름
    public string playTime;                                     // 인게임 날짜
    public string saveTime;                                     // 저장시 현실 시간
    public int gold;                                            // 재화
    public int reputation;                                      // 명성치

    //public List<string> items = new();                        // 보유 아이템 목록
    public List<int> unlockedNodeIds = new();                   // 감독 노드 해금 목록 (농구부 폐부 후 재시작해도 유지)
    public SavedFlowData flowData = new();                      // 날짜 / 턴 진행 상태 (GameFlowData 대응)
    public SavedFacilityData facilities = new();                // 시설 레벨
    public List<SavedStudentData> students = new();
    public List<SavedSlotAssignment> slotAssignments = new();   // JsonUtility가 Dictionary 직렬화 불가 → List로 저장
    public SavedTournamentData tournament = new();
    public SavedMatchSimData matchSim = new();                  // 경기 시뮬레이션 진행 상태
}

// GameFlowData 날짜/턴 관련 필드 대응
// JsonUtility가 DateTime/HashSet을 직렬화할 수 없어 string/List로 저장
[Serializable]
public class SavedFlowData
{
    public string currentDate;                   // DateTime → "yyyy-MM-dd" 형식 문자열
    public int turnIndex;
    public int dayIndex;
    public int currentYear;
    public GamePhase phase;
    public bool isLeagueOpened;
    public bool isLeagueHandled;
    public string leagueTermEnd;                 // DateTime → "yyyy-MM-dd", 없으면 빈 문자열
    public List<string> activeEventIds = new();  // HashSet<string> → List<string>
    public int maxRecruitCount;                  // 모집 정원 저장
    public bool hasPendingFriendlyMatch;         // 친선경기 예약 여부 저장
    public bool hasPlayedVn10001;                // 10001 시청 여부 저장
    public bool hasPlayedVn10002;                // 10002 시청 여부 저장
    public bool hasPlayedVn10003;                // 10003 시청 여부 저장

    // 친선경기 상세 저장
    public string friendlyMatchDate;             // yyyy-MM-dd
    public string friendlyOpponentName;          // 상대 학교명
    public bool friendlyMatchConfirmed;          // 확정 여부

    // string → DateTime 변환 (파싱 실패 시 default 반환)
    public DateTime ParseCurrentDate()
    {
        return DateTime.TryParse(currentDate, out DateTime result) ? result : default;
    }

    public DateTime ParseLeagueTermEnd()
    {
        return DateTime.TryParse(leagueTermEnd, out DateTime result) ? result : default;
    }

    public DateTime ParseFriendlyMatchDate()
    {
        return DateTime.TryParse(friendlyMatchDate, out DateTime result) ? result : default;
    }
}

// FacilitySystem._levels 대응
[Serializable]
public class SavedFacilityData
{
    public int schoolLevel = 1;
    public int gymLevel = 1;
    public int cafeteriaLevel = 1;
    public int counselingCenterLevel = 1;
}

// Student 클래스 필드 대응
[Serializable]
public class SavedStudentData
{
    // 기본 정보
    public int id;
    public string studentName;
    public string positionName;
    public int grade;

    // 이미지 배정
    public CharacterColor portraitColor;
    public int portraitIndex;

    // 신체 정보
    public int height;
    public int weight;

    // 기본 스탯
    public int mental;
    public int shoot;
    public int speed;
    public int jump;
    public int stamina;

    // 잠재 능력
    public int potentialTier;
    public string potential;

    // 컨디션 및 상태
    public int condition;

    // 이벤트 효과
    public List<string> activeEffectIds = new();
    public int conditionRecoveryBonus;
    public float trainingEfficiencyBonus;
    public bool isTrainingBlocked;
}

// 슬롯 인덱스와 학생 ID 쌍으로 배치 정보 저장
[Serializable]
public class SavedSlotAssignment
{
    public int slotIndex;
    public int studentId;
}

// TournamentManager 내부 상태 대응
[Serializable]
public class SavedTournamentData
{
    public bool isInProgress;
    public int teamCount;
    public int currentRoundIndex;
    public int mySchoolReachedRoundTeamCount;             // 현재까지 도달한 라운드의 팀 수
    public List<SavedRoundData> allRounds = new();        // _allRounds 대응
}

[Serializable]
public class SavedRoundData
{
    public List<SavedMatchupData> matchups = new();
}

[Serializable]
public class SavedMatchupData
{
    public string upTeam;
    public string downTeam;
    public string winner;                                 // null 또는 빈 문자열이면 아직 미진행
    public bool includeMySchool;
}

// MatchGameManager 경기 시뮬레이션 진행 상태 대응
[Serializable]
public class SavedMatchSimData
{
    public bool isMatchRunning;
    public string upTeam;
    public string downTeam;
    public string mySchoolName;
    public int progressStageIndex;                        // MatchGameStages.Default 배열 인덱스
    public List<SavedQuarterScore> quarterScores = new(); // 완료된 쿼터 점수 누적
    public List<string> logs = new();                     // 경기 로그
}

[Serializable]
public class SavedQuarterScore
{
    public int quarter;
    public int myScore;
    public int opponentScore;
}

[Serializable]
public class UserData
{
    public int reputation;                                // 영구 명성치
    public List<int> unlockedNodeIds = new();             // 영구 감독 노드 해금 목록
}
