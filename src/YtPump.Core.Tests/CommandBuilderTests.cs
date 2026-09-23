using YtPump.Core.Tools;
using YtPump.Core.YtDlp;

namespace YtPump.Core.Tests;

public sealed class CommandBuilderTests
{
    [Fact]
    public void Probe_HasJsonDumpFlagsAndUrlLast()
    {
        var tools = new ToolsStatus(@"C:\app", @"C:\app\tools", @"C:\app\tools\yt-dlp.exe",
            @"C:\app\tools\ffmpeg.exe", @"C:\app\tools\ffprobe.exe", @"C:\app\tools\deno.exe");
        const string url = "https://www.youtube.com/watch?v=dQw4w9WgXcQ";
        var args = CommandBuilder.Probe(tools, url);

        Assert.Contains("--no-playlist", args);
        Assert.Contains("-J", args);
        Assert.Contains("--skip-download", args);
        Assert.Contains("--windows-filenames", args);
        Assert.Equal($"deno:{tools.DenoPath}", args[args.IndexOf("--js-runtimes") + 1]);
        Assert.Equal(url, args[^1]);
        Assert.DoesNotContain("--proxy", args);
        Assert.DoesNotContain("--cookies-from-browser", args);
    }

    [Fact]
    public void Probe_AddsCookiesFromBrowser()
    {
        var args = CommandBuilder.Probe(DummyTools(), "https://youtu.be/x", cookiesFromBrowser: "firefox");
        Assert.Contains("--cookies-from-browser", args);
        Assert.Equal("firefox", args[args.IndexOf("--cookies-from-browser") + 1]);
        Assert.Equal("https://youtu.be/x", args[^1]);
    }

    [Fact]
    public void Probe_AddsProxyWhenProvided()
    {
        var tools = new ToolsStatus(@"C:\app", @"C:\app\tools", @"C:\app\tools\yt-dlp.exe",
            @"C:\app\tools\ffmpeg.exe", @"C:\app\tools\ffprobe.exe", @"C:\app\tools\deno.exe");
        var args = CommandBuilder.Probe(tools, "https://youtu.be/x", "socks5h://127.0.0.1:1080");
        Assert.Contains("--proxy", args);
        Assert.Equal("socks5h://127.0.0.1:1080", args[args.IndexOf("--proxy") + 1]);
    }

    [Fact]
    public void Download_AddsProxyWhenProvided()
    {
        var args = CommandBuilder.Download(
            DummyTools(),
            "https://youtu.be/x",
            @"D:\Media",
            "bv*+ba/b",
            "mkv",
            "socks5h://127.0.0.1:1080");
        Assert.Contains("--proxy", args);
        Assert.Equal("socks5h://127.0.0.1:1080", args[args.IndexOf("--proxy") + 1]);
    }

    [Fact]
    public void Download_Mkv_HasProgressPrintAndMerge()
    {
        var tools = DummyTools();
        var args = CommandBuilder.Download(
            tools,
            "https://youtu.be/x",
            @"D:\Media",
            "bv*+ba/b",
            "mkv");

        Assert.Contains("--force-overwrites", args);
        Assert.Contains("--progress", args);
        Assert.Contains("-f", args);
        Assert.Equal("bv*+ba/b", args[args.IndexOf("-f") + 1]);
        Assert.Equal("mkv", args[args.IndexOf("--merge-output-format") + 1]);
        Assert.Equal(@"D:\Media", args[args.IndexOf("-P") + 1]);
        Assert.Contains("after_move:YTPFILE:%(filepath)s", args);
        Assert.Equal("%(title)s [%(id)s].%(ext)s", args[args.IndexOf("-o") + 1]);
        Assert.Equal("https://youtu.be/x", args[^1]);
        Assert.DoesNotContain("--proxy", args);
        Assert.DoesNotContain("--write-subs", args);
        Assert.DoesNotContain("--cookies-from-browser", args);
    }

    [Fact]
    public void Download_WriteSubtitlesRu_AddsSidecarFlags()
    {
        var args = CommandBuilder.Download(
            DummyTools(),
            "https://youtu.be/x",
            @"D:\Media",
            "bv*+ba/b",
            "mkv",
            subtitleLanguage: "en-US");
        Assert.Contains("--write-subs", args);
        Assert.Contains("--write-auto-subs", args);
        Assert.Equal("en.*", args[args.IndexOf("--sub-langs") + 1]);
        Assert.Equal("srt", args[args.IndexOf("--convert-subs") + 1]);
    }

    [Fact]
    public void Download_AddsCookiesFromBrowser()
    {
        var args = CommandBuilder.Download(
            DummyTools(),
            "https://youtu.be/x",
            @"D:\Media",
            "bv*+ba/b",
            "mkv",
            cookiesFromBrowser: "edge");
        Assert.Equal("edge", args[args.IndexOf("--cookies-from-browser") + 1]);
        Assert.Equal("https://youtu.be/x", args[^1]);
    }

    [Fact]
    public void Download_Audio_ExtractsM4a()
    {
        var args = CommandBuilder.Download(DummyTools(), "https://youtu.be/x", @"D:\Media", "ba/b", "audio");
        Assert.DoesNotContain("--merge-output-format", args);
        Assert.Contains("--extract-audio", args);
        Assert.Equal("m4a", args[args.IndexOf("--audio-format") + 1]);
    }

    [Fact]
    public void Download_UsesFileStemInOutputTemplate()
    {
        var args = CommandBuilder.Download(
            DummyTools(),
            "https://youtu.be/x",
            @"D:\Media",
            "bv*+ba/b",
            "mkv",
            fileStem: @"Bad:name%");
        Assert.Equal("Badname%% [%(id)s].%(ext)s", args[args.IndexOf("-o") + 1]);
    }

    private static ToolsStatus DummyTools() =>
        new(@"C:\app", @"C:\app\tools", @"C:\app\tools\yt-dlp.exe",
            @"C:\app\tools\ffmpeg.exe", @"C:\app\tools\ffprobe.exe", @"C:\app\tools\deno.exe");
}
