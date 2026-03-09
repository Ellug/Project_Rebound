#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEngine;
using UnityEngine.Networking;

// 구글 시트 동기화부터 전체 임포트/등록까지 데이터 파이프라인을 실행한다.
public static class GoogleSheetSyncer
{
    private const string CSV_FOLDER = "Assets/CSV";
    private const int TIMEOUT_SECONDS = 30;

    // 구글 시트 동기화부터 전체 임포트/등록까지 데이터 파이프라인을 실행한다.
    [MenuItem("Tools/Data/Sync from Google Sheets")]
    public static void SyncAll()
    {
        if (!EditorUtility.DisplayDialog("Sync from Google Sheets",
            "Run pipeline?\n\n1) Download changed CSV from Google Sheets\n2) Import all CSV tables\n3) Sync TableLoadConfig and Addressables",
            "Run", "Cancel"))
            return;

        SyncTables(GoogleSheetSyncConfig.Tables);
    }

    // 시트 목록을 순회하며 다운로드, 비교, 임포트를 처리한다.
    private static void SyncTables(IReadOnlyList<SheetTableEntry> tables)
    {
        int total = tables.Count;
        int downloaded = 0, skipped = 0, failed = 0;
        bool importedNow = true;
        var changedTables = new List<string>();

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

                if (IsContentSameAsLocal(csvPath, csvContent))
                {
                    Debug.Log($"[GoogleSheetSyncer] No change: {tableName}");
                    skipped++;
                    continue;
                }

                File.WriteAllText(csvPath, csvContent, new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));
                downloaded++;
                changedTables.Add(tableName);
            }
        }
        finally
        {
            EditorUtility.ClearProgressBar();
        }

        AssetDatabase.Refresh();
        // 시트 동기화 후에는 전체 임포트 + 등록 동기화를 단일 경로로 실행
        importedNow = CsvBatchImporter.ImportAllTables(showDialog: false);

        string summary = $"Pipeline Complete!\n\nDownloaded: {downloaded}\nSkipped (no change): {skipped}\nFailed: {failed}";
        if (changedTables.Count > 0)
            summary += $"\n\nChanged tables:\n- {string.Join("\n- ", changedTables)}";
        if (!importedNow)
            summary += "\n\nSO script was generated. Import will resume automatically after compile.";

        EditorUtility.DisplayDialog("Data Pipeline", summary, "OK");
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
