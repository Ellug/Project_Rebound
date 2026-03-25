#if UNITY_EDITOR
using System;
using System.IO;
using System.Text;
using UnityEngine;

internal static class GoogleSheetCloudVersionStore
{
    // 다음 버전 라벨 계산
    public static string GetNextVersionLabel()
    {
        int major = Mathf.Max(0, GoogleSheetCloudUploadConfig.VersionMajor);
        int minor = Mathf.Max(0, GoogleSheetCloudUploadConfig.VersionMinor);
        int patch = 0;

        string path = GetVersionStatePath();
        if (File.Exists(path))
        {
            try
            {
                var saved = JsonUtility.FromJson<VersionState>(File.ReadAllText(path, new UTF8Encoding(false)));
                if (saved != null && saved.major == major && saved.minor == minor)
                    patch = Mathf.Max(0, saved.patch + 1);
            }
            catch
            {
                patch = 0;
            }
        }

        return $"v{major}.{minor}.{patch}";
    }

    // 버전 라벨 상태 저장
    public static bool TrySaveVersionLabel(string versionLabel, out string error)
    {
        if (!TryParseVersion(versionLabel, out int major, out int minor, out int patch))
        {
            error = $"버전 라벨 파싱 실패: {versionLabel}";
            return false;
        }

        string path = GetVersionStatePath();
        try
        {
            string dir = Path.GetDirectoryName(path);
            if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
                Directory.CreateDirectory(dir);

            var state = new VersionState { major = major, minor = minor, patch = patch };
            File.WriteAllText(path, JsonUtility.ToJson(state, true), new UTF8Encoding(false));
            error = string.Empty;
            return true;
        }
        catch (Exception e)
        {
            error = $"Path: {path}\n{e.Message}";
            return false;
        }
    }

    // 버전 상태 파일 경로 계산
    private static string GetVersionStatePath()
    {
        string configured = (GoogleSheetCloudUploadConfig.VersionStateFilePath ?? string.Empty).Trim();
        if (string.IsNullOrEmpty(configured))
            configured = "ProjectSettings/GoogleSheetCloudVersionState.json";

        string projectRoot = Path.GetFullPath(Path.Combine(Application.dataPath, ".."));
        return Path.IsPathRooted(configured)
            ? Path.GetFullPath(configured)
            : Path.GetFullPath(Path.Combine(projectRoot, configured));
    }

    // vMajor.Minor.Patch 라벨 파싱
    private static bool TryParseVersion(string label, out int major, out int minor, out int patch)
    {
        major = 0;
        minor = 0;
        patch = 0;

        if (string.IsNullOrWhiteSpace(label))
            return false;

        string text = label.Trim();
        if (!text.StartsWith("v", StringComparison.OrdinalIgnoreCase))
            return false;

        string[] parts = text.Substring(1).Split('.');
        if (parts.Length != 3)
            return false;

        return int.TryParse(parts[0], out major) &&
               int.TryParse(parts[1], out minor) &&
               int.TryParse(parts[2], out patch);
    }

    [Serializable]
    // 버전 상태 모델
    private sealed class VersionState
    {
        public int major;
        public int minor;
        public int patch;
    }
}
#endif
