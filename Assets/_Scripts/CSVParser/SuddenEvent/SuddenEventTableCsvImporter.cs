#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using System.IO;
using System.Collections.Generic;

public static class SuddenEventTableCsvImporter
{
    // CSV 파일을 선택하여 SuddenEventTable ScriptableObject로 임포트
    [MenuItem("Tools/Data/Import SuddenEvent CSV -> SO")]
    public static void Import()
    {
        var csvPath = EditorUtility.OpenFilePanel("Select SuddenEvent CSV", Application.dataPath, "csv");
        if (string.IsNullOrEmpty(csvPath))
            return;

        ImportFromPath(csvPath);
    }

    public static void ImportFromPath(string csvPath)
    {
        const string assetPath = "Assets/_Scripts/SO/SO_SuddenEventTable.asset";

        var csvText = File.ReadAllText(csvPath);
        var rows = ParseCsvToRows(csvText);

        // SO에 데이터 저장
        var so = CsvImportUtil.LoadOrCreateSO<SuddenEventTableSO>(assetPath);
        so.ReplaceAll(rows);

        EditorUtility.SetDirty(so);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log($"[SuddenEventTable] Imported {rows.Count} rows -> {assetPath}");
    }

    // CSV 텍스트를 파싱하여 SuddenEventRow 리스트로 변환
    private static List<SuddenEventRow> ParseCsvToRows(string csv)
    {
        var lines = CsvImportUtil.SplitLines(csv);
        var result = new List<SuddenEventRow>(Mathf.Max(16, lines.Count - 1));
        if (lines.Count <= 1) return result;

        var header = CsvImportUtil.SplitCsvLine(lines[0]);
        var col = CsvImportUtil.BuildColumnMap(header);

        // 각 행을 SuddenEventRow로 변환 (2번째 행이 자료형 정의면 스킵)
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

            var eventName = CsvImportUtil.ReadString(cells, col, "name");
            if (string.IsNullOrEmpty(eventName))
                eventName = CsvImportUtil.ReadString(cells, col, "eventName");

            var description = CsvImportUtil.ReadString(cells, col, "description");
            if (string.IsNullOrEmpty(description))
                description = CsvImportUtil.ReadString(cells, col, "message");

            var r = new SuddenEventRow
            {
                id = id,
                name = eventName,

                context = CsvImportUtil.ReadFlags<SuddenEventContextFlags>(cells, col, "context"),
                condition = CsvImportUtil.ReadFlags<SuddenEventConditionFlags>(cells, col, "condition"),

                scope = CsvImportUtil.ReadEnumSingle(cells, col, "scope", SuddenEventScope.Member),

                termMin = CsvImportUtil.ReadInt(cells, col, "term_min", 1),
                termMax = CsvImportUtil.ReadInt(cells, col, "term_max", 1),
                termScale = CsvImportUtil.ReadEnumSingle(cells, col, "term_scale", SuddenEventTermScale.Day),

                effect1 = CsvImportUtil.ReadString(cells, col, "effect1"),
                effect2 = CsvImportUtil.ReadString(cells, col, "effect2"),
                effect3 = CsvImportUtil.ReadString(cells, col, "effect3"),

                probability = CsvImportUtil.ReadFloat(cells, col, "probability", 0f),
                description = description
            };

            // 최신 CSV는 target_min/target_max를 사용. 구버전 range_* 컬럼은 하위호환으로 유지.
            if (col.ContainsKey("target_min") || col.ContainsKey("target_max"))
            {
                r.targetMin = CsvImportUtil.ReadInt(cells, col, "target_min", 0);
                r.targetMax = CsvImportUtil.ReadInt(cells, col, "target_max", r.targetMin);
            }
            else if (col.ContainsKey("range_min") || col.ContainsKey("range_max"))
            {
                r.targetMin = CsvImportUtil.ReadInt(cells, col, "range_min", 0);
                r.targetMax = CsvImportUtil.ReadInt(cells, col, "range_max", r.targetMin);
            }
            else if (col.ContainsKey("range"))
            {
                var v = CsvImportUtil.ReadInt(cells, col, "range", 0);
                r.targetMin = v;
                r.targetMax = v;
            }
            else
            {
                r.targetMin = 0;
                r.targetMax = 0;
            }

            // min/max 보정 (csv 기입 에러시 시스템상 에러 방지 용도)
            if (r.targetMax < r.targetMin) r.targetMax = r.targetMin;
            if (r.termMax < r.termMin) r.termMax = r.termMin;

            result.Add(r);
        }

        // ID 중복 경고
        var dup = new HashSet<string>();
        var dupList = new List<string>();
        foreach (var row in result)
        {
            if (!dup.Add(row.id))
                dupList.Add(row.id);
        }

        if (dupList.Count > 0)
        {
            var uniqueDups = new HashSet<string>(dupList);
            Debug.LogWarning($"[SuddenEventTable] Found {dupList.Count} duplicate IDs ({uniqueDups.Count} unique): " +
                             string.Join(", ", uniqueDups));
        }

        return result;
    }    
}
#endif
