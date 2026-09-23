using YtPump.Core.YtDlp;

namespace YtPump.Core.Tests;

public sealed class OutputPathResolverTests
{
    [Fact]
    public void PrefersLastExistingCandidate()
    {
        using var tmp = new TempDir();
        var gone = Path.Combine(tmp.Path, "gone.mp4");
        var keep = Path.Combine(tmp.Path, "keep.mkv");
        File.WriteAllText(keep, "x");

        var resolved = OutputPathResolver.Resolve([gone, keep], tmp.Path, "abc");
        Assert.Equal(Path.GetFullPath(keep), resolved);
    }

    [Fact]
    public void SkipsDeletedAfterVideo_FindsMergedById()
    {
        using var tmp = new TempDir();
        var deleted = Path.Combine(tmp.Path, "clip.f396.mp4");
        var merged = Path.Combine(tmp.Path, "Clip title [abc123].mkv");
        File.WriteAllText(merged, "x");

        var resolved = OutputPathResolver.Resolve([deleted], tmp.Path, "abc123");
        Assert.Equal(Path.GetFullPath(merged), resolved);
    }

    [Fact]
    public void PrefersMediaOverSrt()
    {
        using var tmp = new TempDir();
        File.WriteAllText(Path.Combine(tmp.Path, "t [id1].ru.srt"), "sub");
        var mkv = Path.Combine(tmp.Path, "t [id1].mkv");
        File.WriteAllText(mkv, "x");

        var resolved = OutputPathResolver.Resolve([], tmp.Path, "id1");
        Assert.Equal(Path.GetFullPath(mkv), resolved);
    }

    [Fact]
    public void MissingEverything_ReturnsNull() =>
        Assert.Null(OutputPathResolver.Resolve(["nope.mkv"], @"C:\missing-ytpump-dir", "x"));

    private sealed class TempDir : IDisposable
    {
        public string Path { get; } = Directory.CreateTempSubdirectory("ytpump-out-").FullName;

        public void Dispose() => Directory.Delete(Path, recursive: true);
    }
}
