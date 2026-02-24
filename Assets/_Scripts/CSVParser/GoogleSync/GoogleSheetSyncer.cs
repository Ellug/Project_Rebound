#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEngine;
using UnityEngine.Networking;

// 구글 시트에서 CSV를 다운로드하고 로컬 파일과 직접 비교 후 변경 시 저장 및 임포트까지 자동화하는 에디터 전용 동기화 클래스
public static class GoogleSheetSyncer
{
    private const string CSV_FOLDER = "Assets/CSV";
    private const int TIMEOUT_SECONDS = 30;

    [MenuItem("Tools/Data/Sync from Google Sheets")]
    public static void SyncAll()
    {
        if (!EditorUtility.DisplayDialog("Sync from Google Sheets",
            "Download and sync changed CSV tables from Google Sheets?\n\nOnly changed tables will be re-imported.",
            "Sync", "Cancel"))
            return;

        SyncTables(GoogleSheetSyncConfig.Tables, forceAll: false);
    }

    [MenuItem("Tools/Data/Sync from Google Sheets (Force All)")]
    public static void SyncAllForce()
    {
        if (!EditorUtility.DisplayDialog("Force Sync from Google Sheets",
            "Force download ALL CSV tables and re-import regardless of changes?",
            "Force Sync", "Cancel"))
            return;

        SyncTables(GoogleSheetSyncConfig.Tables, forceAll: true);
    }

    // ---- 동기화 ----

    // 지정한 테이블 목록을 순서대로 다운로드 → 로컬 CSV와 직접 비교 → 변경 시 CSV 저장 및 임포트
    public static void SyncTables(IReadOnlyList<SheetTableEntry> tables, bool forceAll = false)
    {
        int total = tables.Count;
        int downloaded = 0, skipped = 0, failed = 0;
        var changedTables = new List<string>();

        try
        {
            for (int i = 0; i < total; i++)
            {
                var entry = tables[i];
                float progress = (float)i / total;

                EditorUtility.DisplayProgressBar(
                    "Syncing from Google Sheets",
                    $"Checking {entry.DisplayName}... ({i + 1}/{total})",
                    progress);

                string csvContent = DownloadCsvSync(entry.SheetUrl);
                if (csvContent == null)
                {
                    Debug.LogError($"[GoogleSheetSyncer] Failed to download: {entry.DisplayName}");
                    failed++;
                    continue;
                }

                string csvPath = Path.Combine(CSV_FOLDER, entry.CsvFileName);

                if (!forceAll && IsContentSameAsLocal(csvPath, csvContent))
                {
                    Debug.Log($"[GoogleSheetSyncer] No change: {entry.DisplayName}");
                    skipped++;
                    continue;
                }

                File.WriteAllText(csvPath, csvContent, new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));

                EditorUtility.DisplayProgressBar(
                    "Syncing from Google Sheets",
                    $"Importing {entry.DisplayName}... ({i + 1}/{total})",
                    progress + 0.5f / total);

                try
                {
                    InvokeImporter(entry.CsvFileName, csvPath);
                }
                catch (Exception e)
                {
                    Debug.LogError($"[GoogleSheetSyncer] Import failed for {entry.DisplayName}: {e.Message}");
                    failed++;
                    continue;
                }

                downloaded++;
                changedTables.Add(entry.DisplayName);
            }
        }
        finally
        {
            EditorUtility.ClearProgressBar();
        }

        if (downloaded > 0)
        {
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }

        string summary = $"Sync Complete!\n\nDownloaded & Imported: {downloaded}\nSkipped (no change): {skipped}\nFailed: {failed}";
        if (changedTables.Count > 0)
            summary += $"\n\nChanged tables:\n- {string.Join("\n- ", changedTables)}";

        EditorUtility.DisplayDialog("Google Sheets Sync", summary, "OK");
    }

    // ---- 다운로드 ----

    // UnityWebRequest를 while 루프로 블로킹해서 동기 처리 (에디터 전용)
    private static string DownloadCsvSync(string url)
    {
        using var request = UnityWebRequest.Get(url);
        request.timeout = TIMEOUT_SECONDS;

        var operation = request.SendWebRequest();
        while (!operation.isDone)
            System.Threading.Thread.Sleep(10);

        if (request.result != UnityWebRequest.Result.Success)
        {
            Debug.LogError($"[GoogleSheetSyncer] HTTP error: {request.error} | URL: {url}");
            return null;
        }

        string content = request.downloadHandler.text;

        // 구글이 HTML 오류 페이지를 반환했을 때 감지 (URL 잘못됐거나 비공개 시트인 경우)
        if (content.TrimStart().StartsWith("<!DOCTYPE", StringComparison.OrdinalIgnoreCase) ||
            content.TrimStart().StartsWith("<html", StringComparison.OrdinalIgnoreCase))
        {
            Debug.LogError($"[GoogleSheetSyncer] Received HTML instead of CSV. URL may be incorrect or sheet is not publicly accessible.\nURL: {url}");
            return null;
        }

        return content;
    }

    // ---- 로컬 비교 ----

    // 로컬 CSV 파일이 존재하고 내용이 동일하면 true
    private static bool IsContentSameAsLocal(string csvPath, string downloadedContent)
    {
        if (!File.Exists(csvPath))
            return false;

        string localContent = File.ReadAllText(csvPath, new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));
        return localContent == downloadedContent;
    }

    // ---- 임포터 라우팅 ----

    // csvFileName에 매핑된 개별 임포터의 ImportFromPath를 호출
    private static void InvokeImporter(string csvFileName, string csvPath)
    {
        switch (csvFileName)
        {
            case "GrowthCommandTable.csv":     GrowthCommandTableCsvImporter.ImportFromPath(csvPath);     break;
            case "AlwaysEffectTable.csv":      AlwaysEffectTableCsvImporter.ImportFromPath(csvPath);      break;
            case "AlwaysEventTable.csv":       AlwaysEventTableCsvImporter.ImportFromPath(csvPath);       break;
            case "SuddenEventTable.csv":       SuddenEventTableCsvImporter.ImportFromPath(csvPath);       break;
            case "SuddenEventEffectTable.csv": SuddenEventEffectTableCsvImporter.ImportFromPath(csvPath); break;
            case "SuddenEventTextTable.csv":   SuddenEventTextTableCsvImporter.ImportFromPath(csvPath);   break;
            case "StatusTextTable.csv":        StatusTextTableCsvImporter.ImportFromPath(csvPath);        break;
            case "SchoolNameTable.csv":        SchoolNameTableCsvImporter.ImportFromPath(csvPath);        break;
            case "EnemyStatTable.csv":         EnemyStatTableCsvImporter.ImportFromPath(csvPath);         break;
            case "StudentNameTable.csv":       StudentNameTableCsvImporter.ImportFromPath(csvPath);       break;
            case "StudentBodyTable.csv":       StudentBodyTableCsvImporter.ImportFromPath(csvPath);       break;
            case "StudentStatTable.csv":       StudentStatTableCsvImporter.ImportFromPath(csvPath);       break;
            case "StudentStartStatTable.csv":  StudentStartStatTableCsvImporter.ImportFromPath(csvPath);  break;
            case "StudentPotentialTable.csv":  StudentPotentialTableCsvImporter.ImportFromPath(csvPath);  break;
            case "StudentStatusProbTable.csv": StudentStatusProbTableCsvImporter.ImportFromPath(csvPath); break;
            case "StudentStatExpTable.csv":    StudentStatExpTableCsvImporter.ImportFromPath(csvPath);    break;
            case "StudentPlusExpTable.csv":    StudentPlusExpTableCsvImporter.ImportFromPath(csvPath);    break;
            case "StudentPositionTable.csv":   StudentPositionTableCsvImporter.ImportFromPath(csvPath);   break;
            default:
                Debug.LogWarning($"[GoogleSheetSyncer] No importer registered for: {csvFileName}");
                break;
        }
    }
}
#endif
