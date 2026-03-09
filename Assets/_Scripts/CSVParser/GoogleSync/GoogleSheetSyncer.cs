#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEngine;
using UnityEngine.Networking;

// 구글 시트 CSV를 내려받아 변경분만 다시 임포트한다.
public static class GoogleSheetSyncer
{
    private const string CSV_FOLDER = "Assets/CSV";
    private const int TIMEOUT_SECONDS = 30;

    // 변경된 CSV만 동기화한다.
    [MenuItem("Tools/Data/Sync from Google Sheets")]
    public static void SyncAll()
    {
        if (!EditorUtility.DisplayDialog("Sync from Google Sheets",
            "Download and sync changed CSV tables from Google Sheets?\n\nOnly changed tables will be re-imported.",
            "Sync", "Cancel"))
            return;

        SyncTables(GoogleSheetSyncConfig.Tables, forceAll: false);
    }

    // 모든 CSV를 강제로 다시 동기화한다.
    [MenuItem("Tools/Data/Sync from Google Sheets (Force All)")]
    public static void SyncAllForce()
    {
        if (!EditorUtility.DisplayDialog("Force Sync from Google Sheets",
            "Force download ALL CSV tables and re-import regardless of changes?",
            "Force Sync", "Cancel"))
            return;

        SyncTables(GoogleSheetSyncConfig.Tables, forceAll: true);
    }

    // 시트 목록을 순회하며 다운로드, 비교, 임포트를 처리한다.
    public static void SyncTables(IReadOnlyList<SheetTableEntry> tables, bool forceAll = false)
    {
        int total = tables.Count;
        int imported = 0, skipped = 0, failed = 0;
        var pendingImports = new List<(string tableName, string csvPath)>();
        var importedTables = new List<string>();

        try
        {
            for (int i = 0; i < total; i++)
            {
                var entry = tables[i];
                string tableName = Path.GetFileNameWithoutExtension(entry.CsvFileName);
                float progress = (float)i / total;

                EditorUtility.DisplayProgressBar(
                    "Syncing from Google Sheets",
                    $"Checking {tableName}... ({i + 1}/{total})",
                    progress);

                string csvContent = DownloadCsvSync(entry.SheetUrl);
                if (csvContent == null)
                {
                    Debug.LogError($"[GoogleSheetSyncer] Failed to download: {tableName}");
                    failed++;
                    continue;
                }

                string csvPath = Path.Combine(CSV_FOLDER, entry.CsvFileName);

                if (!forceAll && IsContentSameAsLocal(csvPath, csvContent))
                {
                    Debug.Log($"[GoogleSheetSyncer] No change: {tableName}");
                    skipped++;
                    continue;
                }

                File.WriteAllText(csvPath, csvContent, new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));
                pendingImports.Add((tableName, csvPath));
            }
        }
        finally
        {
            EditorUtility.ClearProgressBar();
        }

        if (pendingImports.Count > 0)
        {
            // 새 테이블이 있으면 SO 스크립트 생성 후 컴파일 완료 시 ImportAll 자동 재개
            if (!CsvImportCompileBridge.EnsureSoScriptsReadyAndMaybeDefer("Google Sheets Sync"))
                return;

            try
            {
                for (int i = 0; i < pendingImports.Count; i++)
                {
                    var item = pendingImports[i];
                    float progress = (float)i / pendingImports.Count;

                    EditorUtility.DisplayProgressBar(
                        "Syncing from Google Sheets",
                        $"Importing {item.tableName}... ({i + 1}/{pendingImports.Count})",
                        progress);

                    try
                    {
                        CsvImportUtil.Import(item.csvPath);
                        imported++;
                        importedTables.Add(item.tableName);
                    }
                    catch (Exception e)
                    {
                        Debug.LogError($"[GoogleSheetSyncer] Import failed for {item.tableName}: {e.Message}");
                        failed++;
                    }
                }
            }
            finally
            {
                EditorUtility.ClearProgressBar();
            }
        }

        if (imported > 0)
        {
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            // 동기화로 변경된 SO 목록을 TableLoadConfig에 반영
            TableLoadConfigAutoSync.Sync(showDialog: false);
        }

        string summary = $"Sync Complete!\n\nDownloaded & Imported: {imported}\nSkipped (no change): {skipped}\nFailed: {failed}";
        if (importedTables.Count > 0)
            summary += $"\n\nChanged tables:\n- {string.Join("\n- ", importedTables)}";

        EditorUtility.DisplayDialog("Google Sheets Sync", summary, "OK");
    }

    // 에디터에서 동기처럼 돌리는 CSV 다운로드.
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

        // HTML이 오면 잘못된 URL 또는 권한 문제로 본다.
        if (content.TrimStart().StartsWith("<!DOCTYPE", StringComparison.OrdinalIgnoreCase) ||
            content.TrimStart().StartsWith("<html", StringComparison.OrdinalIgnoreCase))
        {
            Debug.LogError($"[GoogleSheetSyncer] Received HTML instead of CSV. URL may be incorrect or sheet is not publicly accessible.\nURL: {url}");
            return null;
        }

        return content;
    }

    // 로컬 CSV와 다운로드 내용을 그대로 비교한다.
    private static bool IsContentSameAsLocal(string csvPath, string downloadedContent)
    {
        if (!File.Exists(csvPath))
            return false;

        string localContent = File.ReadAllText(csvPath, new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));
        return localContent == downloadedContent;
    }

}
#endif
