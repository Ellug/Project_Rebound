#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using UnityEditor;
using UnityEngine;

public static class CsvImportUtil
{
    // ---- Text ----
    // CSV 텍스트를 줄 단위로 분리
    public static List<string> SplitLines(string csv)
    {
        if (string.IsNullOrEmpty(csv)) return new List<string>(0);

        var normalized = csv.Replace("\r\n", "\n").Replace("\r", "\n");
        var raw = normalized.Split('\n');

        var lines = new List<string>(raw.Length);
        for (int i = 0; i < raw.Length; i++)
        {
            var s = raw[i];
            // BOM 제거
            if (i == 0 && s.Length > 0 && s[0] == '\uFEFF')
                s = s.Substring(1);

            lines.Add(s);
        }
        return lines;
    }

    // CSV 한 줄을 컬럼으로 분리 (따옴표 및 쉼표 이스케이프 처리)
    public static List<string> SplitCsvLine(string line)
    {
        var res = new List<string>(32);
        if (line == null) return res;

        bool inQuotes = false;
        var cur = new StringBuilder();

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

    // 헤더 리스트를 컬럼명 -> 인덱스 매핑으로 변환
    public static Dictionary<string, int> BuildColumnMap(List<string> header)
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

    // 해당 행이 자료형 정의 행인지 확인
    public static bool IsTypeDefinitionRow(List<string> cells)
    {
        if (cells == null || cells.Count == 0) return false;

        int typeKeywordCount = 0;
        int nonEmptyCount = 0;

        foreach (var cell in cells)
        {
            var s = (cell ?? "").Trim().ToLowerInvariant();
            if (string.IsNullOrEmpty(s) || s == "-") continue;

            nonEmptyCount++;

            // 정확한 키워드 매칭
            if (s == "string" || s == "int" || s == "float" || s == "double" ||
                s == "bool" || s == "boolean" || s == "long" || s == "short" ||
                s == "byte" || s == "decimal" || s == "enum" || s == "flag" ||
                s == "text" || s == "number" || s == "bool")
            {
                typeKeywordCount++;
            }
            // string 오타 및 단축어 허용
            else if (s.Contains("str") && s.Length <= 7)
            {
                typeKeywordCount++;
            }
        }

        // 비어있지 않은 셀의 50% 이상이 자료형 키워드면 자료형 행으로 판단
        return nonEmptyCount > 0 && typeKeywordCount >= nonEmptyCount * 0.5f;
    }

    // ---- Read ----
    // 컬럼에서 문자열 값 읽기 ("-"는 빈 문자열로 처리)
    public static string ReadString(List<string> cells, Dictionary<string, int> col, string key)
    {
        if (!col.TryGetValue(key, out var idx)) return "";
        if (idx < 0 || idx >= cells.Count) return "";
        var v = (cells[idx] ?? "").Trim();
        return (v == "-" ? "" : v);
    }

    // 컬럼에서 정수 값 읽기
    public static int ReadInt(List<string> cells, Dictionary<string, int> col, string key, int defaultValue)
    {
        var s = ReadString(cells, col, key);
        if (string.IsNullOrEmpty(s)) return defaultValue;
        if (int.TryParse(s, NumberStyles.Integer, CultureInfo.InvariantCulture, out var v)) return v;
        return defaultValue;
    }

    // 컬럼에서 실수 값 읽기
    public static float ReadFloat(List<string> cells, Dictionary<string, int> col, string key, float defaultValue)
    {
        var s = ReadString(cells, col, key);
        if (string.IsNullOrEmpty(s)) return defaultValue;
        if (float.TryParse(s, NumberStyles.Float, CultureInfo.InvariantCulture, out var v)) return v;
        return defaultValue;
    }

    // 컬럼에서 단일 Enum 값 읽기 (문자열 또는 숫자 지원)
    public static T ReadEnumSingle<T>(List<string> cells, Dictionary<string, int> col, string key, T defaultValue)
        where T : struct
    {
        var s = ReadString(cells, col, key);
        if (string.IsNullOrEmpty(s)) return defaultValue;

        // 숫자도 허용
        if (int.TryParse(s, NumberStyles.Integer, CultureInfo.InvariantCulture, out var iv))
        {
            try { return (T)Enum.ToObject(typeof(T), iv); }
            catch { return defaultValue; }
        }

        if (Enum.TryParse<T>(s, ignoreCase: true, out var ev))
            return ev;

        return defaultValue;
    }

    // 컬럼에서 Flag Enum 값 읽기
    public static T ReadFlags<T>(List<string> cells, Dictionary<string, int> col, string key)
        where T : struct
    {
        var s = ReadString(cells, col, key);
        if (string.IsNullOrEmpty(s)) return default;

        // 숫자 허용
        if (int.TryParse(s, NumberStyles.Integer, CultureInfo.InvariantCulture, out var iv))
        {
            try { return (T)Enum.ToObject(typeof(T), iv); }
            catch { return default; }
        }

        int acc = 0;

        // "|" 기준 분리 (공백 허용)
        var parts = s.Split('|');
        for (int i = 0; i < parts.Length; i++)
        {
            var token = parts[i].Trim();
            if (string.IsNullOrEmpty(token)) continue;

            if (Enum.TryParse(token, ignoreCase: true, out T parsed))
            {
                acc |= Convert.ToInt32(parsed);
            }
            else
            {
                // 토큰이 숫자면 OR
                if (int.TryParse(token, NumberStyles.Integer, CultureInfo.InvariantCulture, out var tiv))
                    acc |= tiv;
            }
        }

        try { return (T)Enum.ToObject(typeof(T), acc); }
        catch { return default; }
    }

    // ---- Asset ----
    // ScriptableObject 로드 또는 생성 (없으면 새로 생성)
    public static T LoadOrCreateSO<T>(string assetPath) where T : ScriptableObject
    {
        var so = AssetDatabase.LoadAssetAtPath<T>(assetPath);
        if (so != null) return so;

        so = ScriptableObject.CreateInstance<T>();
        AssetDatabase.CreateAsset(so, assetPath);
        return so;
    }
}
#endif
