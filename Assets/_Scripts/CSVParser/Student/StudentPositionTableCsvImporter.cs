#if UNITY_EDITOR
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

public static class StudentPositionTableCsvImporter
{
    [MenuItem("Tools/Data/Import Student Position CSV -> SO")]
    public static void Import()
    {
        var csvPath = EditorUtility.OpenFilePanel("Select Student Position CSV", Application.dataPath, "csv");
        if (string.IsNullOrEmpty(csvPath))
            return;

        ImportFromPath(csvPath);
    }

    public static void ImportFromPath(string csvPath)
    {
        const string assetPath = "Assets/_Scripts/SO/SO_StudentPositionTable.asset";

        var csvText = File.ReadAllText(csvPath);
        var rows = ParseCsvToRows(csvText);

        var so = CsvImportUtil.LoadOrCreateSO<StudentPositionTableSO>(assetPath);
        so.ReplaceAll(rows);

        EditorUtility.SetDirty(so);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log($"[StudentPositionTable] Imported {rows.Count} rows -> {assetPath}");
    }

    private static List<StudentPositionRow> ParseCsvToRows(string csv)
    {
        var lines = CsvImportUtil.SplitLines(csv);
        var result = new List<StudentPositionRow>(Mathf.Max(16, lines.Count - 1));
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

            var id = CsvImportUtil.ReadInt(cells, col, "id", 0);
            if (id == 0) continue;

            var r = new StudentPositionRow
            {
                id = id,
                positionName = CsvImportUtil.ReadString(cells, col, "position_name"),
                spawnRate = CsvImportUtil.ReadInt(cells, col, "spawn_rate", 0),
                mainStats = CsvImportUtil.ReadString(cells, col, "main_stats"),
                subStats = CsvImportUtil.ReadString(cells, col, "sub_stats"),
                designIntent = CsvImportUtil.ReadString(cells, col, "design_intent")
            };

            result.Add(r);
        }

        return result;
    }
}
#endif
