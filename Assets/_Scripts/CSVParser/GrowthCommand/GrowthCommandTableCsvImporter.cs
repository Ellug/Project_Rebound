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

        ImportFromPath(csvPath);
    }

    public static void ImportFromPath(string csvPath)
    {
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

        int startRow = CsvImportUtil.GetDataStartRow(lines);

        for (int i = startRow; i < lines.Count; i++)
        {
            var line = lines[i];
            if (string.IsNullOrWhiteSpace(line)) continue;

            var cells = CsvImportUtil.SplitCsvLine(line);

            var r = new GrowthCommandRow
            {
                index = CsvImportUtil.ReadInt(cells, col, "index", 0),
                name = CsvImportUtil.ReadString(cells, col, "name"),
                icon = CsvImportUtil.ReadString(cells, col, "icon"),
                parentIndex = CsvImportUtil.ReadInt(cells, col, "parent_index", 0),

                btnType = CsvImportUtil.ReadEnumSingle(cells, col, "btn_type", GrowthCommandBtnType.Action),
                facilityReq = CsvImportUtil.ReadEnumSingle(cells, col, "facility_req", GrowthFacilityReq.None),
                facilityLv = CsvImportUtil.ReadInt(cells, col, "facility_lv", 1),
                target = CsvImportUtil.ReadEnumSingle(cells, col, "target", GrowthCommandTarget.Etc),

                conditionCost = CsvImportUtil.ReadInt(cells, col, "condition_cost", 0),
                mental = CsvImportUtil.ReadInt(cells, col, "mental", 0),

                shoot = CsvImportUtil.ReadFloat(cells, col, "shoot", 0f),
                speed = CsvImportUtil.ReadFloat(cells, col, "speed", 0f),
                jump = CsvImportUtil.ReadFloat(cells, col, "jump", 0f),
                stamina = CsvImportUtil.ReadFloat(cells, col, "stamina", 0f),

                linkedEventId = CsvImportUtil.ReadInt(cells, col, "linked_event_id", 0),
            };

            result.Add(r);
        }

        return result;
    }
}
#endif
