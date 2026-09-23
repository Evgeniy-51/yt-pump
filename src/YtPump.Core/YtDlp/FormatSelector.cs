namespace YtPump.Core.YtDlp;

public static class FormatSelector
{
    public static string Build(string container, string? qualityId, string? audioId)
    {
        var height = ParseHeight(qualityId);
        var language = ParseLanguage(audioId);
        var heightFilter = height is int h ? $"[height<={h}]" : "";
        var languageFilter = language is string lang ? $"[language={lang}]" : "";

        return container switch
        {
            "audio" => language is null
                ? "ba[ext=m4a]/ba/b"
                : $"ba{languageFilter}[ext=m4a]/ba{languageFilter}/ba",
            "mp4" => BuildMp4(heightFilter, languageFilter),
            _ => BuildMkv(heightFilter, languageFilter)
        };
    }

    public static int? ParseHeight(string? qualityId)
    {
        if (string.IsNullOrWhiteSpace(qualityId) || qualityId is "best" or "pending")
        {
            return null;
        }

        return int.TryParse(qualityId, out var height) && height > 0 ? height : null;
    }

    public static string? ParseLanguage(string? audioId)
    {
        if (string.IsNullOrWhiteSpace(audioId) || audioId == "pending")
        {
            return null;
        }

        var pipe = audioId.IndexOf('|');
        var language = pipe < 0 ? audioId : audioId[..pipe];
        if (string.IsNullOrWhiteSpace(language) || language.Equals("und", StringComparison.OrdinalIgnoreCase))
        {
            return null;
        }

        return language;
    }

    private static string BuildMkv(string heightFilter, string languageFilter)
    {
        if (languageFilter.Length > 0)
        {
            return $"bv*{heightFilter}+ba{languageFilter}/bv*{heightFilter}+ba/b";
        }

        return $"bv*{heightFilter}+ba/b";
    }

    private static string BuildMp4(string heightFilter, string languageFilter)
    {
        if (languageFilter.Length > 0)
        {
            return $"bv*[vcodec^=avc]{heightFilter}+ba[acodec^=mp4a]{languageFilter}/bv*{heightFilter}+ba{languageFilter}/bv*{heightFilter}+ba/b[ext=mp4]/b";
        }

        return $"bv*[vcodec^=avc]{heightFilter}+ba[acodec^=mp4a]/bv*{heightFilter}+ba/b[ext=mp4]/b";
    }
}
