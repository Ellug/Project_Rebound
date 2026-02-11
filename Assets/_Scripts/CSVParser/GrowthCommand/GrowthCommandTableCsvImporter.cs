#if UNITY_EDITOR
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

public static class GrowthCommandTableCsvImporter
{
    [MenuItem("Tools/Data/Import Growth Command CSV -> SO")]
    public static void Import()
    {
        var csvPath = EditorUtility.OpenFilePanel("Select Growth Command CSV", Application.dataPath, "csv");
        if (string.IsNullOrEmpty(csvPath))
            return;

        const string assetPath = "Assets/_Scripts/SO/SO_GrowthCommandTable.asset";

        var csvText = File.ReadAllText(csvPath);
        var rows = ParseCsvToRows(csvText);

        var so = CsvImportUtil.LoadOrCreateSO<GrowthCommandTableSO>(assetPath);
        so.ReplaceAll(rows);

        EditorUtility.SetDirty(so);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log($"[GrowthCommandTable] Imported {rows.Count} rows -> {assetPath}");
    }

    private static List<GrowthCommandRow> ParseCsvToRows(string csv)
    {
        var lines = CsvImportUtil.SplitLines(csv);
        var result = new List<GrowthCommandRow>(Mathf.Max(16, lines.Count - 1));
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

            var index = CsvImportUtil.ReadInt(cells, col, "index", 0);
            if (index == 0) continue;

            var r = new GrowthCommandRow
            {
                index = index,
                name = CsvImportUtil.ReadString(cells, col, "name"),
                icon = CsvImportUtil.ReadString(cells, col, "icon"),
                parentIndex = CsvImportUtil.ReadInt(cells, col, "parent_index", 0),

                btnType = ReadBtnType(CsvImportUtil.ReadString(cells, col, "btn_type")),
                facilityReq = ReadFacilityReq(CsvImportUtil.ReadString(cells, col, "facility_req")),
                facilityLv = CsvImportUtil.ReadInt(cells, col, "facility_lv", 1),
                target = ReadTarget(CsvImportUtil.ReadString(cells, col, "target")),

                hpCost = CsvImportUtil.ReadInt(cells, col, "hp_cost", 0),
                mental = CsvImportUtil.ReadInt(cells, col, "mental", 0),

                shoot = CsvImportUtil.ReadFloat(cells, col, "shoot", 0f),
                speed = CsvImportUtil.ReadFloat(cells, col, "speed", 0f),
                defense = CsvImportUtil.ReadFloat(cells, col, "defense", 0f),
                stamina = CsvImportUtil.ReadFloat(cells, col, "stamina", 0f),

                linkedEventId = CsvImportUtil.ReadInt(cells, col, "linked_event_id", 0),
            };

            result.Add(r);
        }

        // 중복 index 경고
        var dupCheck = new HashSet<int>();
        var dupList = new List<int>();
        foreach (var r in result)
        {
            if (!dupCheck.Add(r.index))
                dupList.Add(r.index);
        }

        if (dupList.Count > 0)
        {
            var uniqueDups = new HashSet<int>(dupList);
            Debug.LogWarning($"[GrowthCommandTable] Found {dupList.Count} duplicate indices ({uniqueDups.Count} unique): " +
                             string.Join(", ", uniqueDups));
        }

        return result;
    }

    // ---- Domain parsing only ----
    private static GrowthCommandBtnType ReadBtnType(string s)
    {
        s = (s ?? "").Trim().ToLowerInvariant();
        return s switch
        {
            "category" => GrowthCommandBtnType.Category,
            "action" => GrowthCommandBtnType.Action,
            _ => GrowthCommandBtnType.Action
        };
    }

    private static GrowthFacilityReq ReadFacilityReq(string s)
    {
        s = (s ?? "").Trim().ToLowerInvariant();
        return s switch
        {
            "school" => GrowthFacilityReq.School,
            "gym" => GrowthFacilityReq.Gym,
            "cafeteria" => GrowthFacilityReq.Cafeteria,
            "counselingcenter" => GrowthFacilityReq.CounselingCenter,
            "" => GrowthFacilityReq.None,
            _ => GrowthFacilityReq.None
        };
    }

    private static GrowthCommandTarget ReadTarget(string s)
    {
        s = (s ?? "").Trim().ToLowerInvariant();
        return s switch
        {
            "team" => GrowthCommandTarget.Team,
            "individual" => GrowthCommandTarget.Individual,
            "etc" => GrowthCommandTarget.Etc,
            "" => GrowthCommandTarget.Etc,
            _ => GrowthCommandTarget.Etc
        };
    }
}
#endif
