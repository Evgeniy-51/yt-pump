using YtPump.Core.YtDlp;

namespace YtPump.Core.Tests;

public sealed class MetadataParserTests
{
    [Fact]
    public void Parse_ExtractsHeightsAudioAndRuCaptions()
    {
        var info = MetadataParser.Parse(ReadFixture("youtube-formats.json"));
        Assert.Equal("fixture01", info.Id);
        Assert.Equal("Fixture Video", info.Title);
        Assert.Equal(new[] { 2160, 1080, 720, 360 }, info.Heights);
        Assert.Equal(new[] { "ru", "en" }, info.SubtitleLanguages);
        Assert.Equal(2, info.AudioTracks.Count);
        Assert.Equal("ru|orig", info.AudioTracks[0].Id);
        Assert.True(info.AudioTracks[0].IsOriginal);
        Assert.Equal("en|dub", info.AudioTracks[1].Id);
        Assert.False(info.AudioTracks[1].IsOriginal);
    }

    [Fact]
    public void Parse_SkipsPrefixAndPlaylistNullEntries()
    {
        var raw = "WARNING: something\n" + ReadFixture("youtube-playlist.json");
        var info = MetadataParser.Parse(raw);
        Assert.Equal("pl1", info.Id);
        Assert.Equal(new[] { 1080 }, info.Heights);
        Assert.True(info.AudioTracks[0].IsOriginal);
        Assert.Equal("de", info.AudioTracks[0].Language);
        Assert.Equal(new[] { "de" }, info.SubtitleLanguages);
    }

    [Fact]
    public void Parse_CollapsesCaptionVariants()
    {
        const string json = """
            {"id":"x","title":"t","formats":[],
             "subtitles":{"ru-orig":[{"ext":"vtt"}],"live_chat":[{"ext":"json"}]},
             "automatic_captions":{"a.en":[{"ext":"vtt"}],"en-US":[{"ext":"vtt"}],"ru":[{"ext":"vtt"}]}}
            """;
        var info = MetadataParser.Parse(json);
        Assert.Equal(new[] { "ru", "en" }, info.SubtitleLanguages);
    }

    [Fact]
    public void Parse_Empty_Throws() =>
        Assert.Throws<System.Text.Json.JsonException>(() => MetadataParser.Parse("no json here"));

    private static string ReadFixture(string name) =>
        File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "Fixtures", name));
}
