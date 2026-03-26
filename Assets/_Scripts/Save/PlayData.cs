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
    public SavedMessengerData messenger = new();                // 메신저 상태
    public bool isRecruitmentInProgress;                        // 새 게임 첫 영입 진행 중 여부
    public List<SavedGraduationRecord> graduationRecords = new(); // 졸업 기록 목록 (졸업 날짜, 등급, 보상)
    public List<PendingGraduateGift> pendingGraduateGifts = new(); // 대기 중인 졸업 선물 목록 (랜덤 날짜에 지급 예약된 보상)
    public EquipmentSaveData equipment = new();                 // 장비 강화 상태
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
    public bool hasPlayedVn10002;                // 10002 시청 여부 저장
    public bool hasPlayedVn10003;                // 10003 시청 여부 저장

    // 친선경기 상세 저장
    public string friendlyMatchDate;             // yyyy-MM-dd
    public string friendlyOpponentName;          // 상대 학교명
    public bool friendlyMatchConfirmed;          // 확정 여부

    // 친선경기 월별 신청 횟수 저장
    public int friendlyMatchApplyCount;
    public int friendlyMatchLastMonth;

    // itemeffect_01 영구 보너스 누적값
    public float subsidyPermBonusRate = 0f;

    // itemeffect_06 일시적 훈련 효과 상승 저장
    public string trainingBoostExpireDate = ""; // yyyy-MM-dd, 없으면 빈 문자열
    public string trainingBoostStatKey = "";    // 대상 스탯 키

    // 토너먼트 관련 저장
    public int semiFinalReachedCount;
    public float trainingEfficiencyPermBonusRate;

    public DateTime ParseTrainingBoostExpireDate()
    {
        return DateTime.TryParse(trainingBoostExpireDate, out DateTime result) ? result : default;
    }

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
    public string positionId;
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
    public int shootExp;
    public int speedExp;
    public int jumpExp;
    public int staminaExp;
    public int mentalExp;

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

    public int abnormalState;           // 상태이상 종류
    public int abnormalRemainTurn;      // 상태이상 남은 턴
    public string abnormalReasonTextId; // 상태이상 사유
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
    public bool rollQuarterInjury;
    public List<SavedQuarterScore> quarterScores = new(); // 완료된 쿼터 점수 누적
    public List<string> logs = new();                     // 경기 로그
    public List<SavedMatchStudentStatSnapshot> studentStatSnapshots = new(); // 경기 시작 시점 학생 스탯 스냅샷
    public List<SavedPendingAbnormalData> pendingAbnormals = new(); // 끝나고 적용시킬 부상
}

[Serializable]
public class SavedQuarterScore
{
    public int quarter;
    public int myScore;
    public int opponentScore;
}

[Serializable]
public class SavedMatchStudentStatSnapshot
{
    public int studentId;
    public int mental;
    public int shoot;
    public int speed;
    public int jump;
    public int stamina;
}

[Serializable]
public class SavedPendingAbnormalData
{
    public int studentId;               // 누구 부상인지
    public int abnormalState;           // 상태이상 종류
    public int abnormalRemainTurn;      // 턴 수
    public string abnormalReasonTextId; // 상세 사유
}

[Serializable]
public class UserData
{
    public int reputation;                                // 영구 명성치
    public List<int> unlockedNodeIds = new();             // 영구 감독 노드 해금 목록
}

// 메신저 전체 저장 데이터
// 현재 보고 있던 방 ID
// 채팅방 목록
[Serializable]
public class SavedMessengerData
{
    public string currentViewingRoomId;
    public List<SavedChatRoomData> rooms = new();
}

// 채팅방 1개 저장 데이터
// 방 ID / 이름 / 읽음 상태 / 마지막 갱신 시각 / 메시지 목록
[Serializable]
public class SavedChatRoomData
{
    public string roomId;
    public string roomName;
    public bool hasUnread;
    public string lastUpdatedDate;
    public List<SavedChatMessageData> messages = new();
}

// 메시지 1개 저장 데이터
// 발신자 / 메시지 타입 / 내용 / 시각 / 선택지 선택 상태
[Serializable]
public class SavedChatMessageData
{
    public int senderType;
    public int eventType;
    public string content;
    public string timestamp;

    public int selectedChoiceIndex = -1;
    public List<SavedChoiceOptionData> choices = new();
}

// 선택지 버튼 1개 저장 데이터
// 표시 텍스트만 저장
[Serializable]
public class SavedChoiceOptionData
{
    public string text;
}

// 졸업생 1명의 기록
[Serializable]
public class SavedGraduationRecord
{
    public string graduationDate; // 졸업 날짜 (yyyy-MM-dd)
    public int gradeIndex;        // 등급 (1~4)
    public string gradeLabel;     // 등급 텍스트 ("1등급" 등)
    public string rewardType;     // 받은 보상 종류 (없으면 빈 문자열)
}

// 졸업 선물 대기 항목 1개
[Serializable]
public sealed class PendingGraduateGift
{
    public string studentId;
    public string studentName;
    public string gradeLabel;

    // 실제 실행/저장용 키 (ex. itemeffect_01)
    public string rewardId;

    public string rewardName;

    public string triggerDate; // yyyy-MM-dd
}