namespace YtPump.Core.Tools;

public static class ToolsLocator
{
    public const string YtDlpFileName = "yt-dlp.exe";
    public const string FfmpegFileName = "ffmpeg.exe";
    public const string FfprobeFileName = "ffprobe.exe";
    public const string DenoFileName = "deno.exe";
    public const string ToolsFolderName = "tools";

    /// <summary>
    /// Directory of the running executable. Do not use AppContext.BaseDirectory
    /// (single-file extract dir).
    /// </summary>
    public static string GetAppDirectory()
    {
        var path = Environment.ProcessPath;
        if (string.IsNullOrWhiteSpace(path))
        {
            // Designer / host without a process path. Runtime always has ProcessPath.
            return Path.GetFullPath(AppContext.BaseDirectory);
        }

        var dir = Path.GetDirectoryName(path);
        if (string.IsNullOrWhiteSpace(dir))
        {
            throw new InvalidOperationException("Cannot resolve application directory.");
        }

        return dir;
    }

    public static ToolsStatus Check(string? appDirectory = null)
    {
        var appDir = string.IsNullOrWhiteSpace(appDirectory)
            ? GetAppDirectory()
            : Path.GetFullPath(appDirectory);

        var toolsDir = Path.Combine(appDir, ToolsFolderName);
        return new ToolsStatus(
            AppDirectory: appDir,
            ToolsDirectory: toolsDir,
            YtDlpPath: Path.Combine(toolsDir, YtDlpFileName),
            FfmpegPath: Path.Combine(toolsDir, FfmpegFileName),
            FfprobePath: Path.Combine(toolsDir, FfprobeFileName),
            DenoPath: Path.Combine(toolsDir, DenoFileName));
    }
}
