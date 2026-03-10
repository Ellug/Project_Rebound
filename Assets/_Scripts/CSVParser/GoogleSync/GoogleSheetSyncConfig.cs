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

    private const string GID_STUDENT_TRUST_START                    = "1176049319"; // 학생신뢰도 시작값
    private const string GID_STUDENT_TRUST_LEVEL                    = "811608658";  // 학생신뢰도 단계 구분
    private const string GID_STUDENT_TRUST_BENEFIT                  = "2041083839";           // 학생신뢰도혜택
    private const string GID_STUDENT_TRUST_UP_CONDITION             = "1682193230";           // 학생신뢰도상승조건
    private const string GID_STUDENT_TRUST_DOWN_CONDITION_STATUS    = "1392944573";           // 학생신뢰도하락컨디션
    private const string GID_STUDENT_TRUST_DOWN_NEGLECT             = "1175187511";           // 학생신뢰도하락방치
    private const string GID_GRADUATE_GIFT_TIER_REWARD              = "1612068185";           // 졸업생의선물티어별보상
    private const string GID_FACILITY_UPGRADE                       = "1179982310";           // 시설업그레이드
    private const string GID_COACH_NODE_MASTER                      = "1141857137";           // 감독노드마스터테이블
    private const string GID_COACH_NODE_PREREQUISITE                = "1492463210";           // 감독노드선행조건테이블
    private const string GID_COACH_NODE_EFFECT_DETAIL               = "756317486";           // 감독노드효과상세테이블
    private const string GID_CONTENT_UNLOCK_FEATURE                 = "1941271437";           // 컨텐츠 기능 해금 테이블
    private const string GID_TIER_MANAGE_OPEN_CONDITION             = "1252285184";           // 티어 관리 및 개방 조건 테이블
    private const string GID_GRADUATE_GRADE                         = "2023062391";           // 졸업생 등급
    private const string GID_GRADUATE_GIFT_POPUP                    = "1794259775";           // 졸업생의 선물 팝업
    private const string GID_POSITION_INFO_POPUP                    = "537180031";           // 포지션 정보 팝업

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

        new("StudentTrustStartTable.csv",               Url(GID_STUDENT_TRUST_START)),                    // 학생신뢰도 시작값
        new("StudentTrustLevelTable.csv",               Url(GID_STUDENT_TRUST_LEVEL)),                    // 학생신뢰도 레벨
        new("StudentTrustBenefitTable.csv",             Url(GID_STUDENT_TRUST_BENEFIT)),                  // 학생신뢰도혜택
        new("StudentTrustUpConditionTable.csv",         Url(GID_STUDENT_TRUST_UP_CONDITION)),             // 학생신뢰도상승조건
        new("StudentTrustDownConditionStatusTable.csv", Url(GID_STUDENT_TRUST_DOWN_CONDITION_STATUS)),    // 학생신뢰도하락컨디션
        new("StudentTrustDownNeglectTable.csv",         Url(GID_STUDENT_TRUST_DOWN_NEGLECT)),             // 학생신뢰도하락방치
        new("GraduateGiftTierRewardTable.csv",          Url(GID_GRADUATE_GIFT_TIER_REWARD)),              // 졸업생의선물티어별보상
        new("FacilityUpgradeTable.csv",                 Url(GID_FACILITY_UPGRADE)),                        // 시설업그레이드
        new("CoachNodeMasterTable.csv",                 Url(GID_COACH_NODE_MASTER)),                       // 감독노드마스터테이블
        new("CoachNodePrerequisiteTable.csv",           Url(GID_COACH_NODE_PREREQUISITE)),                 // 감독노드선행조건테이블
        new("CoachNodeEffectDetailTable.csv",           Url(GID_COACH_NODE_EFFECT_DETAIL)),                // 감독노드효과상세테이블
        new("ContentUnlockFeatureTable.csv",            Url(GID_CONTENT_UNLOCK_FEATURE)),                  // 컨텐츠 기능 해금 테이블
        new("TierManageOpenConditionTable.csv",         Url(GID_TIER_MANAGE_OPEN_CONDITION)),              // 티어 관리 및 개방 조건 테이블
        new("GraduateGradeTable.csv",                   Url(GID_GRADUATE_GRADE)),                          // 졸업생 등급
        new("GraduateGiftPopupTable.csv",               Url(GID_GRADUATE_GIFT_POPUP)),                     // 졸업생의 선물 팝업
        new("PositionInfoPopupTable.csv",               Url(GID_POSITION_INFO_POPUP)),                     // 포지션 정보 팝업
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
