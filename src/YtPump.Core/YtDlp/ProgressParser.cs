using System.Globalization;

namespace YtPump.Core.YtDlp;

public readonly record struct DownloadProgress(
    double? Percent,
    string? Speed,
    string? Eta,
    string? Downloaded,
    string? Total);

public static class ProgressParser
{
    public const string ProgressPrefix = "YTP|";
    public const string FilePrefix = "YTPFILE:";

    public static bool TryParseProgress(string? line, out DownloadProgress progress)
    {
        progress = default;
        var payload = StripPrefix(line, ProgressPrefix);
        if (payload is null)
        {
            return false;
        }

        var parts = payload.Split('|');
        if (parts.Length < 5)
        {
            return false;
        }

        progress = new DownloadProgress(
            Percent: ParsePercent(parts[4]),
            Speed: Clean(parts[3]),
            Eta: Clean(parts[2]),
            Downloaded: Clean(parts[0]),
            Total: Clean(parts[1]));
        return true;
    }

    public static bool TryParseOutputPath(string? line, out string path)
    {
        path = "";
        var payload = StripPrefix(line, FilePrefix);
        if (payload is null)
        {
            return false;
        }

        path = payload.Trim().Trim('"');
        return path.Length > 0;
    }

    private static string? StripPrefix(string? line, string prefix)
    {
        if (string.IsNullOrWhiteSpace(line))
        {
            return null;
        }

        var trimmed = line.Trim();
        var index = trimmed.IndexOf(prefix, StringComparison.Ordinal);
        if (index < 0)
        {
            return null;
        }

        return trimmed[(index + prefix.Length)..];
    }

    private static double? ParsePercent(string raw)
    {
        var cleaned = raw.Trim().Trim('%').Replace(',', '.');
        if (cleaned.Length == 0 || IsNa(cleaned))
        {
            return null;
        }

        return double.TryParse(cleaned, NumberStyles.Float, CultureInfo.InvariantCulture, out var value)
            ? value
            : null;
    }

    private static string? Clean(string raw)
    {
        var trimmed = raw.Trim();
        return trimmed.Length == 0 || IsNa(trimmed) ? null : trimmed;
    }

    private static bool IsNa(string value) =>
        value.Equals("NA", StringComparison.OrdinalIgnoreCase)
        || value.Equals("N/A", StringComparison.OrdinalIgnoreCase)
        || value.Equals("None", StringComparison.OrdinalIgnoreCase);
}
