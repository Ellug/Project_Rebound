#if UNITY_EDITOR
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

public static class StudentStatTableCsvImporter
{
    [MenuItem("Tools/Data/Import Student Stat CSV -> SO")]
    public static void Import()
    {
        var csvPath = EditorUtility.OpenFilePanel("Select Student Stat CSV", Application.dataPath, "csv");
        if (string.IsNullOrEmpty(csvPath))
            return;

        ImportFromPath(csvPath);
    }

    public static void ImportFromPath(string csvPath)
    {
        const string assetPath = "Assets/_Scripts/SO/SO_StudentStatTable.asset";

        var csvText = File.ReadAllText(csvPath);
        var rows = ParseCsvToRows(csvText);

        var so = CsvImportUtil.LoadOrCreateSO<StudentStatTableSO>(assetPath);
        so.ReplaceAll(rows);

        EditorUtility.SetDirty(so);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log($"[StudentStatTable] Imported {rows.Count} rows -> {assetPath}");
    }

    private static List<StudentStatRow> ParseCsvToRows(string csv)
    {
        var lines = CsvImportUtil.SplitLines(csv);
        var result = new List<StudentStatRow>(Mathf.Max(16, lines.Count - 1));
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

            var r = new StudentStatRow
            {
                statId = statId,
                statName = CsvImportUtil.ReadString(cells, col, "stat_name"),
                roleDesc = CsvImportUtil.ReadString(cells, col, "role_desc"),
                logicEffect = CsvImportUtil.ReadString(cells, col, "logic_effect")
            };

            result.Add(r);
        }

        return result;
    }
}
#endif
