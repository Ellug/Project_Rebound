#if UNITY_EDITOR
using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using UnityEditor;
using UnityEngine;

public static class CsvImportUtil
{
    public const string CsvFolder = "Assets/CSV"; // CSV 원본 폴더 경로
    public const string SoFolder = "Assets/_Scripts/SO"; // SO 생성 폴더 경로

    // CSV 폴더의 csv 파일 경로를 이름순으로 반환
    public static string[] FindCsvPaths()
        => Directory.GetFiles(CsvFolder, "*.*", SearchOption.TopDirectoryOnly)
            .Where(path => string.Equals(Path.GetExtension(path), ".csv", StringComparison.OrdinalIgnoreCase))
            .OrderBy(Path.GetFileNameWithoutExtension, StringComparer.OrdinalIgnoreCase)
            .Select(path => path.Replace('\\', '/'))
            .ToArray();

    // 파일명 기준으로 SO와 Row를 찾아 임포트
    public static string Import(string csvPath)
    {
        var tableName = Path.GetFileNameWithoutExtension(csvPath);
        var soType = FindTableType(tableName);
        var rowType = FindRowType(soType);
        var rows = ParseRows(rowType, File.ReadAllText(Path.GetFullPath(csvPath)));
        var assetPath = $"{SoFolder}/SO_{tableName}.asset";
        var so = LoadOrCreateSO(soType, assetPath);
        soType.GetMethod("ReplaceAll", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
            .Invoke(so, new object[] { rows });
        EditorUtility.SetDirty(so);
        return assetPath;
    }

    // 개행과 BOM을 정리해서 줄 리스트로 변환
    public static List<string> SplitLines(string csv)
    {
        if (string.IsNullOrEmpty(csv)) return new List<string>(0);

        var raw = csv.Replace("\r\n", "\n").Replace("\r", "\n").Split('\n');
        var lines = new List<string>(raw.Length);

        for (int i = 0; i < raw.Length; i++)
        {
            var line = raw[i];

            if (i == 0 && line.Length > 0 && line[0] == '\uFEFF')
                line = line.Substring(1);
                
            lines.Add(line);
        }

        return lines;
    }

    // 따옴표를 고려해 한 줄을 셀로 분리
    public static List<string> SplitCsvLine(string line)
    {
        var cells = new List<string>(32);
        if (line == null) return cells;

        bool inQuotes = false;
        var builder = new StringBuilder();

        for (int i = 0; i < line.Length; i++)
        {
            char c = line[i];

            if (c == '"')
            {
                if (inQuotes && i + 1 < line.Length && line[i + 1] == '"')
                {
                    builder.Append('"');
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
                cells.Add(builder.ToString());
                builder.Clear();
                continue;
            }

            builder.Append(c);
        }

        cells.Add(builder.ToString());
        return cells;
    }

    // 2행이 타입 선언이면 데이터는 3행부터 시작.
    public static int GetDataStartRow(List<string> lines)
    {
        if (lines.Count > 1 && IsTypeDefinitionRow(SplitCsvLine(lines[1])))
            return 2;
        return 1;
    }

    // 행의 절반 이상이 타입 키워드면 타입 행으로 간주.
    public static bool IsTypeDefinitionRow(List<string> cells)
    {
        if (cells == null || cells.Count == 0) return false;

        int hit = 0;
        int count = 0;

        foreach (var cell in cells)
        {
            var value = Clean(cell).ToLowerInvariant();
            if (string.IsNullOrEmpty(value)) continue;

            count++;
            if (value == "string" || value == "int" || value == "float" || value == "double" ||
                value == "bool" || value == "boolean" || value == "long" || value == "short" ||
                value == "byte" || value == "decimal" || value == "enum" || value == "flag" ||
                value == "text" || value == "number")
            {
                hit++;
            }
        }

        return count > 0 && hit >= count * 0.5f;
    }

    // 파일명과 같은 TableSO 타입을 탐색
    static Type FindTableType(string tableName)
        => TypeCache.GetTypesDerivedFrom<ScriptableObject>().FirstOrDefault(type => type.Name == $"{tableName}SO")
           ?? throw new Exception($"Missing table type: {tableName}SO");

    // ReplaceAll 또는 _rows에서 Row 타입을 탐색
    static Type FindRowType(Type soType)
    {
        var replaceAll = soType.GetMethod("ReplaceAll", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        if (replaceAll != null)
            return replaceAll.GetParameters()[0].ParameterType.GetGenericArguments()[0];

        var rowsField = soType.GetField("_rows", BindingFlags.Instance | BindingFlags.NonPublic);
        if (rowsField != null)
            return rowsField.FieldType.GetGenericArguments()[0];

        throw new Exception($"Missing row type: {soType.Name}");
    }

    // 헤더와 public field 이름을 맞춰 Row 리스트를 작성
    static IList ParseRows(Type rowType, string csv)
    {
        var lines = SplitLines(csv);
        var rows = (IList)Activator.CreateInstance(typeof(List<>).MakeGenericType(rowType));
        if (lines.Count <= 1) return rows;

        var fields = rowType.GetFields(BindingFlags.Instance | BindingFlags.Public)
            .ToDictionary(field => Normalize(field.Name), field => field);
        var columns = new List<(int index, FieldInfo field)>();
        var header = SplitCsvLine(lines[0]);

        for (int i = 0; i < header.Count; i++)
        {
            if (fields.TryGetValue(Normalize(header[i]), out var field))
                columns.Add((i, field));
        }

        for (int i = GetDataStartRow(lines); i < lines.Count; i++)
        {
            if (string.IsNullOrWhiteSpace(lines[i])) continue;

            var row = Activator.CreateInstance(rowType);
            var cells = SplitCsvLine(lines[i]);

            foreach (var (index, field) in columns)
            {
                if (index >= cells.Count) continue;

                var value = Clean(cells[index]);
                if (field.FieldType == typeof(string))
                {
                    field.SetValue(row, value);
                    continue;
                }

                if (string.IsNullOrEmpty(value)) continue;
                if (TryParseValue(value, field.FieldType, out var parsed))
                    field.SetValue(row, parsed);
            }

            rows.Add(row);
        }

        return rows;
    }

    // 기본 타입과 enum 문자열을 값으로 변환
    static bool TryParseValue(string value, Type type, out object parsed)
    {
        parsed = null;

        if (type.IsEnum)
        {
            parsed = ParseEnum(type, value);
            return true;
        }

        switch (Type.GetTypeCode(type))
        {
            case TypeCode.Boolean:
                if (bool.TryParse(value, out var boolValue))
                {
                    parsed = boolValue;
                    return true;
                }
                if (value == "1" || value == "0")
                {
                    parsed = value == "1";
                    return true;
                }
                return false;
            case TypeCode.Int32:
                if (TryParseInt(value, out var intValue))
                {
                    parsed = intValue;
                    return true;
                }
                return false;
            case TypeCode.Int64:
                if (long.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var longValue))
                {
                    parsed = longValue;
                    return true;
                }
                if (TryParseInt(value, out intValue))
                {
                    parsed = (long)intValue;
                    return true;
                }
                return false;
            case TypeCode.Int16:
                if (short.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var shortValue))
                {
                    parsed = shortValue;
                    return true;
                }
                return false;
            case TypeCode.Byte:
                if (byte.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var byteValue))
                {
                    parsed = byteValue;
                    return true;
                }
                return false;
            case TypeCode.Single:
                if (float.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out var floatValue))
                {
                    parsed = floatValue;
                    return true;
                }
                return false;
            case TypeCode.Double:
                if (double.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out var doubleValue))
                {
                    parsed = doubleValue;
                    return true;
                }
                return false;
            case TypeCode.Decimal:
                if (decimal.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out var decimalValue))
                {
                    parsed = decimalValue;
                    return true;
                }
                return false;
            default:
                return false;
        }
    }

    // enum은 숫자와 Flags 조합 문자열을 모두 허용
    static object ParseEnum(Type type, string value)
    {
        if (TryParseInt(value, out var intValue))
            return Enum.ToObject(type, intValue);

        if (!Attribute.IsDefined(type, typeof(FlagsAttribute)))
            return Enum.ToObject(type, ParseEnumValue(type, value));

        int flags = 0;
        foreach (var token in value.Split('|'))
        {
            var part = token.Trim();
            if (string.IsNullOrEmpty(part)) continue;
            flags |= TryParseInt(part, out intValue) ? intValue : ParseEnumValue(type, part);
        }
        return Enum.ToObject(type, flags);
    }

    // enum 이름 비교는 대소문자와 구분 문자를 무시
    static int ParseEnumValue(Type type, string value)
    {
        var normalized = Normalize(value);
        foreach (var name in Enum.GetNames(type))
        {
            if (Normalize(name) != normalized) continue;
            return Convert.ToInt32(Enum.Parse(type, name), CultureInfo.InvariantCulture);
        }

        throw new ArgumentException($"Requested value '{value}' was not found.");
    }

    // 숫자 또는 suffix 숫자 ID를 int로 변환
    static bool TryParseInt(string value, out int parsed)
    {
        if (int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out parsed))
            return true;

        int index = value.LastIndexOf('_');
        return index >= 0 && int.TryParse(value.Substring(index + 1), NumberStyles.Integer, CultureInfo.InvariantCulture, out parsed);
    }

    // 공백과 '-' placeholder를 정리
    static string Clean(string value)
    {
        value = (value ?? "").Trim();
        return value == "-" ? "" : value;
    }

    // 헤더와 필드 비교용 키로 정규화
    static string Normalize(string value)
    {
        if (string.IsNullOrEmpty(value)) return "";

        var builder = new StringBuilder(value.Length);
        for (int i = 0; i < value.Length; i++)
        {
            if (char.IsLetterOrDigit(value[i]))
                builder.Append(char.ToLowerInvariant(value[i]));
        }
        return builder.ToString();
    }

    // SO 에셋을 불러오거나 새로 만든다
    static ScriptableObject LoadOrCreateSO(Type soType, string assetPath)
    {
        var so = AssetDatabase.LoadAssetAtPath(assetPath, soType) as ScriptableObject;
        if (so != null) return so;

        so = ScriptableObject.CreateInstance(soType);
        AssetDatabase.CreateAsset(so, assetPath);
        return so;
    }
}
#endif
