using YtPump.Core.YtDlp;

namespace YtPump.Core.Tests;

public sealed class FormatSelectorTests
{
    [Fact]
    public void Mkv_HeightAndLanguage_HasFallbackChain()
    {
        var selector = FormatSelector.Build("mkv", "1080", "ru|orig");
        Assert.Equal("bv*[height<=1080]+ba[language=ru]/bv*[height<=1080]+ba/b", selector);
    }

    [Fact]
    public void Mkv_BestWithoutLanguage_NoHeightNoLang()
    {
        var selector = FormatSelector.Build("mkv", "best", "pending");
        Assert.Equal("bv*+ba/b", selector);
        Assert.DoesNotContain("language", selector);
    }

    [Fact]
    public void Mp4_PrefersAvcAacWithLanguage()
    {
        var selector = FormatSelector.Build("mp4", "720", "en|dub");
        Assert.Contains("vcodec^=avc", selector);
        Assert.Contains("[height<=720]", selector);
        Assert.Contains("ba[acodec^=mp4a][language=en]", selector);
        Assert.Contains("b[ext=mp4]", selector);
    }

    [Fact]
    public void AudioOnly_PrefersM4aThenFallback()
    {
        Assert.Equal("ba[language=ru][ext=m4a]/ba[language=ru]/ba", FormatSelector.Build("audio", "1080", "ru|orig"));
        Assert.Equal("ba[ext=m4a]/ba/b", FormatSelector.Build("audio", "best", "pending"));
        Assert.DoesNotContain("height", FormatSelector.Build("audio", "1080", "ru|orig"));
    }

    [Fact]
    public void UndLanguage_TreatedAsMissing() =>
        Assert.Null(FormatSelector.ParseLanguage("und|orig"));
}
