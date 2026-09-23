namespace YtPump.Core.YtDlp;

public sealed class AudioTrackInfo
{
    public required string Id { get; init; }
    public string? Language { get; init; }
    public bool IsOriginal { get; init; }
    public string? DisplayName { get; init; }
}

public sealed class VideoInfo
{
    public required string Id { get; init; }
    public required string Title { get; init; }
    public IReadOnlyList<int> Heights { get; init; } = [];
    public IReadOnlyList<AudioTrackInfo> AudioTracks { get; init; } = [];
    public IReadOnlyList<string> SubtitleLanguages { get; init; } = [];
}
