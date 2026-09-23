using System.ComponentModel;
using System.Diagnostics;
using System.Text;

namespace YtPump.Core.YtDlp;

public sealed record YtDlpResult(int ExitCode, string StandardOutput, string StandardError, bool Canceled);

public sealed class YtDlpProcess
{
    private static readonly TimeSpan KillWait = TimeSpan.FromSeconds(3);

    public async Task<YtDlpResult> RunAsync(
        string exePath,
        IReadOnlyList<string> arguments,
        IProgress<string>? stderrProgress,
        CancellationToken cancellationToken,
        IProgress<string>? stdoutProgress = null)
    {
        if (!File.Exists(exePath))
        {
            throw new FileNotFoundException("yt-dlp executable not found.", exePath);
        }

        var startInfo = new ProcessStartInfo
        {
            FileName = exePath,
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            RedirectStandardInput = true,
            CreateNoWindow = true,
            StandardOutputEncoding = Encoding.UTF8,
            StandardErrorEncoding = Encoding.UTF8,
            WorkingDirectory = Path.GetDirectoryName(exePath) ?? Environment.CurrentDirectory
        };
        startInfo.Environment["PYTHONUTF8"] = "1";
        foreach (var argument in arguments)
        {
            startInfo.ArgumentList.Add(argument);
        }

        using var process = new Process { StartInfo = startInfo };
        process.Start();
        using var job = KillOnCloseJob.TryAssign(process);
        try
        {
            process.StandardInput.Close();
        }
        catch (IOException)
        {
        }

        var stdoutTask = ReadLinesAsync(process.StandardOutput, stdoutProgress);
        var stderrTask = ReadLinesAsync(process.StandardError, stderrProgress);

        try
        {
            await process.WaitForExitAsync(cancellationToken).ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
            await TryKillAsync(process).ConfigureAwait(false);
            return new YtDlpResult(
                -1,
                await SafeRead(stdoutTask).ConfigureAwait(false),
                await SafeRead(stderrTask).ConfigureAwait(false),
                Canceled: true);
        }

        return new YtDlpResult(
            process.ExitCode,
            await stdoutTask.ConfigureAwait(false),
            await stderrTask.ConfigureAwait(false),
            Canceled: false);
    }

    private static async Task<string> ReadLinesAsync(StreamReader reader, IProgress<string>? progress)
    {
        var builder = new StringBuilder();
        while (await reader.ReadLineAsync().ConfigureAwait(false) is { } line)
        {
            builder.AppendLine(line);
            progress?.Report(line);
        }

        return builder.ToString();
    }

    private static async Task<string> SafeRead(Task<string> task)
    {
        try
        {
            return await task.WaitAsync(TimeSpan.FromSeconds(2)).ConfigureAwait(false);
        }
        catch (Exception ex) when (ex is TimeoutException or IOException or OperationCanceledException)
        {
            return "";
        }
    }

    private static async Task TryKillAsync(Process process)
    {
        try
        {
            if (!process.HasExited)
            {
                process.Kill(entireProcessTree: true);
            }

            await process.WaitForExitAsync().WaitAsync(KillWait).ConfigureAwait(false);
        }
        catch (InvalidOperationException)
        {
        }
        catch (Win32Exception)
        {
        }
        catch (TimeoutException)
        {
        }
    }
}
