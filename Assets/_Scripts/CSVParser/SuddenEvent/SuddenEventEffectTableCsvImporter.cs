#if UNITY_EDITOR
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

public static class SuddenEventEffectTableCsvImporter
{
    [MenuItem("Tools/Data/Import SuddenEventEffectTable CSV -> SO")]
    public static void Import()
    {
        var csvPath = EditorUtility.OpenFilePanel("Select SuddenEventEffectTable CSV", Application.dataPath, "csv");
        if (string.IsNullOrEmpty(csvPath))
            return;

        ImportFromPath(csvPath);
    }

    public static void ImportFromPath(string csvPath)
    {
        const string assetPath = "Assets/_Scripts/SO/SO_SuddenEventEffectTable.asset";

        var csvText = File.ReadAllText(csvPath);
        var rows = ParseCsvToRows(csvText);

        var so = CsvImportUtil.LoadOrCreateSO<SuddenEventEffectTableSO>(assetPath);
        so.ReplaceAll(rows);

        EditorUtility.SetDirty(so);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log($"[SuddenEventEffectTable] Imported {rows.Count} rows -> {assetPath}");
    }

    private static List<SuddenEventEffectRow> ParseCsvToRows(string csv)
    {
        var lines = CsvImportUtil.SplitLines(csv);
        var result = new List<SuddenEventEffectRow>(Mathf.Max(16, lines.Count - 1));
        if (lines.Count <= 1) return result;

        var header = CsvImportUtil.SplitCsvLine(lines[0]);
        var col = CsvImportUtil.BuildColumnMap(header);

        int startRow = CsvImportUtil.GetDataStartRow(lines);

        for (int i = startRow; i < lines.Count; i++)
        {
            var line = lines[i];
            if (string.IsNullOrWhiteSpace(line)) continue;

            var cells = CsvImportUtil.SplitCsvLine(line);

            var r = new SuddenEventEffectRow
            {
                id = CsvImportUtil.ReadString(cells, col, "ID"),
                type = CsvImportUtil.ReadEnumSingle(cells, col, "type", SuddenEventEffectType.None),
                targetName = CsvImportUtil.ReadString(cells, col, "target_name"),
                targetMin = CsvImportUtil.ReadEnumSingle(cells, col, "target_min", PlayerStat.None),
                targetMax = CsvImportUtil.ReadEnumSingle(cells, col, "target_max", PlayerStat.None),
                amountMin = CsvImportUtil.ReadInt(cells, col, "amount_min", 0),
                amountMax = CsvImportUtil.ReadInt(cells, col, "amount_max", 0),
            };

            result.Add(r);
        }

        return result;
    }
}
#endif
