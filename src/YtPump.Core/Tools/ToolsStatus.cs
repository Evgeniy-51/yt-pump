namespace YtPump.Core.Tools;

public sealed record ToolsStatus(
    string AppDirectory,
    string ToolsDirectory,
    string YtDlpPath,
    string FfmpegPath,
    string FfprobePath,
    string DenoPath)
{
    public bool YtDlpExists => File.Exists(YtDlpPath);
    public bool FfmpegExists => File.Exists(FfmpegPath);
    public bool FfprobeExists => File.Exists(FfprobePath);
    public bool DenoExists => File.Exists(DenoPath);
    public bool AllPresent => YtDlpExists && FfmpegExists && FfprobeExists && DenoExists;

    public IReadOnlyList<string> MissingFileNames
    {
        get
        {
            var missing = new List<string>(4);
            if (!YtDlpExists)
            {
                missing.Add(ToolsLocator.YtDlpFileName);
            }

            if (!FfmpegExists)
            {
                missing.Add(ToolsLocator.FfmpegFileName);
            }

            if (!FfprobeExists)
            {
                missing.Add(ToolsLocator.FfprobeFileName);
            }

            if (!DenoExists)
            {
                missing.Add(ToolsLocator.DenoFileName);
            }

            return missing;
        }
    }
}
