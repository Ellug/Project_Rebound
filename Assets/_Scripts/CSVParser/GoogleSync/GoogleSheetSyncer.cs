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

    // 시트 동기화 + 임포트 + 등록 + 클라우드 업로드 실행
    [MenuItem("Tools/Data/Sync from Google Sheets (Full)")]
    public static void SyncAll()
    {
        RunSyncPipeline(includeCloudUpload: true);
    }

    // 시트 동기화 + 임포트 + 등록까지만 실행
    [MenuItem("Tools/Data/Sync from Google Sheets (No Cloud Upload)")]
    public static void SyncOnly()
    {
        RunSyncPipeline(includeCloudUpload: false);
    }

    // 실행 모드별 파이프라인 진입점
    public static void RunSyncPipeline(bool includeCloudUpload)
    {
        if (includeCloudUpload && !GoogleSheetCloudUploader.TryValidateUgsCliForPipeline(out string ugsValidationMessage))
        {
            Debug.LogError($"[GoogleSheetSyncer] {ugsValidationMessage}");
            EditorUtility.DisplayDialog("Sync 차단 - UGS CLI 필요", ugsValidationMessage, "확인");
            return;
        }

        string steps = includeCloudUpload
            ? "1) Download changed CSV from Google Sheets\n2) Import all CSV tables\n3) Sync TableLoadConfig and Addressables\n4) Build Addressables and upload to CCD (if there are changes)"
            : "1) Download changed CSV from Google Sheets\n2) Import all CSV tables\n3) Sync TableLoadConfig and Addressables\n4) Stop before cloud upload";

        if (!EditorUtility.DisplayDialog("Sync from Google Sheets", $"Run pipeline?\n\n{steps}", "Run", "Cancel"))
            return;

        SyncTables(GoogleSheetSyncConfig.Tables, includeCloudUpload);
    }

    // 시트 목록을 순회하며 다운로드, 비교, 임포트를 처리한다.
    private static void SyncTables(IReadOnlyList<SheetTableEntry> tables, bool includeCloudUpload)
    {
        int total = tables.Count;
        int downloaded = 0, skipped = 0, failed = 0;
        bool importedNow;
        bool deferredByCompile;
        var changedTables = new List<string>();
        GoogleSheetCloudUploadResult cloudResult;

        try
        {
            for (int i = 0; i < total; i++)
            {
                var entry = tables[i];
                string tableName = Path.GetFileNameWithoutExtension(entry.CsvFileName);
                float progress = (float)i / total;

                EditorUtility.DisplayProgressBar("데이터 동기화", $"로딩 중... ({i + 1}/{total})", progress);

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

                try
                {
                    File.WriteAllText(csvPath, csvContent, new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));
                    downloaded++;
                    changedTables.Add(tableName);
                }
                catch (Exception e)
                {
                    Debug.LogError($"[GoogleSheetSyncer] Failed to write CSV: {tableName}\n{e.Message}");
                    failed++;
                }
            }
        }
        finally
        {
            EditorUtility.ClearProgressBar();
        }

        AssetDatabase.Refresh();
        try
        {
            // 시트 동기화 후에는 전체 임포트 + 등록 동기화를 단일 경로로 실행
            EditorUtility.DisplayProgressBar("데이터 동기화", "로딩 중... (임포트)", 0.7f);
            importedNow = CsvBatchImporter.ImportAllTables(showDialog: false, out deferredByCompile, allowCompileDefer: true);

            if (includeCloudUpload)
            {
                EditorUtility.DisplayProgressBar("데이터 동기화", "로딩 중... (클라우드 업로드)", 0.9f);
                if (importedNow && downloaded > 0)
                    cloudResult = GoogleSheetCloudUploader.BuildAndUploadAddressables();
                else if (deferredByCompile)
                {
                    if (downloaded > 0)
                        CsvImportCompileBridge.QueueCloudUploadAfterPendingImport();
                    cloudResult = GoogleSheetCloudUploadResult.Skip("Skipped cloud upload because import is deferred until script compile.");
                }
                else if (!importedNow)
                    cloudResult = GoogleSheetCloudUploadResult.Skip("Skipped cloud upload because import failed.");
                else
                    cloudResult = GoogleSheetCloudUploadResult.Skip("Skipped cloud upload because no CSV changes were detected.");
            }
            else
            {
                cloudResult = GoogleSheetCloudUploadResult.Skip("Skipped cloud upload because this mode only runs Syncer.");
            }
        }
        finally
        {
            EditorUtility.ClearProgressBar();
        }

        string summary = $"Pipeline Complete!\n\nDownloaded: {downloaded}\nSkipped (no change): {skipped}\nFailed: {failed}";
        if (changedTables.Count > 0)
            summary += $"\n\nChanged tables:\n- {string.Join("\n- ", changedTables)}";
        if (deferredByCompile)
            summary += "\n\nSO script was generated. Import will resume automatically after compile.";
        else if (!importedNow)
            summary += "\n\nCSV import failed. Check Console for details.";
        summary += $"\n\nCloud Upload:\n{cloudResult.Message}";
        if (!cloudResult.Success && !cloudResult.Skipped)
            summary += "\n\nCloud upload failed. Check Console for details.";

        EditorUtility.DisplayDialog("Data Pipeline", summary, "OK");
    }

    // 에디터에서 동기처럼 돌리는 CSV 다운로드
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

    // 로컬 CSV와 다운로드 내용을 그대로 비교
    private static bool IsContentSameAsLocal(string csvPath, string downloadedContent)
    {
        if (!File.Exists(csvPath))
            return false;

        string localContent = File.ReadAllText(csvPath, new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));
        return localContent == downloadedContent;
    }

}
#endif
