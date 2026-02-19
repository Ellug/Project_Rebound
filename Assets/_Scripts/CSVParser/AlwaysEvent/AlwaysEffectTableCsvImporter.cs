#if UNITY_EDITOR
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

public static class AlwaysEffectTableCsvImporter
{
    [MenuItem("Tools/Data/Import AlwaysEffectTable CSV -> SO")]
    public static void Import()
    {
        var csvPath = EditorUtility.OpenFilePanel("Select AlwaysEffectTable CSV", Application.dataPath, "csv");
        if (string.IsNullOrEmpty(csvPath))
            return;

        ImportFromPath(csvPath);
    }

    public static void ImportFromPath(string csvPath)
    {
        const string assetPath = "Assets/_Scripts/SO/SO_AlwaysEffectTable.asset";

        var csvText = File.ReadAllText(csvPath);
        var rows = ParseCsvToRows(csvText);

        var so = CsvImportUtil.LoadOrCreateSO<AlwaysEffectTableSO>(assetPath);
        so.ReplaceAll(rows);

        EditorUtility.SetDirty(so);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log($"[AlwaysEffectTable] Imported {rows.Count} rows -> {assetPath}");
    }

    private static List<AlwaysEffectRow> ParseCsvToRows(string csv)
    {
        var lines = CsvImportUtil.SplitLines(csv);
        var result = new List<AlwaysEffectRow>(Mathf.Max(16, lines.Count - 1));
        if (lines.Count <= 1) return result;

        var header = CsvImportUtil.SplitCsvLine(lines[0]);
        var col = CsvImportUtil.BuildColumnMap(header);

        int startRow = 1;
        if (lines.Count > 1)
        {
            var secondRow = CsvImportUtil.SplitCsvLine(lines[1]);
            if (CsvImportUtil.IsTypeDefinitionRow(secondRow))
                startRow = 2;
        }

        for (int i = startRow; i < lines.Count; i++)
        {
            var line = lines[i];
            if (string.IsNullOrWhiteSpace(line)) continue;

            var cells = CsvImportUtil.SplitCsvLine(line);

            var id = CsvImportUtil.ReadString(cells, col, "ID");
            if (string.IsNullOrEmpty(id)) continue;

            var r = new AlwaysEffectRow
            {
                id = id,
                trainingState = ReadBool(CsvImportUtil.ReadString(cells, col, "training_state")),
                conditionIncrease = CsvImportUtil.ReadInt(cells, col, "condition_increase", 0),
                conditionDecline = CsvImportUtil.ReadInt(cells, col, "condition_decline", 0),
                conditionRecoveryUp = CsvImportUtil.ReadInt(cells, col, "condition_recovery_up", 0),
                conditionRecoveryDown = CsvImportUtil.ReadInt(cells, col, "condition_recovery_down", 0),
                trainingIncrease = CsvImportUtil.ReadFloat(cells, col, "training_increase", 0f),
                trainingDecline = CsvImportUtil.ReadFloat(cells, col, "training_decline", 0f)
            };

            result.Add(r);
        }

        // ID 중복 경고
        var dup = new HashSet<string>();
        foreach (var row in result)
        {
            if (!dup.Add(row.id))
                Debug.LogWarning($"[AlwaysEffectTable] Duplicate ID detected: {row.id}");
        }

        return result;
    }

    private static bool ReadBool(string s)
    {
        s = (s ?? "").Trim().ToLowerInvariant();
        return s == "true" || s == "1" || s == "yes";
    }
}
#endif
