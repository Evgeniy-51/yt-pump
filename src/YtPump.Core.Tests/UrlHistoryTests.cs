using YtPump.Core.Settings;

namespace YtPump.Core.Tests;

public sealed class UrlHistoryTests
{
    [Fact]
    public void Remember_MovesExistingToFrontAndCaps()
    {
        var current = Enumerable.Range(1, 10).Select(i => $"https://youtu.be/{i}");
        var next = UrlHistory.Remember(current, "https://youtu.be/3");
        Assert.Equal("https://youtu.be/3", next[0]);
        Assert.Equal(10, next.Count);
        Assert.DoesNotContain("https://youtu.be/3", next.Skip(1));
    }

    [Fact]
    public void Normalize_DropsEmptyAndDuplicates()
    {
        var next = UrlHistory.Normalize([" https://a ", "", "https://a", "https://b"]);
        Assert.Equal(new[] { "https://a", "https://b" }, next);
    }
}
