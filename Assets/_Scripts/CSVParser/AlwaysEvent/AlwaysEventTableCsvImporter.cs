#if UNITY_EDITOR
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

public static class AlwaysEventTableCsvImporter
{
    [MenuItem("Tools/Data/Import AlwaysEventTable CSV -> SO")]
    public static void Import()
    {
        var csvPath = EditorUtility.OpenFilePanel("Select AlwaysEventTable CSV", Application.dataPath, "csv");
        if (string.IsNullOrEmpty(csvPath))
            return;

        ImportFromPath(csvPath);
    }

    public static void ImportFromPath(string csvPath)
    {
        const string assetPath = "Assets/_Scripts/SO/SO_AlwaysEventTable.asset";

        var csvText = File.ReadAllText(csvPath);
        var rows = ParseCsvToRows(csvText);

        var so = CsvImportUtil.LoadOrCreateSO<AlwaysEventTableSO>(assetPath);
        so.ReplaceAll(rows);

        EditorUtility.SetDirty(so);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log($"[AlwaysEventTable] Imported {rows.Count} rows -> {assetPath}");
    }

    private static List<AlwaysEventRow> ParseCsvToRows(string csv)
    {
        var lines = CsvImportUtil.SplitLines(csv);
        var result = new List<AlwaysEventRow>(Mathf.Max(16, lines.Count - 1));
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

            var id = CsvImportUtil.ReadString(cells, col, "ID");
            if (string.IsNullOrEmpty(id)) continue;

            var r = new AlwaysEventRow
            {
                id = id,
                name = CsvImportUtil.ReadString(cells, col, "name"),
                type = CsvImportUtil.ReadString(cells, col, "type"),
                termStart = CsvImportUtil.ReadString(cells, col, "term_start"),
                termEnd = CsvImportUtil.ReadString(cells, col, "term_end"),
                term = CsvImportUtil.ReadInt(cells, col, "term", 0),
                priority = CsvImportUtil.ReadInt(cells, col, "priority", 0),
                range = CsvImportUtil.ReadInt(cells, col, "range", 0),
                effectId = CsvImportUtil.ReadString(cells, col, "effect_id"),
                description = CsvImportUtil.ReadString(cells, col, "description")
            };

            result.Add(r);
        }

        // ID 중복 경고
        var dup = new HashSet<string>();
        foreach (var row in result)
        {
            if (!dup.Add(row.id))
                Debug.LogWarning($"[AlwaysEventTable] Duplicate ID detected: {row.id}");
        }

        return result;
    }
}
#endif
