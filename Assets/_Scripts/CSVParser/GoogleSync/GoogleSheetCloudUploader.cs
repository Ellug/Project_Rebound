#if UNITY_EDITOR
using System;
using System.IO;
using System.Text;
using UnityEditor;
using Debug = UnityEngine.Debug;

public static class GoogleSheetCloudUploader
{
    private const string ProjectIdKey = "project-id";
    private const string EnvironmentNameKey = "environment-name";
    private const string BucketNameKey = "bucket-name";

    [MenuItem("Tools/Data/Upload Addressables to CCD")]
    // 메뉴 수동 업로드 실행
    public static void UploadFromMenu()
    {
        if (!TryValidateUgsCliForPipeline(out string reason))
        {
            EditorUtility.DisplayDialog("UGS CLI 확인 실패", reason, "확인");
            return;
        }

        var result = BuildAndUploadAddressables();
        string title = result.Success ? "CCD Upload Complete" : (result.Skipped ? "CCD Upload Skipped" : "CCD Upload Failed");
        EditorUtility.DisplayDialog(title, result.Message, "OK");
    }

    // Syncer 시작 전 UGS CLI 실행 가능 여부 확인
    public static bool TryValidateUgsCliForPipeline(out string message)
    {
        // 실행 파일 빠른 확인
        if (GoogleSheetUgsCli.TryValidateExecutable(out _))
        {
            message = string.Empty;
            return true;
        }

        message = "UGS CLI를 찾지 못했거나 실행할 수 없어 Syncer를 차단함.\n\n" +
                $"현재 설정값: {GoogleSheetCloudUploadConfig.UgsExecutable}\n\n" +
                "조치:\n" +
                "1) UGS CLI 설치 + PATH 등록\n" +
                $"2) {GoogleSheetCloudUploadConfig.UgsExecutable} --version 확인\n" +
                $"3) {GoogleSheetCloudUploadConfig.UgsExecutable} login 및 config 설정";
        return false;
    }

    // Addressables 빌드 -> CCD 업로드 순차 실행
    public static GoogleSheetCloudUploadResult BuildAndUploadAddressables()
    {
        try
        {
            // UGS 설정값 읽기
            if (!GoogleSheetUgsCli.TryGetConfig(ProjectIdKey, out string projectId))
                return GoogleSheetCloudUploadResult.Fail($"UGS config 누락: {ProjectIdKey}");
            if (!GoogleSheetUgsCli.TryGetConfig(EnvironmentNameKey, out string environmentName))
                return GoogleSheetCloudUploadResult.Fail($"UGS config 누락: {EnvironmentNameKey}");
            if (!GoogleSheetUgsCli.TryGetConfig(BucketNameKey, out string bucketName))
                return GoogleSheetCloudUploadResult.Fail($"UGS config 누락: {BucketNameKey}");

            // Addressables 빌드 결과 확보
            EditorUtility.DisplayProgressBar("클라우드 업로드", "로딩 중... (Addressables 빌드)", 0.25f);
            if (!GoogleSheetAddressablesBuild.TryBuildAndGetRemoteOutput(
                    out string remoteBuildDir,
                    out int fileCount,
                    out string buildError))
            {
                return GoogleSheetCloudUploadResult.Fail(buildError);
            }

            // 릴리즈 메타 구성
            string releaseNotes = string.Empty;
            string versionLabel = string.Empty;

            if (GoogleSheetCloudUploadConfig.CreateReleaseOnSync)
            {
                if (GoogleSheetCloudUploadConfig.UseAutoSemanticVersion)
                {
                    versionLabel = GoogleSheetCloudVersionStore.GetNextVersionLabel();
                    releaseNotes = versionLabel;
                }
                else
                {
                    string prefix = (GoogleSheetCloudUploadConfig.ReleaseNotesPrefix ?? "sheet_sync").Trim();
                    if (string.IsNullOrWhiteSpace(prefix))
                        prefix = "sheet_sync";
                    releaseNotes = $"{prefix.Replace(' ', '_')}_{DateTime.Now:yyyyMMdd_HHmmss}";
                }
            }

            EditorUtility.DisplayProgressBar("클라우드 업로드", "로딩 중... (CCD 업로드)", 0.75f);

            // ccd sync 명령 구성
            string syncPathArg = ResolveSyncPathArg(remoteBuildDir);
            var args = new StringBuilder(256)
                .Append("ccd entries sync ").Append(GoogleSheetUgsCli.QuoteArg(syncPathArg))
                .Append(" --project-id ").Append(GoogleSheetUgsCli.QuoteArg(projectId))
                .Append(" --environment-name ").Append(GoogleSheetUgsCli.QuoteArg(environmentName))
                .Append(" --bucket-name ").Append(GoogleSheetUgsCli.QuoteArg(bucketName));

            if (GoogleSheetCloudUploadConfig.DeleteMissingEntries)
                args.Append(" --delete");

            if (GoogleSheetCloudUploadConfig.CreateReleaseOnSync)
            {
                args.Append(" --create-release")
                    .Append(" --release-notes ").Append(GoogleSheetUgsCli.QuoteArg(releaseNotes));
            }

            if (!GoogleSheetUgsCli.Run(args.ToString(), out string syncOutput))
            {
                Debug.LogError($"[GoogleSheetCloudUploader] CCD sync failed.\n{syncOutput}");
                return GoogleSheetCloudUploadResult.Fail("CCD sync 실패 (Console 확인)");
            }

            // 업로드 성공 후 patch 상태 저장
            if (!string.IsNullOrEmpty(versionLabel) &&
                !GoogleSheetCloudVersionStore.TrySaveVersionLabel(versionLabel, out string saveError))
            {
                Debug.LogWarning($"[GoogleSheetCloudUploader] 버전 상태 저장 실패\n{saveError}");
            }

            string msg =
                "Addressables build + CCD upload complete.\n\n" +
                $"Project: {projectId}\n" +
                $"Environment: {environmentName}\n" +
                $"Bucket: {bucketName}\n" +
                $"Local Folder: {remoteBuildDir}\n" +
                $"Uploaded Files: {fileCount}";

            if (!string.IsNullOrEmpty(versionLabel))
                msg += $"\nRelease Version: {versionLabel}";

            return GoogleSheetCloudUploadResult.Ok(msg);
        }
        finally
        {
            EditorUtility.ClearProgressBar();
        }
    }

    // ugs sync 경로 인자 계산
    private static string ResolveSyncPathArg(string absoluteBuildDir)
    {
        // npm ugs.cmd shim이 공백 포함 절대경로 인자 전달을 깨뜨리는 경우가 있어 상대경로 우선 사용
        string projectRoot = Path.GetFullPath(Path.Combine(UnityEngine.Application.dataPath, ".."));
        string fullRoot = Path.GetFullPath(projectRoot).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
        string fullTarget = Path.GetFullPath(absoluteBuildDir).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);

        if (fullTarget.StartsWith(fullRoot + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase) ||
            string.Equals(fullTarget, fullRoot, StringComparison.OrdinalIgnoreCase))
        {
            string relative = Path.GetRelativePath(fullRoot, fullTarget);
            return relative.Replace('/', Path.DirectorySeparatorChar);
        }

        return fullTarget;
    }
}

public readonly struct GoogleSheetCloudUploadResult
{
    public readonly bool Success;
    public readonly bool Skipped;
    public readonly string Message;

    private GoogleSheetCloudUploadResult(bool success, bool skipped, string message)
    {
        Success = success;
        Skipped = skipped;
        Message = message ?? string.Empty;
    }

    public static GoogleSheetCloudUploadResult Ok(string message) => new GoogleSheetCloudUploadResult(true, false, message);
    public static GoogleSheetCloudUploadResult Skip(string message) => new GoogleSheetCloudUploadResult(false, true, message);
    public static GoogleSheetCloudUploadResult Fail(string message) => new GoogleSheetCloudUploadResult(false, false, message);
}
#endif
