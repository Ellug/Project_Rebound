#if UNITY_EDITOR
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.AddressableAssets;
using UnityEditor.AddressableAssets.Settings;
using UnityEngine;
using UnityEngine.AddressableAssets;

public static class TableLoadConfigAutoSync
{
    // 자동 생성/갱신할 테이블 로드 설정 에셋 경로
    private static readonly string ConfigFolder = $"{CsvImportUtil.SoFolder}/Config";
    private static readonly string ConfigAssetPath = $"{CsvImportUtil.SoFolder}/Config/TableLoadConfig.asset";

    // CSV와 SO를 기준으로 TableLoadConfig와 Addressables 엔트리를 동기화
    public static void Sync(bool showDialog)
    {
        EnsureFolder(ConfigFolder);

        var config = AssetDatabase.LoadAssetAtPath<TableLoadConfigSO>(ConfigAssetPath);
        if (config == null)
        {
            config = ScriptableObject.CreateInstance<TableLoadConfigSO>();
            AssetDatabase.CreateAsset(config, ConfigAssetPath);
        }

        var refs = new List<AssetReference>();
        var missingSoAssets = new List<string>();
        int addressableAdded = 0;

        AddressableAssetSettings settings = AddressableAssetSettingsDefaultObject.Settings;
        var defaultGroup = settings != null ? settings.DefaultGroup : null;

        var csvPaths = CsvImportUtil.FindCsvPaths();
        for (int i = 0; i < csvPaths.Length; i++)
        {
            string tableName = Path.GetFileNameWithoutExtension(csvPaths[i]);
            string soAssetPath = $"{CsvImportUtil.SoFolder}/SO_{tableName}.asset";
            string guid = AssetDatabase.AssetPathToGUID(soAssetPath);

            if (string.IsNullOrEmpty(guid))
            {
                missingSoAssets.Add($"SO_{tableName}.asset");
                continue;
            }

            if (settings != null && defaultGroup != null)
            {
                var entry = settings.FindAssetEntry(guid);
                if (entry == null)
                {
                    entry = settings.CreateOrMoveEntry(guid, defaultGroup, false, false);
                    addressableAdded++;
                }

                if (entry != null)
                    entry.address = tableName;
            }

            refs.Add(new AssetReference(guid));
        }

        config.ReplaceAll(refs);
        EditorUtility.SetDirty(config);
        if (settings != null)
            EditorUtility.SetDirty(settings);

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        if (!showDialog) return;

        string message = $"Tables: {refs.Count}\n" +
                        $"Addressable Added: {addressableAdded}\n" +
                        $"Missing SO: {missingSoAssets.Count}";

        if (missingSoAssets.Count > 0)
            message += $"\n\n{string.Join("\n", missingSoAssets)}";

        EditorUtility.DisplayDialog("Sync Table Load Config", message, "OK");
    }

    // 경로에 필요한 폴더가 없으면 순차적으로 생성
    private static void EnsureFolder(string folderPath)
    {
        if (AssetDatabase.IsValidFolder(folderPath))
            return;

        var parts = folderPath.Split('/');
        string current = parts[0];

        for (int i = 1; i < parts.Length; i++)
        {
            string next = $"{current}/{parts[i]}";
            if (!AssetDatabase.IsValidFolder(next))
                AssetDatabase.CreateFolder(current, parts[i]);
            current = next;
        }
    }
}
#endif
