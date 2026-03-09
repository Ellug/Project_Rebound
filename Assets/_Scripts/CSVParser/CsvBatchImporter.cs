#if UNITY_EDITOR
using System;
using System.IO;
using UnityEditor;
using UnityEngine;

public static class CsvBatchImporter
{
    // CSV 폴더 전체를 순회하며 임포트
    public static void ImportAllTables() => ImportAllTables(showDialog: true);

    // 자동 파이프라인에서 호출할 수 있는 전체 임포트 엔트리
    public static bool ImportAllTables(bool showDialog)
    {
        // 누락된 SO 스크립트가 있으면 생성 후 컴파일 완료 시 자동 재개
        if (!CsvImportCompileBridge.EnsureSoScriptsReadyAndMaybeDefer("Import All CSV Tables"))
            return false;

        var csvPaths = CsvImportUtil.FindCsvPaths();
        int successCount = 0;
        int failCount = 0;

        try
        {
            for (int i = 0; i < csvPaths.Length; i++)
            {
                var csvPath = csvPaths[i];
                EditorUtility.DisplayProgressBar("Importing CSV Tables", Path.GetFileName(csvPath), (float)i / csvPaths.Length);

                try
                {
                    CsvImportUtil.Import(csvPath);
                    successCount++;
                }
                catch (Exception e)
                {
                    Debug.LogError($"[CsvBatchImporter] {Path.GetFileName(csvPath)}\n{e.Message}");
                    failCount++;
                }
            }
        }
        finally
        {
            EditorUtility.ClearProgressBar();
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        // 임포트된 SO 기준으로 테이블 로드 설정을 즉시 갱신
        TableLoadConfigAutoSync.Sync(showDialog: false);

        if (showDialog)
            EditorUtility.DisplayDialog("Import Complete", $"Success: {successCount}\nFailed: {failCount}", "OK");
            
        return true;
    }
}
#endif
