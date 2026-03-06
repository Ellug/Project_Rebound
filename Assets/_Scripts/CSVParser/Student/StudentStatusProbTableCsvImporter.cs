#if UNITY_EDITOR
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

public static class StudentStatusProbTableCsvImporter
{
    [MenuItem("Tools/Data/Import Student Status Prob CSV -> SO")]
    public static void Import()
    {
        var csvPath = EditorUtility.OpenFilePanel("Select Student Status Prob CSV", Application.dataPath, "csv");
        if (string.IsNullOrEmpty(csvPath))
            return;

        ImportFromPath(csvPath);
    }

    public static void ImportFromPath(string csvPath)
    {
        const string assetPath = "Assets/_Scripts/SO/SO_StudentStatusProbTable.asset";

        var csvText = File.ReadAllText(csvPath);
        var rows = ParseCsvToRows(csvText);

        var so = CsvImportUtil.LoadOrCreateSO<StudentStatusProbTableSO>(assetPath);
        so.ReplaceAll(rows);

        EditorUtility.SetDirty(so);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log($"[StudentStatusProbTable] Imported {rows.Count} rows -> {assetPath}");
    }

    private static List<StudentStatusProbRow> ParseCsvToRows(string csv)
    {
        var lines = CsvImportUtil.SplitLines(csv);
        var result = new List<StudentStatusProbRow>(Mathf.Max(16, lines.Count - 1));
        if (lines.Count <= 1) return result;

        var header = CsvImportUtil.SplitCsvLine(lines[0]);
        var col = CsvImportUtil.BuildColumnMap(header);

        int startRow = CsvImportUtil.GetDataStartRow(lines);

        for (int i = startRow; i < lines.Count; i++)
        {
            var line = lines[i];
            if (string.IsNullOrWhiteSpace(line)) continue;

            var cells = CsvImportUtil.SplitCsvLine(line);

            var r = new StudentStatusProbRow
            {
                isInsane = string.Equals(CsvImportUtil.ReadString(cells, col, "is_insane"), "TRUE", System.StringComparison.OrdinalIgnoreCase),
                conditionMax = CsvImportUtil.ReadInt(cells, col, "condition_max", 0),
                probInsanity = CsvImportUtil.ReadInt(cells, col, "prob_insanity", 0),
                probInjury = CsvImportUtil.ReadInt(cells, col, "prob_injury", 0),
                probDisease = CsvImportUtil.ReadInt(cells, col, "prob_disease", 0),
                totalRisk = CsvImportUtil.ReadInt(cells, col, "total_risk", 0)
            };

            result.Add(r);
        }

        return result;
    }
}
#endif
