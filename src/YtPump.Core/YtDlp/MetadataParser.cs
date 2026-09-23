using System.Text.Json;

namespace YtPump.Core.YtDlp;

public static class MetadataParser
{
    public static VideoInfo Parse(string raw)
    {
        var json = ExtractJsonObject(raw);
        using var doc = JsonDocument.Parse(json);
        var video = UnwrapPlaylistEntry(doc.RootElement);

        var id = GetString(video, "id") ?? "";
        var title = GetString(video, "title") ?? id;
        var heights = new SortedSet<int>(Comparer<int>.Create((a, b) => b.CompareTo(a)));
        var audio = new Dictionary<string, AudioTrackInfo>(StringComparer.Ordinal);

        if (video.TryGetProperty("formats", out var formats) && formats.ValueKind == JsonValueKind.Array)
        {
            foreach (var format in formats.EnumerateArray())
            {
                if (format.ValueKind != JsonValueKind.Object)
                {
                    continue;
                }

                var vcodec = GetString(format, "vcodec");
                var acodec = GetString(format, "acodec");
                var height = GetHeight(format);
                if (!IsNone(vcodec) && height > 0)
                {
                    heights.Add(height);
                }

                if (IsNone(acodec))
                {
                    continue;
                }

                var language = GetString(format, "language");
                var display = (string?)null;
                var isOriginal = false;
                if (format.TryGetProperty("audio_track", out var track) && track.ValueKind == JsonValueKind.Object)
                {
                    display = GetString(track, "display_name");
                    if (track.TryGetProperty("is_original", out var originalFlag)
                        && originalFlag.ValueKind == JsonValueKind.True)
                    {
                        isOriginal = true;
                    }
                }

                var note = GetString(format, "format_note") ?? "";
                if (note.Contains("original", StringComparison.OrdinalIgnoreCase))
                {
                    isOriginal = true;
                }

                var key = AudioKey(language, isOriginal);
                if (!audio.ContainsKey(key))
                {
                    audio[key] = new AudioTrackInfo
                    {
                        Id = key,
                        Language = language,
                        IsOriginal = isOriginal,
                        DisplayName = display
                    };
                }
            }
        }

        var tracks = audio.Values
            .OrderByDescending(t => t.IsOriginal)
            .ThenBy(t => t.Language, StringComparer.OrdinalIgnoreCase)
            .ToArray();

        return new VideoInfo
        {
            Id = id,
            Title = title,
            Heights = heights.ToArray(),
            AudioTracks = tracks,
            SubtitleLanguages = SubtitleLanguageCodes.FromVideo(video)
        };
    }

    private static JsonElement UnwrapPlaylistEntry(JsonElement root)
    {
        if (root.ValueKind != JsonValueKind.Object)
        {
            throw new JsonException("Expected a JSON object from yt-dlp.");
        }

        if (!root.TryGetProperty("_type", out var type) || type.GetString() != "playlist")
        {
            return root;
        }

        if (!root.TryGetProperty("entries", out var entries) || entries.ValueKind != JsonValueKind.Array)
        {
            throw new JsonException("Playlist dump has no entries.");
        }

        foreach (var entry in entries.EnumerateArray())
        {
            if (entry.ValueKind == JsonValueKind.Object)
            {
                return entry;
            }
        }

        throw new JsonException("Playlist dump has no usable entries.");
    }

    private static string ExtractJsonObject(string raw)
    {
        if (string.IsNullOrWhiteSpace(raw))
        {
            throw new JsonException("Empty yt-dlp output.");
        }

        var start = raw.IndexOf('{');
        if (start < 0)
        {
            throw new JsonException("No JSON object in yt-dlp output.");
        }

        return start == 0 ? raw : raw[start..];
    }

    private static int GetHeight(JsonElement format)
    {
        if (!format.TryGetProperty("height", out var height) || height.ValueKind != JsonValueKind.Number)
        {
            return 0;
        }

        return height.TryGetInt32(out var value) ? value : 0;
    }

    private static string AudioKey(string? language, bool isOriginal)
    {
        var lang = string.IsNullOrWhiteSpace(language) ? "und" : language.Trim();
        return $"{lang}|{(isOriginal ? "orig" : "dub")}";
    }

    private static bool IsNone(string? codec) =>
        string.IsNullOrWhiteSpace(codec) || codec.Equals("none", StringComparison.OrdinalIgnoreCase);

    private static string? GetString(JsonElement element, string name)
    {
        if (!element.TryGetProperty(name, out var value))
        {
            return null;
        }

        return value.ValueKind switch
        {
            JsonValueKind.String => value.GetString(),
            JsonValueKind.Null => null,
            _ => value.ToString()
        };
    }
}
