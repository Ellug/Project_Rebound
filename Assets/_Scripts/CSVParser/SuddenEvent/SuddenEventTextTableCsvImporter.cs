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

            var r = new SuddenEventTextRow
            {
                id = id,
                range = CsvImportUtil.ReadInt(cells, col, "range", 0),
                speaker = CsvImportUtil.ReadString(cells, col, "speaker"),
                text = CsvImportUtil.ReadString(cells, col, "text"),
            };

            result.Add(r);
        }

        // (id, range) 중복 경고
        var dup = new HashSet<string>();
        var dupList = new List<string>();
        foreach (var row in result)
        {
            var key = $"{row.id}:{row.range}";
            if (!dup.Add(key))
                dupList.Add(key);
        }

        if (dupList.Count > 0)
        {
            var uniqueDups = new HashSet<string>(dupList);
            Debug.LogWarning($"[SuddenEventTextTable] Found {dupList.Count} duplicate keys ({uniqueDups.Count} unique): " +
                             string.Join(", ", uniqueDups));
        }

        return result;
    }
}
#endif
