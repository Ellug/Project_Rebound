#if UNITY_EDITOR
using System;
using System.Diagnostics;
using System.IO;
using System.Text;

internal static class GoogleSheetUgsCli
{
    // UGS CLI 실행 파일 빠른 확인
    public static bool TryValidateExecutable(out string output)
        => Run("--version", out output, 10);

    // UGS config 값 조회
    public static bool TryGetConfig(string key, out string value)
    {
        if (!Run($"config get {key}", out string output, 15))
        {
            value = string.Empty;
            return false;
        }

        value = LastValueLine(output);
        return !string.IsNullOrWhiteSpace(value);
    }

    // UGS CLI 명령 실행
    public static bool Run(string arguments, out string output, int timeoutSeconds = -1)
    {
        var stdout = new StringBuilder();
        var stderr = new StringBuilder();
        output = string.Empty;

        string exe = GoogleSheetCloudUploadConfig.UgsExecutable;
        bool isBatch = exe.EndsWith(".cmd", StringComparison.OrdinalIgnoreCase) ||
                       exe.EndsWith(".bat", StringComparison.OrdinalIgnoreCase);

        string fileName;
        string startArgs;
        if (isBatch)
        {
            // cmd /s /c ""ugs.cmd" ..."" 형태로 감싸야 인용부호 포함 인자 전달이 안정적
            string shell = Environment.GetEnvironmentVariable("ComSpec");
            if (string.IsNullOrWhiteSpace(shell))
                shell = "cmd.exe";

            fileName = shell;
            startArgs = $"/d /s /c \"\"{exe}\" {arguments}\"";
        }
        else
        {
            fileName = exe;
            startArgs = arguments;
        }

        var startInfo = new ProcessStartInfo
        {
            FileName = fileName,
            Arguments = startArgs,
            WorkingDirectory = Path.GetFullPath(Path.Combine(UnityEngine.Application.dataPath, "..")),
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            CreateNoWindow = true,
        };

        using var process = new Process { StartInfo = startInfo };
        process.OutputDataReceived += (_, e) => { if (e.Data != null) stdout.AppendLine(e.Data); };
        process.ErrorDataReceived += (_, e) => { if (e.Data != null) stderr.AppendLine(e.Data); };

        try
        {
            process.Start();
            process.BeginOutputReadLine();
            process.BeginErrorReadLine();
        }
        catch (Exception e)
        {
            output = e.Message;
            return false;
        }

        int limit = timeoutSeconds > 0 ? timeoutSeconds : GoogleSheetCloudUploadConfig.UgsCommandTimeoutSeconds;
        if (!process.WaitForExit(Math.Max(1, limit) * 1000))
        {
            try { process.Kill(); } catch { }
            output = $"timeout: {GoogleSheetCloudUploadConfig.UgsExecutable} {arguments}";
            return false;
        }

        process.WaitForExit();

        string outText = stdout.ToString().Trim();
        string errText = stderr.ToString().Trim();
        output = string.IsNullOrWhiteSpace(errText) ? outText : $"{outText}\n{errText}".Trim();
        return process.ExitCode == 0;
    }

    // CLI 인자 따옴표 감싸기
    public static string QuoteArg(string value) => $"\"{(value ?? string.Empty).Replace("\"", "\\\"")}\"";

    // CLI 출력 마지막 유효 값 추출
    private static string LastValueLine(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
            return string.Empty;

        string[] lines = text.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
        for (int i = lines.Length - 1; i >= 0; i--)
        {
            string line = lines[i].Trim();
            if (line.Length == 0) continue;
            if (line.StartsWith("[", StringComparison.Ordinal)) continue;
            return line;
        }
        return string.Empty;
    }
}
#endif
