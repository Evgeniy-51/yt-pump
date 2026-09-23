using System.Text;

namespace YtPump.Core.Validation;

public static class FileNameSanitizer
{
    public const int MaxStemLength = 180;

    private static readonly HashSet<string> Reserved =
    [
        "CON", "PRN", "AUX", "NUL",
        "COM1", "COM2", "COM3", "COM4", "COM5", "COM6", "COM7", "COM8", "COM9",
        "LPT1", "LPT2", "LPT3", "LPT4", "LPT5", "LPT6", "LPT7", "LPT8", "LPT9"
    ];

    public static string Sanitize(string? name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            return "";
        }

        var buffer = new StringBuilder(name.Length);
        foreach (var ch in name.Trim())
        {
            if (ch < 32 || ch is '<' or '>' or ':' or '"' or '/' or '\\' or '|' or '?' or '*')
            {
                continue;
            }

            buffer.Append(ch);
        }

        var stem = buffer.ToString().Trim().TrimEnd('.');
        while (stem.Contains("  ", StringComparison.Ordinal))
        {
            stem = stem.Replace("  ", " ", StringComparison.Ordinal);
        }

        if (Reserved.Contains(stem.ToUpperInvariant()))
        {
            stem += "_";
        }

        if (stem.Length > MaxStemLength)
        {
            stem = stem[..MaxStemLength].TrimEnd().TrimEnd('.');
        }

        return stem;
    }

    public static string EscapeYtDlpTemplate(string literal) =>
        (literal ?? "").Replace("%", "%%", StringComparison.Ordinal);

    public static string OutputTemplate(string? fileStem)
    {
        var stem = Sanitize(fileStem);
        if (stem.Length == 0)
        {
            return "%(title)s [%(id)s].%(ext)s";
        }

        return $"{EscapeYtDlpTemplate(stem)} [%(id)s].%(ext)s";
    }
}
