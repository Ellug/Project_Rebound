#if UNITY_EDITOR
using System.Collections.Generic;

// 구글 시트 → CSV 동기화에 사용할 시트 ID와 각 테이블의 export URL을 정의하는 설정 클래스
public static class GoogleSheetSyncConfig
{
    // 스프레드시트 URL에서 /d/{ID}/edit 부분의 ID
    private const string SHEET_ID = "SHEET_ID_HERE";

    // 각 탭의 gid는 구글 시트에서 탭 클릭 시 URL 끝의 gid=숫자 부분으로 확인
    private const string GID_GROWTH_COMMAND        = "0";
    private const string GID_ALWAYS_EFFECT         = "0";
    private const string GID_ALWAYS_EVENT          = "0";
    private const string GID_SUDDEN_EVENT          = "0";
    private const string GID_SUDDEN_EVENT_EFFECT   = "0";
    private const string GID_SUDDEN_EVENT_TEXT     = "0";
    private const string GID_STATUS_TEXT           = "0";
    private const string GID_SCHOOL_NAME           = "0";
    private const string GID_ENEMY_STAT            = "0";
    private const string GID_STUDENT_NAME          = "0";
    private const string GID_STUDENT_BODY          = "0";
    private const string GID_STUDENT_STAT          = "0";
    private const string GID_STUDENT_START_STAT    = "0";
    private const string GID_STUDENT_POTENTIAL     = "0";
    private const string GID_STUDENT_STATUS_PROB   = "0";
    private const string GID_STUDENT_STAT_EXP      = "0";
    private const string GID_STUDENT_PLUS_EXP      = "0";
    private const string GID_STUDENT_POSITION      = "0";

    private static string Url(string gid) =>
        $"https://docs.google.com/spreadsheets/d/{SHEET_ID}/export?format=csv&gid={gid}";

    public static readonly IReadOnlyList<SheetTableEntry> Tables = new List<SheetTableEntry>(18)
    {
        new("GrowthCommandTable.csv",     "Growth Command",        Url(GID_GROWTH_COMMAND)),
        new("AlwaysEffectTable.csv",      "Always Effect",         Url(GID_ALWAYS_EFFECT)),
        new("AlwaysEventTable.csv",       "Always Event",          Url(GID_ALWAYS_EVENT)),
        new("SuddenEventTable.csv",       "Sudden Event",          Url(GID_SUDDEN_EVENT)),
        new("SuddenEventEffectTable.csv", "Sudden Event Effect",   Url(GID_SUDDEN_EVENT_EFFECT)),
        new("SuddenEventTextTable.csv",   "Sudden Event Text",     Url(GID_SUDDEN_EVENT_TEXT)),
        new("StatusTextTable.csv",        "Status Text",           Url(GID_STATUS_TEXT)),
        new("SchoolNameTable.csv",        "School Name",           Url(GID_SCHOOL_NAME)),
        new("EnemyStatTable.csv",         "Enemy Stat",            Url(GID_ENEMY_STAT)),
        new("StudentNameTable.csv",       "Student Name",          Url(GID_STUDENT_NAME)),
        new("StudentBodyTable.csv",       "Student Body",          Url(GID_STUDENT_BODY)),
        new("StudentStatTable.csv",       "Student Stat",          Url(GID_STUDENT_STAT)),
        new("StudentStartStatTable.csv",  "Student Start Stat",    Url(GID_STUDENT_START_STAT)),
        new("StudentPotentialTable.csv",  "Student Potential",     Url(GID_STUDENT_POTENTIAL)),
        new("StudentStatusProbTable.csv", "Student Status Prob",   Url(GID_STUDENT_STATUS_PROB)),
        new("StudentStatExpTable.csv",    "Student Stat Exp",      Url(GID_STUDENT_STAT_EXP)),
        new("StudentPlusExpTable.csv",    "Student Plus Exp",      Url(GID_STUDENT_PLUS_EXP)),
        new("StudentPositionTable.csv",   "Student Position",      Url(GID_STUDENT_POSITION)),
    };
}

// CSV 파일명, UI 표시명, 구글 시트 export URL을 묶는 테이블
public readonly struct SheetTableEntry
{
    public readonly string CsvFileName;
    public readonly string DisplayName;
    public readonly string SheetUrl;

    public SheetTableEntry(string csvFileName, string displayName, string sheetUrl)
    {
        CsvFileName = csvFileName;
        DisplayName = displayName;
        SheetUrl = sheetUrl;
    }
}
#endif
