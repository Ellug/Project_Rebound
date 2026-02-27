#if UNITY_EDITOR
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEngine;

// TutorialGuide CSV를 ScriptableObject로 변환하는 에디터 전용 임포터
public static class TutorialGuideTableCsvImporter
{
    // 메뉴에서 수동으로 CSV 선택 후 임포트
    [MenuItem("Tools/Data/Import Tutorial Guide CSV -> SO")]
    public static void Import()
    {
        var csvPath = EditorUtility.OpenFilePanel("Select Tutorial Guide CSV", Application.dataPath, "csv");
        if (string.IsNullOrEmpty(csvPath))
            return;

        ImportFromPath(csvPath);
    }

    // 지정 경로의 CSV를 읽어 SO로 변환
    public static void ImportFromPath(string csvPath)
    {
        const string assetPath = "Assets/_Scripts/SO/SO_TutorialGuideTable.asset";

        string csvText = ReadAllTextAutoEncoding(csvPath);
        var rows = ParseCsvToRows(csvText);

        // SO 로드 또는 생성 후 데이터 교체
        var so = CsvImportUtil.LoadOrCreateSO<TutorialGuideTableSO>(assetPath);
        so.ReplaceAll(rows);

        EditorUtility.SetDirty(so);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log($"[TutorialGuideTable] Imported {rows.Count} rows -> {assetPath}");
    }

    // UTF-8 우선 시도 후, 실패 시 CP949로 재시도
    private static string ReadAllTextAutoEncoding(string path)
    {
        try
        {
            return File.ReadAllText(path, new UTF8Encoding(encoderShouldEmitUTF8Identifier: false, throwOnInvalidBytes: true));
        }
        catch
        {
            return File.ReadAllText(path, Encoding.GetEncoding(949));
        }
    }

    // CSV 텍스트를 파싱하여 Row 리스트 생성
    private static List<TutorialGuideRow> ParseCsvToRows(string csv)
    {
        var lines = CsvImportUtil.SplitLines(csv);
        var result = new List<TutorialGuideRow>(Mathf.Max(8, lines.Count - 1));
        if (lines.Count <= 1) return result;

        // 헤더 기반 컬럼 매핑 구성
        var header = CsvImportUtil.SplitCsvLine(lines[0]);
        var col = CsvImportUtil.BuildColumnMap(header);

        // 두 번째 줄이 타입 정의 행이면 스킵
        int startRow = 1;
        if (lines.Count > 1)
        {
            var secondRow = CsvImportUtil.SplitCsvLine(lines[1]);
            if (CsvImportUtil.IsTypeDefinitionRow(secondRow))
                startRow = 2;
        }

        // 실제 데이터 행 파싱
        for (int i = startRow; i < lines.Count; i++)
        {
            var line = lines[i];
            if (string.IsNullOrWhiteSpace(line)) continue;

            var cells = CsvImportUtil.SplitCsvLine(line);

            int index = CsvImportUtil.ReadInt(cells, col, "index", 0);
            if (index == 0) continue; // index 0은 무시

            var r = new TutorialGuideRow
            {
                index = index,
                img = CsvImportUtil.ReadString(cells, col, "img"),
                titleText = CsvImportUtil.ReadString(cells, col, "title_text"),
                desc = CsvImportUtil.ReadString(cells, col, "description"),
            };

            result.Add(r);
        }

        return result;
    }
}
#endif