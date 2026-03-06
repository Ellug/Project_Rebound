#if UNITY_EDITOR
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

public static class SuddenEventTextTableCsvImporter
{
    [MenuItem("Tools/Data/Import SuddenEventTextTable CSV -> SO")]
    public static void Import()
    {
        var csvPath = EditorUtility.OpenFilePanel("Select SuddenEventTextTable CSV", Application.dataPath, "csv");
        if (string.IsNullOrEmpty(csvPath))
            return;

        ImportFromPath(csvPath);
    }

    public static void ImportFromPath(string csvPath)
    {
        const string assetPath = "Assets/_Scripts/SO/SO_SuddenEventTextTable.asset";

        var csvText = File.ReadAllText(csvPath);
        var rows = ParseCsvToRows(csvText);

        var so = CsvImportUtil.LoadOrCreateSO<SuddenEventTextTableSO>(assetPath);
        so.ReplaceAll(rows);

        EditorUtility.SetDirty(so);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log($"[SuddenEventTextTable] Imported {rows.Count} rows -> {assetPath}");
    }

    private static List<SuddenEventTextRow> ParseCsvToRows(string csv)
    {
        var lines = CsvImportUtil.SplitLines(csv);
        var result = new List<SuddenEventTextRow>(Mathf.Max(16, lines.Count - 1));
        if (lines.Count <= 1) return result;

        // 첫 줄을 헤더로 고정
        var header = CsvImportUtil.SplitCsvLine(lines[0]);
        var col = CsvImportUtil.BuildColumnMap(header);

        int startRow = CsvImportUtil.GetDataStartRow(lines);

        for (int i = startRow; i < lines.Count; i++)
        {
            var line = lines[i];
            if (string.IsNullOrWhiteSpace(line)) continue;

            var cells = CsvImportUtil.SplitCsvLine(line);

            var r = new SuddenEventTextRow
            {
                id = CsvImportUtil.ReadString(cells, col, "ID"),
                target = CsvImportUtil.ReadInt(cells, col, "target", 0),
                speaker = CsvImportUtil.ReadString(cells, col, "speaker"),
                description = CsvImportUtil.ReadString(cells, col, "description"),
            };

            result.Add(r);
        }

        return result;
    }
}
#endif
