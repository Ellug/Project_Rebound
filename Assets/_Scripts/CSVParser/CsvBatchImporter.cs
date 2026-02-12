#if UNITY_EDITOR
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

public static class CsvBatchImporter
{
    private const string CSV_FOLDER = "Assets/CSV";

    // 모든 CSV 테이블을 한 번에 임포트
    [MenuItem("Tools/Data/Import All CSV Tables")]
    public static void ImportAllTables()
    {
        if (!EditorUtility.DisplayDialog("Import All CSV Tables",
            "Import all CSV files from the CSV folder?\n\nThis will overwrite existing ScriptableObjects.",
            "Import", "Cancel"))
        {
            return;
        }

        // 임포트할 CSV 파일 목록 (각 임포터의 ImportFromPath 메서드 사용)
        var importTasks = new List<(string csvFileName, System.Action<string> importAction, string tableName)>
        {
            ("GrowthCommandTable.csv", GrowthCommandTableCsvImporter.ImportFromPath, "Growth Command"),
            ("SuddenEventTable.csv", SuddenEventTableCsvImporter.ImportFromPath, "Sudden Event"),
            ("SuddenEventEffectTable.csv", SuddenEventEffectTableCsvImporter.ImportFromPath, "Sudden Event Effect"),
            ("SuddenEventTextTable.csv", SuddenEventTextTableCsvImporter.ImportFromPath, "Sudden Event Text"),
            ("StatusTextTable.csv", StatusTextTableCsvImporter.ImportFromPath, "Status Text")
        };

        int totalCount = importTasks.Count;
        int successCount = 0;
        int failCount = 0;

        // 각 테이블 순회하며 임포트
        for (int i = 0; i < importTasks.Count; i++)
        {
            var (csvFileName, importAction, tableName) = importTasks[i];
            float progress = (float)i / totalCount;

            EditorUtility.DisplayProgressBar("Importing CSV Tables",
                $"Importing {tableName}... ({i + 1}/{totalCount})", progress);

            string csvPath = Path.Combine(CSV_FOLDER, csvFileName);

            if (!File.Exists(csvPath))
            {
                Debug.LogWarning($"[CsvBatchImporter] CSV file not found: {csvPath}");
                failCount++;
                continue;
            }

            try
            {
                importAction(csvPath);
                successCount++;
                Debug.Log($"[CsvBatchImporter] Successfully imported: {tableName}");
            }
            catch (System.Exception e)
            {
                Debug.LogError($"[CsvBatchImporter] Failed to import {tableName}: {e.Message}");
                failCount++;
            }
        }

        EditorUtility.ClearProgressBar();
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        // 결과 요약 다이얼로그 표시
        string message = $"Import Complete!\n\nSuccess: {successCount}\nFailed: {failCount}";
        EditorUtility.DisplayDialog("Import Complete", message, "OK");
    }
}
#endif
