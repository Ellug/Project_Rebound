#if UNITY_EDITOR
using System;
using System.IO;
using UnityEditor;
using UnityEngine;

public static class CsvBatchImporter
{
    // 파일 선택으로 단일 CSV를 임포트
    [MenuItem("Tools/Data/Import CSV Table")]
    public static void Import()
    {
        var csvPath = EditorUtility.OpenFilePanel("Select CSV", Path.GetFullPath(CsvImportUtil.CsvFolder), "csv");
        if (string.IsNullOrEmpty(csvPath)) return;
        ImportAndSave(csvPath);
    }

    // CSV 폴더 전체를 순회하며 임포트
    [MenuItem("Tools/Data/Import All CSV Tables")]
    public static void ImportAllTables()
    {
        // 누락된 SO 스크립트가 있으면 생성 후 컴파일 완료 시 자동 재개
        if (!CsvImportCompileBridge.EnsureSoScriptsReadyAndMaybeDefer("Import All CSV Tables"))
            return;

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
        EditorUtility.DisplayDialog("Import Complete", $"Success: {successCount}\nFailed: {failCount}", "OK");
    }

    // 선택된 에셋이 CSV 폴더의 csv 파일인지 검사
    [MenuItem("Assets/Data/Import Selected CSV", true)]
    static bool ValidateImportSelected()
    {
        var assetPath = AssetDatabase.GetAssetPath(Selection.activeObject);
        return assetPath.StartsWith($"{CsvImportUtil.CsvFolder}/", StringComparison.OrdinalIgnoreCase) &&
               string.Equals(Path.GetExtension(assetPath), ".csv", StringComparison.OrdinalIgnoreCase);
    }

    // 프로젝트 창에서 선택한 CSV를 임포트
    [MenuItem("Assets/Data/Import Selected CSV")]
    static void ImportSelected()
    {
        ImportAndSave(AssetDatabase.GetAssetPath(Selection.activeObject));
    }

    // 임포트 후 에셋 DB를 저장하고 갱신
    static void ImportAndSave(string csvPath)
    {
        var assetPath = CsvImportUtil.Import(csvPath);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log($"[CsvBatchImporter] {Path.GetFileName(csvPath)} -> {assetPath}");
    }
}
#endif
