using YtPump.Core.Validation;

namespace YtPump.Core.Tests;

public sealed class UrlValidatorTests
{
    [Fact]
    public void Empty_IsEmpty()
    {
        Assert.False(UrlValidator.TryNormalize("  ", out _, out var status));
        Assert.Equal(UrlStatus.Empty, status);
    }

    [Theory]
    [InlineData("not a url")]
    [InlineData("ftp://example.com/a")]
    [InlineData("https://")]
    public void Invalid_Rejected(string input)
    {
        Assert.False(UrlValidator.TryNormalize(input, out _, out var status));
        Assert.Equal(UrlStatus.Invalid, status);
    }

    [Fact]
    public void HttpsYoutube_OkAndTrimmed()
    {
        Assert.True(UrlValidator.TryNormalize("  https://youtu.be/w8-ZKfECtO4  ", out var url, out var status));
        Assert.Equal(UrlStatus.Ok, status);
        Assert.StartsWith("https://youtu.be/w8-ZKfECtO4", url, StringComparison.OrdinalIgnoreCase);
    }
}
