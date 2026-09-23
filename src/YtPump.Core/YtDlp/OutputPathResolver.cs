namespace YtPump.Core.YtDlp;

public static class OutputPathResolver
{
    private static readonly HashSet<string> MediaExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".mkv", ".mp4", ".webm", ".m4a", ".mp3", ".opus", ".ogg", ".m4v", ".mov", ".aac", ".flac", ".wav"
    };

    public static string? Resolve(
        IReadOnlyList<string> candidates,
        string? outputDirectory,
        string? videoId)
    {
        for (var i = candidates.Count - 1; i >= 0; i--)
        {
            var path = candidates[i];
            if (!string.IsNullOrWhiteSpace(path) && File.Exists(path))
            {
                return Path.GetFullPath(path);
            }
        }

        if (string.IsNullOrWhiteSpace(outputDirectory)
            || string.IsNullOrWhiteSpace(videoId)
            || !Directory.Exists(outputDirectory))
        {
            return null;
        }

        var needle = "[" + videoId + "]";
        FileInfo[] matches;
        try
        {
            matches = new DirectoryInfo(outputDirectory)
                .EnumerateFiles("*", SearchOption.TopDirectoryOnly)
                .Where(file => file.Name.Contains(needle, StringComparison.OrdinalIgnoreCase)
                               && !IsTempSidecar(file))
                .ToArray();
        }
        catch (IOException)
        {
            return null;
        }

        if (matches.Length == 0)
        {
            return null;
        }

        var media = matches.Where(file => MediaExtensions.Contains(file.Extension)).ToArray();
        var pick = (media.Length > 0 ? media : matches)
            .OrderByDescending(file => file.LastWriteTimeUtc)
            .First();
        return pick.FullName;
    }

    private static bool IsTempSidecar(FileInfo file) =>
        file.Extension.Equals(".part", StringComparison.OrdinalIgnoreCase)
        || file.Extension.Equals(".ytdl", StringComparison.OrdinalIgnoreCase)
        || file.Name.EndsWith(".part", StringComparison.OrdinalIgnoreCase);
}
