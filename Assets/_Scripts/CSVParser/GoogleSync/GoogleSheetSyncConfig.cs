#if UNITY_EDITOR
using System.Collections.Generic;

// 구글 시트 → CSV 동기화에 사용할 시트 ID와 각 테이블의 export URL을 정의하는 설정 클래스
public static class GoogleSheetSyncConfig
{
    // 스프레드시트 URL에서 /d/{ID}/edit 부분의 ID
    private const string SHEET_ID = "1ebS4feVi0ngSsAC_qaomDV1kJ0pqkUmEeZUXefQdZX0";
    private static string Url(string gid) => $"https://docs.google.com/spreadsheets/d/{SHEET_ID}/export?format=csv&gid={gid}";

    // 각 탭의 gid는 구글 시트에서 탭 클릭 시 URL 끝의 gid=숫자 부분으로 확인
    private const string GID_GROWTH_COMMAND        = "794913062";
    private const string GID_ALWAYS_EFFECT         = "714199830";
    private const string GID_ALWAYS_EVENT          = "1634482227";
    private const string GID_SUDDEN_EVENT          = "1158268687";
    private const string GID_SUDDEN_EVENT_EFFECT   = "661072925";
    private const string GID_SUDDEN_EVENT_TEXT     = "1729541202";
    private const string GID_STATUS_TEXT           = "236823575"; // 돌발 이벤트 스탯 텍스트
    private const string GID_SCHOOL_NAME           = "1242330876"; // 학교 이름
    private const string GID_ENEMY_STAT            = "505381689"; // 적 스탯
    private const string GID_TUTORIAL_GUIDE        = "1755714998"; // 튜토리얼 가이드
    // 로스터 효과 -> 이거 쓸 일이 있나??? 안 넣음 불확실해서
    private const string GID_STUDENT_NAME          = "573141999"; // 학생 이름 목록
    private const string GID_STUDENT_BODY          = "537352849"; // 신장 및 체중
    private const string GID_STUDENT_STAT          = "230632196"; // 스탯 설명
    private const string GID_STUDENT_START_STAT    = "979106116"; // 초기스탯
    private const string GID_STUDENT_POTENTIAL     = "1193688286"; // 포지션 별 잠재능력
    private const string GID_STUDENT_STATUS_PROB   = "2004434082"; // 상태이상 확률
    private const string GID_STUDENT_STAT_EXP      = "1766696818"; // 스탯 경험치
    private const string GID_STUDENT_PLUS_EXP      = "1242881921"; // 추가 경험치
    private const string GID_STUDENT_POSITION      = "201406178"; // 포지션 별 생성 확률

    private const string GID_GRADUATE_GIFT_TIER_REWARD      = "1612068185";           // 졸업생의선물티어별보상
    private const string GID_FACILITY_UPGRADE               = "1179982310";           // 시설업그레이드
    private const string GID_COACH_NODE_MASTER              = "1141857137";           // 감독노드마스터테이블
    private const string GID_COACH_NODE_PREREQUISITE        = "1492463210";           // 감독노드선행조건테이블
    private const string GID_COACH_NODE_EFFECT_DETAIL       = "756317486";            // 감독노드효과상세테이블
    private const string GID_CONTENT_UNLOCK_FEATURE         = "1941271437";           // 컨텐츠 기능 해금 테이블
    private const string GID_TIER_MANAGE_OPEN_CONDITION     = "1252285184";           // 티어 관리 및 개방 조건 테이블
    private const string GID_GRADUATE_GRADE                 = "2023062391";           // 졸업생 등급
    private const string GID_GRADUATE_GIFT_POPUP            = "1794259775";           // 졸업생의 선물 팝업
    private const string GID_POSITION_INFO_POPUP            = "537180031";            // 포지션 정보 팝업

    private const string GID_WEEKEND_TRAINING                   = "370901189";      // 주말 훈련
    private const string GID_SUDDEN_EVENT_MSG_TEXT              = "1728993233";     // 돌발 이벤트 메시지 텍스트 테이블
    private const string GID_SUDDEN_EVENT_MSG                   = "1331535821";     // 돌발 이벤트 메시지 테이블
    private const string GID_SUDDEN_EVENT_MSG_LIST              = "863372423";      // 돌발 이벤트 메시지 목록 테이블
    private const string GID_SUDDEN_EVENT_MSG_SAVE_DATA         = "1170729418";     // 돌발 이벤트 메시지 세이브 데이터 테이블
    private const string GID_LOBBY_SUDDEN_EVENT_MSG_SAVE_DATA   = "2095264250";     // 로비 돌발 이벤트 메시지 세이브 데이터 테이블
    private const string GID_STORY                              = "1767584745";     // 스토리 데이터 테이블

    private const string GID_FRIENDLY_MATCH_SCHEDULE_MSG_TEXT   = "176960736";      // 친선경기 스케쥴 메시지 텍스트 테이블
    private const string GID_FRIENDLY_MATCH_SCHEDULE_MSG        = "123577051";      // 친선경기 스케쥴 메시지 테이블
    private const string GID_FRIENDLY_MATCH_SCHEDULE_MSG_LIST   = "1587989658";     // 친선경기 스케쥴 메시지 목록 테이블
    private const string GID_REWARD_POPUP                       = "435958384";      // 보상 팝업창
    private const string GID_HALFTIME_SELECT_TEXT               = "2038993277";     // 작전타임 선택지 텍스트 테이블
    private const string GID_HALFTIME_SELECT                    = "930760343";      // 작전타임 선택지 테이블


    public static readonly IReadOnlyList<SheetTableEntry> Tables = new List<SheetTableEntry>
    {
        new("GrowthCommandTable.csv",                   Url(GID_GROWTH_COMMAND)),
        new("AlwaysEffectTable.csv",                    Url(GID_ALWAYS_EFFECT)),
        new("AlwaysEventTable.csv",                     Url(GID_ALWAYS_EVENT)),
        new("SuddenEventTable.csv",                     Url(GID_SUDDEN_EVENT)),
        new("SuddenEventEffectTable.csv",               Url(GID_SUDDEN_EVENT_EFFECT)),
        new("SuddenEventTextTable.csv",                 Url(GID_SUDDEN_EVENT_TEXT)),
        new("StatusTextTable.csv",                      Url(GID_STATUS_TEXT)),                            // 돌발 이벤트 스탯 텍스트
        new("SchoolNameTable.csv",                      Url(GID_SCHOOL_NAME)),
        new("EnemyStatTable.csv",                       Url(GID_ENEMY_STAT)),
        new("TutorialGuideTable.csv",                   Url(GID_TUTORIAL_GUIDE)),
        new("StudentNameTable.csv",                     Url(GID_STUDENT_NAME)),
        new("StudentBodyTable.csv",                     Url(GID_STUDENT_BODY)),
        new("StudentStatTable.csv",                     Url(GID_STUDENT_STAT)),
        new("StudentStartStatTable.csv",                Url(GID_STUDENT_START_STAT)),
        new("StudentPotentialTable.csv",                Url(GID_STUDENT_POTENTIAL)),
        new("StudentStatusProbTable.csv",               Url(GID_STUDENT_STATUS_PROB)),
        new("StudentStatExpTable.csv",                  Url(GID_STUDENT_STAT_EXP)),
        new("StudentPlusExpTable.csv",                  Url(GID_STUDENT_PLUS_EXP)),
        new("StudentPositionTable.csv",                 Url(GID_STUDENT_POSITION)),

        new("GraduateGiftTierRewardTable.csv",          Url(GID_GRADUATE_GIFT_TIER_REWARD)),               // 졸업생의선물티어별보상
        new("FacilityUpgradeTable.csv",                 Url(GID_FACILITY_UPGRADE)),                        // 시설업그레이드
        new("CoachNodeMasterTable.csv",                 Url(GID_COACH_NODE_MASTER)),                       // 감독노드마스터테이블
        new("CoachNodePrerequisiteTable.csv",           Url(GID_COACH_NODE_PREREQUISITE)),                 // 감독노드선행조건테이블
        new("CoachNodeEffectDetailTable.csv",           Url(GID_COACH_NODE_EFFECT_DETAIL)),                // 감독노드효과상세테이블
        new("ContentUnlockFeatureTable.csv",            Url(GID_CONTENT_UNLOCK_FEATURE)),                  // 컨텐츠 기능 해금 테이블
        new("TierManageOpenConditionTable.csv",         Url(GID_TIER_MANAGE_OPEN_CONDITION)),              // 티어 관리 및 개방 조건 테이블
        new("GraduateGradeTable.csv",                   Url(GID_GRADUATE_GRADE)),                          // 졸업생 등급
        new("GraduateGiftPopupTable.csv",               Url(GID_GRADUATE_GIFT_POPUP)),                     // 졸업생의 선물 팝업
        new("PositionInfoPopupTable.csv",               Url(GID_POSITION_INFO_POPUP)),                     // 포지션 정보 팝업

        new("WeekendTrainingTable.csv",                 Url(GID_WEEKEND_TRAINING)),                        // 주말 훈련
        new("SuddenEventMsgTextTable.csv",              Url(GID_SUDDEN_EVENT_MSG_TEXT)),                   // 돌발 이벤트 메시지 텍스트 테이블
        new("SuddenEventMsgTable.csv",                  Url(GID_SUDDEN_EVENT_MSG)),                        // 돌발 이벤트 메시지 테이블
        new("SuddenEventMsgListTable.csv",              Url(GID_SUDDEN_EVENT_MSG_LIST)),                   // 돌발 이벤트 메시지 목록 테이블
        new("SuddenEventMsgSaveDataTable.csv",          Url(GID_SUDDEN_EVENT_MSG_SAVE_DATA)),              // 돌발 이벤트 메시지 세이브 데이터 테이블
        new("LobbySuddenEventMsgSaveDataTable.csv",     Url(GID_LOBBY_SUDDEN_EVENT_MSG_SAVE_DATA)),        // 로비 돌발 이벤트 메시지 세이브 데이터 테이블
        new("StoryTable.csv",                           Url(GID_STORY)),                                   // 스토리 데이터 테이블

        new("FriendlyMatchScheduleMsgTextTable.csv",    Url(GID_FRIENDLY_MATCH_SCHEDULE_MSG_TEXT)),         // 친선경기 스케쥴 메시지 텍스트 테이블
        new("FriendlyMatchScheduleMsgTable.csv",        Url(GID_FRIENDLY_MATCH_SCHEDULE_MSG)),              // 친선경기 스케쥴 메시지 테이블
        new("FriendlyMatchScheduleMsgListTable.csv",    Url(GID_FRIENDLY_MATCH_SCHEDULE_MSG_LIST)),         // 친선경기 스케쥴 메시지 목록 테이블
        new("RewardPopupTable.csv",                     Url(GID_REWARD_POPUP)),                             // 보상 팝업창
        new("HalftimeSelectTextTable.csv",              Url(GID_HALFTIME_SELECT_TEXT)),                     // 작전타임 선택지 텍스트 테이블
        new("HalftimeSelectTable.csv",                  Url(GID_HALFTIME_SELECT)),                          // 작전타임 선택지 테이블
    };
}

// CSV 파일명과 구글 시트 export URL을 묶는 테이블
public readonly struct SheetTableEntry
{
    public readonly string CsvFileName;
    public readonly string SheetUrl;

    public SheetTableEntry(string csvFileName, string sheetUrl)
    {
        CsvFileName = csvFileName;
        SheetUrl = sheetUrl;
    }
}
#endif
