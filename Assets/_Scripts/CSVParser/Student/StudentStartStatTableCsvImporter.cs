#if UNITY_EDITOR
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

public static class StudentStartStatTableCsvImporter
{
    [MenuItem("Tools/Data/Import Student Start Stat CSV -> SO")]
    public static void Import()
    {
        var csvPath = EditorUtility.OpenFilePanel("Select Student Start Stat CSV", Application.dataPath, "csv");
        if (string.IsNullOrEmpty(csvPath))
            return;

        ImportFromPath(csvPath);
    }

    public static void ImportFromPath(string csvPath)
    {
        const string assetPath = "Assets/_Scripts/SO/SO_StudentStartStatTable.asset";

        var csvText = File.ReadAllText(csvPath);
        var rows = ParseCsvToRows(csvText);

        var so = CsvImportUtil.LoadOrCreateSO<StudentStartStatTableSO>(assetPath);
        so.ReplaceAll(rows);

        EditorUtility.SetDirty(so);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log($"[StudentStartStatTable] Imported {rows.Count} rows -> {assetPath}");
    }

    private static List<StudentStartStatRow> ParseCsvToRows(string csv)
    {
        var lines = CsvImportUtil.SplitLines(csv);
        var result = new List<StudentStartStatRow>(Mathf.Max(16, lines.Count - 1));
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

            var statId = CsvImportUtil.ReadInt(cells, col, "stat_id", 0);
            if (statId == 0) continue;

            var r = new StudentStartStatRow
            {
                statId = statId,
                statName = CsvImportUtil.ReadString(cells, col, "stat_name"),
                grade = CsvImportUtil.ReadInt(cells, col, "grade", 0),
                statMin = CsvImportUtil.ReadInt(cells, col, "stat_min", 0),
                statMax = CsvImportUtil.ReadInt(cells, col, "stat_max", 0),
                baseMin = CsvImportUtil.ReadInt(cells, col, "base_min", 0),
                baseMax = CsvImportUtil.ReadInt(cells, col, "base_max", 0)
            };

            result.Add(r);
        }

        return result;
    }
}
#endif
