#if UNITY_EDITOR
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

public static class TutorialGuideTableCsvImporter
{
    [MenuItem("Tools/Data/Import Tutorial Guide CSV -> SO")]
    public static void Import()
    {
        var csvPath = EditorUtility.OpenFilePanel("Select Tutorial Guide CSV", Application.dataPath, "csv");
        if (string.IsNullOrEmpty(csvPath))
            return;

        ImportFromPath(csvPath);
    }

    public static void ImportFromPath(string csvPath)
    {
        const string assetPath = "Assets/_Scripts/SO/SO_TutorialGuideTable.asset";

        var csvText = File.ReadAllText(csvPath);
        var rows = ParseCsvToRows(csvText);

        var so = CsvImportUtil.LoadOrCreateSO<TutorialGuideTableSO>(assetPath);
        so.ReplaceAll(rows);

        EditorUtility.SetDirty(so);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log($"[TutorialGuideTable] Imported {rows.Count} rows -> {assetPath}");
    }

    private static List<TutorialGuideRow> ParseCsvToRows(string csv)
    {
        var lines = CsvImportUtil.SplitLines(csv);
        var result = new List<TutorialGuideRow>(Mathf.Max(8, lines.Count - 1));
        if (lines.Count <= 1) return result;

        var header = CsvImportUtil.SplitCsvLine(lines[0]);
        var col = CsvImportUtil.BuildColumnMap(header);

        int startRow = CsvImportUtil.GetDataStartRow(lines);

        for (int i = startRow; i < lines.Count; i++)
        {
            var line = lines[i];
            if (string.IsNullOrWhiteSpace(line)) continue;

            var cells = CsvImportUtil.SplitCsvLine(line);

            var r = new TutorialGuideRow
            {
                index = CsvImportUtil.ReadInt(cells, col, "index", 0),
                img = CsvImportUtil.ReadString(cells, col, "img"),
                titleText = CsvImportUtil.ReadString(cells, col, "title_text"),
                desc = CsvImportUtil.ReadString(cells, col, "description"),
            };

            result.Add(r);
        }

        return result;
    }
}
#endif