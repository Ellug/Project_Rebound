#if UNITY_EDITOR
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

public static class StudentStatExpTableCsvImporter
{
    [MenuItem("Tools/Data/Import Student Stat Exp CSV -> SO")]
    public static void Import()
    {
        var csvPath = EditorUtility.OpenFilePanel("Select Student Stat Exp CSV", Application.dataPath, "csv");
        if (string.IsNullOrEmpty(csvPath))
            return;

        ImportFromPath(csvPath);
    }

    public static void ImportFromPath(string csvPath)
    {
        const string assetPath = "Assets/_Scripts/SO/SO_StudentStatExpTable.asset";

        var csvText = File.ReadAllText(csvPath);
        var rows = ParseCsvToRows(csvText);

        var so = CsvImportUtil.LoadOrCreateSO<StudentStatExpTableSO>(assetPath);
        so.ReplaceAll(rows);

        EditorUtility.SetDirty(so);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log($"[StudentStatExpTable] Imported {rows.Count} rows -> {assetPath}");
    }

    private static List<StudentStatExpRow> ParseCsvToRows(string csv)
    {
        var lines = CsvImportUtil.SplitLines(csv);
        var result = new List<StudentStatExpRow>(Mathf.Max(16, lines.Count - 1));
        if (lines.Count <= 1) return result;

        var header = CsvImportUtil.SplitCsvLine(lines[0]);
        var col = CsvImportUtil.BuildColumnMap(header);

        int startRow = CsvImportUtil.GetDataStartRow(lines);

        for (int i = startRow; i < lines.Count; i++)
        {
            var line = lines[i];
            if (string.IsNullOrWhiteSpace(line)) continue;

            var cells = CsvImportUtil.SplitCsvLine(line);

            var r = new StudentStatExpRow
            {
                level = CsvImportUtil.ReadInt(cells, col, "level", 0),
                expNext = CsvImportUtil.ReadInt(cells, col, "exp_next", 0),
                expTotal = CsvImportUtil.ReadInt(cells, col, "exp_total", 0)
            };

            result.Add(r);
        }

        return result;
    }
}
#endif
