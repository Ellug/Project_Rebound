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

            var r = new SuddenEventEffectRow
            {
                id = id,
                type = ReadEffectType(CsvImportUtil.ReadString(cells, col, "type")),
                targetNameId = CsvImportUtil.ReadString(cells, col, "targetName"),
                targetStatMin = ReadPlayerStat(CsvImportUtil.ReadString(cells, col, "targetStat_min")),
                targetStatMax = ReadPlayerStat(CsvImportUtil.ReadString(cells, col, "targetStat_max")),
            };

            // max가 비어있으면 min으로 보정
            if (r.targetStatMax == PlayerStat.None)
                r.targetStatMax = r.targetStatMin;

            result.Add(r);
        }

        // ID 중복 경고
        var dup = new HashSet<string>();
        foreach (var row in result)
        {
            if (!dup.Add(row.id))
                Debug.LogWarning($"[SuddenEventEffectTable] Duplicate ID detected: {row.id}");
        }

        return result;
    }

    // ---- Domain parsing only ----
    private static SuddenEventEffectType ReadEffectType(string s)
    {
        s = (s ?? "").Trim();
        if (string.IsNullOrEmpty(s) || s == "-")
            return SuddenEventEffectType.None;

        var lower = s.ToLowerInvariant();

        // 문자열 입력도 허용
        if (lower == "none") return SuddenEventEffectType.None;
        if (lower == "fixed") return SuddenEventEffectType.Fixed;
        if (lower == "percent" || lower == "per_cent" || lower == "percenter" || lower == "percentage")
            return SuddenEventEffectType.Percent;

        // 숫자 입력
        if (int.TryParse(lower, out var iv))
        {
            return iv switch
            {
                0 => SuddenEventEffectType.None,
                1 => SuddenEventEffectType.Fixed,
                2 => SuddenEventEffectType.Percent,
                _ => SuddenEventEffectType.None
            };
        }

        return SuddenEventEffectType.None;
    }

    private static PlayerStat ReadPlayerStat(string s)
    {
        s = (s ?? "").Trim();
        if (string.IsNullOrEmpty(s) || s == "-") return PlayerStat.None;

        if (int.TryParse(s, out var iv))
            return (PlayerStat)iv;

        var lower = s.ToLowerInvariant();
        return lower switch
        {
            "mental" => PlayerStat.Mental,
            "shoot" => PlayerStat.Shoot,
            "speed" => PlayerStat.Speed,
            "jump" => PlayerStat.Jump,
            "vital" => PlayerStat.Vital,
            "condition" => PlayerStat.Condition,
            "money" => PlayerStat.Money,
            "fame" => PlayerStat.Fame,
            _ => PlayerStat.None
        };
    }
}
#endif
