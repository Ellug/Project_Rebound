#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using System.IO;
using System.Collections.Generic;

public static class SuddenEventTableCsvImporter
{
    // CSV 파일을 선택하여 SuddenEventTable ScriptableObject로 임포트
    [MenuItem("Tools/Data/Import SuddenEvent CSV -> SO")]
    public static void Import()
    {
        var csvPath = EditorUtility.OpenFilePanel("Select SuddenEvent CSV", Application.dataPath, "csv");
        if (string.IsNullOrEmpty(csvPath))
            return;

        ImportFromPath(csvPath);
    }

    public static void ImportFromPath(string csvPath)
    {
        const string assetPath = "Assets/_Scripts/SO/SO_SuddenEventTable.asset";

        var csvText = File.ReadAllText(csvPath);
        var rows = ParseCsvToRows(csvText);

        // SO에 데이터 저장
        var so = CsvImportUtil.LoadOrCreateSO<SuddenEventTableSO>(assetPath);
        so.ReplaceAll(rows);

        EditorUtility.SetDirty(so);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log($"[SuddenEventTable] Imported {rows.Count} rows -> {assetPath}");
    }

    // CSV 텍스트를 파싱하여 SuddenEventRow 리스트로 변환
    private static List<SuddenEventRow> ParseCsvToRows(string csv)
    {
        var lines = CsvImportUtil.SplitLines(csv);
        var result = new List<SuddenEventRow>(Mathf.Max(16, lines.Count - 1));
        if (lines.Count <= 1) return result;

        var header = CsvImportUtil.SplitCsvLine(lines[0]);
        var col = CsvImportUtil.BuildColumnMap(header);

        int startRow = CsvImportUtil.GetDataStartRow(lines);

        for (int i = startRow; i < lines.Count; i++)
        {
            var line = lines[i];
            if (string.IsNullOrWhiteSpace(line)) continue;

            var cells = CsvImportUtil.SplitCsvLine(line);

            var r = new SuddenEventRow
            {
                id = CsvImportUtil.ReadString(cells, col, "ID"),
                name = CsvImportUtil.ReadString(cells, col, "name"),

                context = CsvImportUtil.ReadFlags<SuddenEventContextFlags>(cells, col, "context"),
                condition = CsvImportUtil.ReadFlags<SuddenEventConditionFlags>(cells, col, "condition"),
                category = CsvImportUtil.ReadFlags<SuddenEventCategoryFlags>(cells, col, "category"),

                scope = CsvImportUtil.ReadEnumSingle(cells, col, "scope", SuddenEventScope.Member),
                targetMin = CsvImportUtil.ReadInt(cells, col, "target_min", 0),
                targetMax = CsvImportUtil.ReadInt(cells, col, "target_max", 0),

                termMin = CsvImportUtil.ReadInt(cells, col, "term_min", 1),
                termMax = CsvImportUtil.ReadInt(cells, col, "term_max", 1),
                termScale = CsvImportUtil.ReadEnumSingle(cells, col, "term_scale", SuddenEventTermScale.Day),

                isTrigger = string.Equals(CsvImportUtil.ReadString(cells, col, "is_trigger"), "TRUE", System.StringComparison.OrdinalIgnoreCase),
                triggerStatus1 = CsvImportUtil.ReadEnumSingle(cells, col, "trigger_status1", SuddenEventTriggerStatus.None),
                triggerCondition1 = CsvImportUtil.ReadEnumSingle(cells, col, "trigger_condition1", SuddenEventTriggerCondition.None),
                triggerThreshold1 = CsvImportUtil.ReadInt(cells, col, "trigger_threshold1", -1),
                triggerStatus2 = CsvImportUtil.ReadEnumSingle(cells, col, "trigger_status2", SuddenEventTriggerStatus.None),
                triggerCondition2 = CsvImportUtil.ReadEnumSingle(cells, col, "trigger_condition2", SuddenEventTriggerCondition.None),
                triggerThreshold2 = CsvImportUtil.ReadInt(cells, col, "trigger_threshold2", -1),

                effect1 = CsvImportUtil.ReadString(cells, col, "effect1"),
                effect2 = CsvImportUtil.ReadString(cells, col, "effect2"),
                effect3 = CsvImportUtil.ReadString(cells, col, "effect3"),

                isProbable = string.Equals(CsvImportUtil.ReadString(cells, col, "is_probable"), "TRUE", System.StringComparison.OrdinalIgnoreCase),
                probability = CsvImportUtil.ReadFloat(cells, col, "probability", 0f),
                description = CsvImportUtil.ReadString(cells, col, "description")
            };

            result.Add(r);
        }

        return result;
    }
}
#endif
