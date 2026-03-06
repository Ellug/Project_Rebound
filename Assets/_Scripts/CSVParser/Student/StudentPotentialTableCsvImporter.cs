#if UNITY_EDITOR
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

public static class StudentPotentialTableCsvImporter
{
    [MenuItem("Tools/Data/Import Student Potential CSV -> SO")]
    public static void Import()
    {
        var csvPath = EditorUtility.OpenFilePanel("Select Student Potential CSV", Application.dataPath, "csv");
        if (string.IsNullOrEmpty(csvPath))
            return;

        ImportFromPath(csvPath);
    }

    public static void ImportFromPath(string csvPath)
    {
        const string assetPath = "Assets/_Scripts/SO/SO_StudentPotentialTable.asset";

        var csvText = File.ReadAllText(csvPath);
        var rows = ParseCsvToRows(csvText);

        var so = CsvImportUtil.LoadOrCreateSO<StudentPotentialTableSO>(assetPath);
        so.ReplaceAll(rows);

        EditorUtility.SetDirty(so);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log($"[StudentPotentialTable] Imported {rows.Count} rows -> {assetPath}");
    }

    private static List<StudentPotentialRow> ParseCsvToRows(string csv)
    {
        var lines = CsvImportUtil.SplitLines(csv);
        var result = new List<StudentPotentialRow>(Mathf.Max(16, lines.Count - 1));
        if (lines.Count <= 1) return result;

        var header = CsvImportUtil.SplitCsvLine(lines[0]);
        var col = CsvImportUtil.BuildColumnMap(header);

        int startRow = CsvImportUtil.GetDataStartRow(lines);

        for (int i = startRow; i < lines.Count; i++)
        {
            var line = lines[i];
            if (string.IsNullOrWhiteSpace(line)) continue;

            var cells = CsvImportUtil.SplitCsvLine(line);

            var r = new StudentPotentialRow
            {
                id = CsvImportUtil.ReadString(cells, col, "id"),
                positionName = CsvImportUtil.ReadString(cells, col, "position_name"),
                tier1Stat = CsvImportUtil.ReadString(cells, col, "tier1_stat"),
                tier1Prob = CsvImportUtil.ReadInt(cells, col, "tier1_prob", 0),
                tier2Stat = CsvImportUtil.ReadString(cells, col, "tier2_stat"),
                tier2Prob = CsvImportUtil.ReadInt(cells, col, "tier2_prob", 0),
                tier3Stat = CsvImportUtil.ReadString(cells, col, "tier3_stat"),
                tier3Prob = CsvImportUtil.ReadInt(cells, col, "tier3_prob", 0)
            };

            result.Add(r);
        }

        return result;
    }
}
#endif
