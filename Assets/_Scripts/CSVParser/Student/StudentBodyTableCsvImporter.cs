#if UNITY_EDITOR
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

public static class StudentBodyTableCsvImporter
{
    [MenuItem("Tools/Data/Import Student Body CSV -> SO")]
    public static void Import()
    {
        var csvPath = EditorUtility.OpenFilePanel("Select Student Body CSV", Application.dataPath, "csv");
        if (string.IsNullOrEmpty(csvPath))
            return;

        ImportFromPath(csvPath);
    }

    public static void ImportFromPath(string csvPath)
    {
        const string assetPath = "Assets/_Scripts/SO/SO_StudentBodyTable.asset";

        var csvText = File.ReadAllText(csvPath);
        var rows = ParseCsvToRows(csvText);

        var so = CsvImportUtil.LoadOrCreateSO<StudentBodyTableSO>(assetPath);
        so.ReplaceAll(rows);

        EditorUtility.SetDirty(so);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log($"[StudentBodyTable] Imported {rows.Count} rows -> {assetPath}");
    }

    private static List<StudentBodyRow> ParseCsvToRows(string csv)
    {
        var lines = CsvImportUtil.SplitLines(csv);
        var result = new List<StudentBodyRow>(Mathf.Max(16, lines.Count - 1));
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

            var positionId = CsvImportUtil.ReadInt(cells, col, "position_id", 0);
            if (positionId == 0) continue;

            var r = new StudentBodyRow
            {
                positionId = positionId,
                positionName = CsvImportUtil.ReadString(cells, col, "position_name"),
                minHeight = CsvImportUtil.ReadInt(cells, col, "min_height", 0),
                maxHeight = CsvImportUtil.ReadInt(cells, col, "max_height", 0),
                minWeight = CsvImportUtil.ReadInt(cells, col, "min_weight", 0),
                maxWeight = CsvImportUtil.ReadInt(cells, col, "max_weight", 0)
            };

            result.Add(r);
        }

        return result;
    }
}
#endif
