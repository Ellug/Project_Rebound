using UnityEngine;

public static class CachedSOData
{
    // Loaded Tables
    private static GrowthCommandTableSO _growthCommandTable;
    private static SuddenEventTableSO _suddenEventTable;
    private static SuddenEventEffectTableSO _suddenEventEffectTable;
    private static SuddenEventTextTableSO _suddenEventTextTable;
    private static StatusTextTableSO _statusTextTable;

    // Properties
    public static GrowthCommandTableSO GrowthCommandTable => _growthCommandTable;
    public static SuddenEventTableSO SuddenEventTable => _suddenEventTable;
    public static SuddenEventEffectTableSO SuddenEventEffectTable => _suddenEventEffectTable;
    public static SuddenEventTextTableSO SuddenEventTextTable => _suddenEventTextTable;
    public static StatusTextTableSO StatusTextTable => _statusTextTable;

    // StartManager에서 로드된 테이블을 등록
    public static void RegisterTables(
        GrowthCommandTableSO growthCommand,
        SuddenEventTableSO suddenEvent,
        SuddenEventEffectTableSO suddenEventEffect,
        SuddenEventTextTableSO suddenEventText,
        StatusTextTableSO statusText)
    {
        _growthCommandTable = growthCommand;
        _suddenEventTable = suddenEvent;
        _suddenEventEffectTable = suddenEventEffect;
        _suddenEventTextTable = suddenEventText;
        _statusTextTable = statusText;
    }

    // 모든 테이블 해제
    public static void Clear()
    {
        _growthCommandTable = null;
        _suddenEventTable = null;
        _suddenEventEffectTable = null;
        _suddenEventTextTable = null;
        _statusTextTable = null;
    }
}
