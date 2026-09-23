using YtPump.Core.YtDlp;

namespace YtPump.Core.Tests;

public sealed class ProgressParserTests
{
    [Fact]
    public void ParseProgress_ReadsFields()
    {
        Assert.True(ProgressParser.TryParseProgress(
            "YTP|123|456|01:10|8.65KiB/s| 42.3%", out var progress));
        Assert.Equal(42.3, progress.Percent);
        Assert.Equal("8.65KiB/s", progress.Speed);
        Assert.Equal("01:10", progress.Eta);
        Assert.Equal("123", progress.Downloaded);
        Assert.Equal("456", progress.Total);
    }

    [Fact]
    public void ParseProgress_IgnoresHumanDownloadLine()
    {
        Assert.False(ProgressParser.TryParseProgress(
            "[download]  42.3% of 1.20GiB at 8.65MiB/s ETA 01:10", out _));
    }

    [Fact]
    public void ParseProgress_AllowsTypePrefix()
    {
        Assert.True(ProgressParser.TryParseProgress(
            "download:YTP|1|2|NA|NA|100.0%", out var progress));
        Assert.Equal(100.0, progress.Percent);
        Assert.Null(progress.Speed);
        Assert.Null(progress.Eta);
    }

    [Fact]
    public void ParseOutputPath_TakesLastPrefix()
    {
        Assert.True(ProgressParser.TryParseOutputPath(
            @"YTPFILE:D:\Media\Title [id].mkv", out var path));
        Assert.Equal(@"D:\Media\Title [id].mkv", path);
        Assert.False(ProgressParser.TryParseOutputPath("[download] Destination: x.mkv", out _));
    }
}
