#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Globalization;
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

        // 생성/갱신할 SO 저장 위치
        const string assetPath = "Assets/_Scripts/SO/SO_GrowthCommandTable.asset";

        var csvText = File.ReadAllText(csvPath);
        var rows = ParseCsvToRows(csvText);

        // SO 로드/생성
        var so = AssetDatabase.LoadAssetAtPath<GrowthCommandTableSO>(assetPath);
        if (so == null)
        {
            so = ScriptableObject.CreateInstance<GrowthCommandTableSO>();
            AssetDatabase.CreateAsset(so, assetPath);
        }

        so.ReplaceAll(rows);
        EditorUtility.SetDirty(so);

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log($"[GrowthCommandTable] Imported {rows.Count} rows -> {assetPath}");
    }

    private static List<GrowthCommandRow> ParseCsvToRows(string csv)
    {
        // CRLF/LF 모두 대응
        var lines = csv.Replace("\r\n", "\n").Replace("\r", "\n").Split('\n');
        var result = new List<GrowthCommandRow>(Mathf.Max(16, lines.Length - 1));

        if (lines.Length <= 1)
            return result;

        // 헤더 파싱
        var header = SplitCsvLine(lines[0]);
        var col = BuildColumnMap(header);

        for (int i = 1; i < lines.Length; i++)
        {
            var line = lines[i];
            if (string.IsNullOrWhiteSpace(line)) continue;

            var cells = SplitCsvLine(line);

            // index 필수
            var index = ReadInt(cells, col, "index", 0);
            if (index == 0) continue;

            var r = new GrowthCommandRow
            {
                index = index,
                name = ReadString(cells, col, "name"),
                icon = ReadString(cells, col, "icon"),
                parentIndex = ReadInt(cells, col, "parent_index", 0),
                btnType = ReadBtnType(cells, col, "btn_type"),
                facilityReq = ReadFacilityReq(cells, col, "facility_req"),
                facilityLv = ReadInt(cells, col, "facility_lv", 1),
                target = ReadTarget(cells, col, "target"),

                hpCost = ReadInt(cells, col, "hp_cost", 0),
                mental = ReadInt(cells, col, "mental", 0),

                shoot = ReadFloat(cells, col, "shoot", 0f),
                speed = ReadFloat(cells, col, "speed", 0f),
                defense = ReadFloat(cells, col, "defense", 0f),
                stamina = ReadFloat(cells, col, "stamina", 0f),

                linkedEventId = ReadLinkedEventId(cells, col, "linked_event_id")
            };

            result.Add(r);
        }

        // 중복 index 검사(원하면 강제 에러 처리)
        var dupCheck = new HashSet<int>();
        for (int i = 0; i < result.Count; i++)
        {
            if (!dupCheck.Add(result[i].index))
            {
                Debug.LogWarning($"[GrowthCommandTable] Duplicate index detected: {result[i].index}");
            }
        }

        return result;
    }

    private static Dictionary<string, int> BuildColumnMap(List<string> header)
    {
        var map = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        for (int i = 0; i < header.Count; i++)
        {
            var key = (header[i] ?? "").Trim();
            if (string.IsNullOrEmpty(key)) continue;
            map[key] = i;
        }
        return map;
    }

    private static List<string> SplitCsvLine(string line)
    {
        var res = new List<string>(32);
        if (line == null) return res;

        bool inQuotes = false;
        var cur = new System.Text.StringBuilder();

        for (int i = 0; i < line.Length; i++)
        {
            char c = line[i];

            if (c == '\"')
            {
                // "" -> "
                if (inQuotes && i + 1 < line.Length && line[i + 1] == '\"')
                {
                    cur.Append('\"');
                    i++;
                }
                else
                {
                    inQuotes = !inQuotes;
                }
                continue;
            }

            if (c == ',' && !inQuotes)
            {
                res.Add(cur.ToString());
                cur.Clear();
                continue;
            }

            cur.Append(c);
        }

        res.Add(cur.ToString());
        return res;
    }

    private static string ReadString(List<string> cells, Dictionary<string, int> col, string key)
    {
        if (!col.TryGetValue(key, out var idx)) return "";
        if (idx < 0 || idx >= cells.Count) return "";
        var v = (cells[idx] ?? "").Trim();
        return (v == "-" ? "" : v);
    }

    private static int ReadInt(List<string> cells, Dictionary<string, int> col, string key, int defaultValue)
    {
        var s = ReadString(cells, col, key);
        if (string.IsNullOrEmpty(s)) return defaultValue;
        if (int.TryParse(s, NumberStyles.Integer, CultureInfo.InvariantCulture, out var v)) return v;
        return defaultValue;
    }

    private static float ReadFloat(List<string> cells, Dictionary<string, int> col, string key, float defaultValue)
    {
        var s = ReadString(cells, col, key);
        if (string.IsNullOrEmpty(s)) return defaultValue;
        if (float.TryParse(s, NumberStyles.Float, CultureInfo.InvariantCulture, out var v)) return v;
        return defaultValue;
    }

    private static GrowthCommandBtnType ReadBtnType(List<string> cells, Dictionary<string, int> col, string key)
    {
        var s = ReadString(cells, col, key).ToLowerInvariant();
        return s switch
        {
            "category" => GrowthCommandBtnType.Category,
            "action" => GrowthCommandBtnType.Action,
            _ => GrowthCommandBtnType.Action
        };
    }

    private static GrowthFacilityReq ReadFacilityReq(List<string> cells, Dictionary<string, int> col, string key)
    {
        var s = ReadString(cells, col, key).ToLowerInvariant();
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

    private static GrowthCommandTarget ReadTarget(List<string> cells, Dictionary<string, int> col, string key)
    {
        var s = ReadString(cells, col, key).ToLowerInvariant();
        return s switch
        {
            "team" => GrowthCommandTarget.Team,
            "individual" => GrowthCommandTarget.Individual,
            "etc" => GrowthCommandTarget.Etc,
            "" => GrowthCommandTarget.Etc,
            _ => GrowthCommandTarget.Etc
        };
    }

    private static int ReadLinkedEventId(List<string> cells, Dictionary<string, int> col, string key)
    {
        // "-" 또는 빈값 -> 0
        return ReadInt(cells, col, key, 0);
    }
}
#endif
