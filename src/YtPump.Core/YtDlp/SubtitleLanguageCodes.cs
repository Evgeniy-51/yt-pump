using System.Text.Json;

namespace YtPump.Core.YtDlp;

public static class SubtitleLanguageCodes
{
    public const int RadioLimit = 8;

    public static string? Normalize(string? key)
    {
        if (string.IsNullOrWhiteSpace(key))
        {
            return null;
        }

        var code = key.Trim().ToLowerInvariant();
        if (code.StartsWith("a.", StringComparison.Ordinal))
        {
            code = code[2..];
        }

        var dash = code.IndexOf('-');
        if (dash >= 0)
        {
            code = code[..dash];
        }

        if (code.Length is < 2 or > 3)
        {
            return null;
        }

        foreach (var ch in code)
        {
            if (ch is < 'a' or > 'z')
            {
                return null;
            }
        }

        return code;
    }

    public static IReadOnlyList<string> FromVideo(JsonElement video)
    {
        var codes = new HashSet<string>(StringComparer.Ordinal);
        AddMap(video, "subtitles", codes);
        AddMap(video, "automatic_captions", codes);
        return codes
            .OrderBy(Rank)
            .ThenBy(code => code, StringComparer.Ordinal)
            .ToArray();
    }

    private static void AddMap(JsonElement video, string property, HashSet<string> codes)
    {
        if (!video.TryGetProperty(property, out var map) || map.ValueKind != JsonValueKind.Object)
        {
            return;
        }

        foreach (var item in map.EnumerateObject())
        {
            if (item.Value.ValueKind != JsonValueKind.Array || item.Value.GetArrayLength() == 0)
            {
                continue;
            }

            var code = Normalize(item.Name);
            if (code != null)
            {
                codes.Add(code);
            }
        }
    }

    private static int Rank(string code) => code switch
    {
        "ru" => 0,
        "en" => 1,
        _ => 2
    };
}
