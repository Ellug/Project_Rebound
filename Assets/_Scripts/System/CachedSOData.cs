public static class CachedSOData
{
    // Loaded Tables
    private static GrowthCommandTableSO _growthCommandTable;
    private static AlwaysEffectTableSO _alwaysEffectTable;
    private static AlwaysEventTableSO _alwaysEventTable;
    private static SuddenEventTableSO _suddenEventTable;
    private static SuddenEventEffectTableSO _suddenEventEffectTable;
    private static SuddenEventTextTableSO _suddenEventTextTable;
    private static StatusTextTableSO _statusTextTable;
    private static SchoolNameTableSO _schoolNameTable;
    private static StudentNameTableSO _studentNameTable;
    private static StudentBodyTableSO _studentBodyTable;
    private static StudentStatTableSO _studentStatTable;
    private static StudentStartStatTableSO _studentStartStatTable;
    private static StudentPotentialTableSO _studentPotentialTable;
    private static StudentStatusProbTableSO _studentStatusProbTable;
    private static StudentStatExpTableSO _studentStatExpTable;
    private static StudentPlusExpTableSO _studentPlusExpTable;
    private static StudentPositionTableSO _studentPositionTable;

    // Properties
    public static GrowthCommandTableSO GrowthCommandTable => _growthCommandTable;
    public static AlwaysEffectTableSO AlwaysEffectTable => _alwaysEffectTable;
    public static AlwaysEventTableSO AlwaysEventTable => _alwaysEventTable;
    public static SuddenEventTableSO SuddenEventTable => _suddenEventTable;
    public static SuddenEventEffectTableSO SuddenEventEffectTable => _suddenEventEffectTable;
    public static SuddenEventTextTableSO SuddenEventTextTable => _suddenEventTextTable;
    public static StatusTextTableSO StatusTextTable => _statusTextTable;
    public static SchoolNameTableSO SchoolNameTable => _schoolNameTable;
    public static StudentNameTableSO StudentNameTable => _studentNameTable;
    public static StudentBodyTableSO StudentBodyTable => _studentBodyTable;
    public static StudentStatTableSO StudentStatTable => _studentStatTable;
    public static StudentStartStatTableSO StudentStartStatTable => _studentStartStatTable;
    public static StudentPotentialTableSO StudentPotentialTable => _studentPotentialTable;
    public static StudentStatusProbTableSO StudentStatusProbTable => _studentStatusProbTable;
    public static StudentStatExpTableSO StudentStatExpTable => _studentStatExpTable;
    public static StudentPlusExpTableSO StudentPlusExpTable => _studentPlusExpTable;
    public static StudentPositionTableSO StudentPositionTable => _studentPositionTable;

    // StartManager에서 로드된 테이블을 등록
    public static void RegisterTables(
        GrowthCommandTableSO growthCommand,
        AlwaysEffectTableSO alwaysEffect,
        AlwaysEventTableSO alwaysEvent,
        SuddenEventTableSO suddenEvent,
        SuddenEventEffectTableSO suddenEventEffect,
        SuddenEventTextTableSO suddenEventText,
        StatusTextTableSO statusText,
        SchoolNameTableSO schoolName,
        StudentNameTableSO studentName,
        StudentBodyTableSO studentBody,
        StudentStatTableSO studentStat,
        StudentStartStatTableSO studentStartStat,
        StudentPotentialTableSO studentPotential,
        StudentStatusProbTableSO studentStatusProb,
        StudentStatExpTableSO studentStatExp,
        StudentPlusExpTableSO studentPlusExp,
        StudentPositionTableSO studentPosition)
    {
        _growthCommandTable = growthCommand;
        _alwaysEffectTable = alwaysEffect;
        _alwaysEventTable = alwaysEvent;
        _suddenEventTable = suddenEvent;
        _suddenEventEffectTable = suddenEventEffect;
        _suddenEventTextTable = suddenEventText;
        _statusTextTable = statusText;
        _schoolNameTable = schoolName;
        _studentNameTable = studentName;
        _studentBodyTable = studentBody;
        _studentStatTable = studentStat;
        _studentStartStatTable = studentStartStat;
        _studentPotentialTable = studentPotential;
        _studentStatusProbTable = studentStatusProb;
        _studentStatExpTable = studentStatExp;
        _studentPlusExpTable = studentPlusExp;
        _studentPositionTable = studentPosition;
    }

    // 모든 테이블 해제
    public static void Clear()
    {
        _growthCommandTable = null;
        _alwaysEffectTable = null;
        _alwaysEventTable = null;
        _suddenEventTable = null;
        _suddenEventEffectTable = null;
        _suddenEventTextTable = null;
        _statusTextTable = null;
        _schoolNameTable = null;
        _studentNameTable = null;
        _studentBodyTable = null;
        _studentStatTable = null;
        _studentStartStatTable = null;
        _studentPotentialTable = null;
        _studentStatusProbTable = null;
        _studentStatExpTable = null;
        _studentPlusExpTable = null;
        _studentPositionTable = null;
    }
}
