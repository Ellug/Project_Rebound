#if UNITY_EDITOR
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

public static class StatusTextTableCsvImporter
{
    [MenuItem("Tools/Data/Import StatusTextTable CSV -> SO")]
    public static void Import()
    {
        var csvPath = EditorUtility.OpenFilePanel("Select StatusTextTable CSV", Application.dataPath, "csv");
        if (string.IsNullOrEmpty(csvPath))
            return;

        ImportFromPath(csvPath);
    }

    public static void ImportFromPath(string csvPath)
    {
        const string assetPath = "Assets/_Scripts/SO/SO_StatusTextTable.asset";

        var csvText = File.ReadAllText(csvPath);
        var rows = ParseCsvToRows(csvText);

        var so = CsvImportUtil.LoadOrCreateSO<StatusTextTableSO>(assetPath);
        so.ReplaceAll(rows);

        EditorUtility.SetDirty(so);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log($"[StatusTextTable] Imported {rows.Count} rows -> {assetPath}");
    }

    private static List<StatusTextRow> ParseCsvToRows(string csv)
    {
        var lines = CsvImportUtil.SplitLines(csv);
        var result = new List<StatusTextRow>(Mathf.Max(16, lines.Count - 1));
        if (lines.Count <= 1) return result;

        var header = CsvImportUtil.SplitCsvLine(lines[0]);
        var col = CsvImportUtil.BuildColumnMap(header);

        // 2번째 행이 자료형 정의면 스킵
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

            var r = new StatusTextRow
            {
                id = id,
                index = CsvImportUtil.ReadInt(cells, col, "Index", 0),
                text = CsvImportUtil.ReadString(cells, col, "Text"),
                description = CsvImportUtil.ReadString(cells, col, "Description"),
            };

            result.Add(r);
        }

        // (id, index) 중복 경고
        var dup = new HashSet<string>();
        var dupList = new List<string>();
        foreach (var row in result)
        {
            var key = $"{row.id}:{row.index}";
            if (!dup.Add(key))
                dupList.Add(key);
        }

        if (dupList.Count > 0)
        {
            var uniqueDups = new HashSet<string>(dupList);
            Debug.LogWarning($"[StatusTextTable] Found {dupList.Count} duplicate keys ({uniqueDups.Count} unique): " +
                             string.Join(", ", uniqueDups));
        }

        return result;
    }
}
#endif
