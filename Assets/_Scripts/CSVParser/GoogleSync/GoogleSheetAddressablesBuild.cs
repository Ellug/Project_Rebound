#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEditor.AddressableAssets;
using UnityEditor.AddressableAssets.Build;
using UnityEditor.AddressableAssets.Settings;

internal static class GoogleSheetAddressablesBuild
{
    // Addressables 빌드 + 원격 출력 폴더 확보
    public static bool TryBuildAndGetRemoteOutput(out string remoteBuildDir, out int fileCount, out string error)
    {
        remoteBuildDir = string.Empty;
        fileCount = 0;
        error = string.Empty;

        if (!TryResolveRemoteBuildDirectory(out remoteBuildDir, out error))
            return false;

        EditorUtility.DisplayProgressBar("Cloud Upload", "Building Addressables...", 0.35f);

        AddressableAssetSettings.BuildPlayerContent(out AddressablesPlayerBuildResult buildResult);
        if (!string.IsNullOrEmpty(buildResult.Error))
        {
            error = $"Addressables build failed.\n{buildResult.Error}";
            return false;
        }

        if (!Directory.Exists(remoteBuildDir))
        {
            error = $"빌드 폴더 없음: {remoteBuildDir}";
            return false;
        }

        fileCount = Directory.GetFiles(remoteBuildDir, "*", SearchOption.AllDirectories).Length;
        if (fileCount <= 0)
        {
            error = $"업로드할 파일 없음: {remoteBuildDir}";
            return false;
        }

        return true;
    }

    // 활성 프로필 Remote.BuildPath 절대 경로 변환
    private static bool TryResolveRemoteBuildDirectory(out string fullPath, out string error)
    {
        AddressableAssetSettings settings = AddressableAssetSettingsDefaultObject.Settings;
        if (settings == null)
        {
            fullPath = string.Empty;
            error = "Addressables settings 없음";
            return false;
        }

        string profileId = settings.activeProfileId;
        string rawBuildPath = settings.profileSettings.GetValueByName(profileId, AddressableAssetSettings.kRemoteBuildPath);
        string evaluatedBuildPath = settings.profileSettings.EvaluateString(profileId, rawBuildPath);
        if (string.IsNullOrWhiteSpace(evaluatedBuildPath))
        {
            fullPath = string.Empty;
            error = "Remote.BuildPath 해석 실패";
            return false;
        }

        string projectRoot = Path.GetFullPath(Path.Combine(UnityEngine.Application.dataPath, ".."));
        fullPath = Path.IsPathRooted(evaluatedBuildPath)
            ? Path.GetFullPath(evaluatedBuildPath)
            : Path.GetFullPath(Path.Combine(projectRoot, evaluatedBuildPath));
        error = string.Empty;
        return true;
    }
}
#endif
