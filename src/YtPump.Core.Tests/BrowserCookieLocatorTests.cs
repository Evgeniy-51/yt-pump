using YtPump.Core.Cookies;

namespace YtPump.Core.Tests;

public sealed class BrowserCookieLocatorTests
{
    [Fact]
    public void ProfilePaths_WindowsLayout()
    {
        Assert.Equal(
            @"L\Google\Chrome\User Data",
            BrowserCookieLocator.GetProfileDirectory("chrome", @"L", @"R"));
        Assert.Equal(
            @"R\Mozilla\Firefox\Profiles",
            BrowserCookieLocator.GetProfileDirectory("firefox", @"L", @"R"));
        Assert.Equal(
            @"L\Microsoft\Edge\User Data",
            BrowserCookieLocator.GetProfileDirectory("EDGE", @"L", @"R"));
        Assert.Null(BrowserCookieLocator.GetProfileDirectory("safari", @"L", @"R"));
    }

    [Fact]
    public void Installed_PrefersRememberedAndSkipsMissing()
    {
        using var tmp = new TempDir();
        Directory.CreateDirectory(Path.Combine(tmp.Local, @"Google\Chrome\User Data"));
        Directory.CreateDirectory(Path.Combine(tmp.Roaming, @"Mozilla\Firefox\Profiles"));

        var order = BrowserCookieLocator.Installed("chrome", tmp.Local, tmp.Roaming);
        Assert.Equal(new[] { "chrome", "firefox" }, order.ToArray());
    }

    [Fact]
    public void NormalizeName_KnownOnly()
    {
        Assert.Equal("firefox", BrowserCookieLocator.NormalizeName(" Firefox "));
        Assert.Null(BrowserCookieLocator.NormalizeName("iexplore"));
        Assert.Null(BrowserCookieLocator.NormalizeName(""));
    }

    private sealed class TempDir : IDisposable
    {
        public string Root { get; } = Directory.CreateTempSubdirectory("ytpump-ck-").FullName;
        public string Local => Path.Combine(Root, "local");
        public string Roaming => Path.Combine(Root, "roaming");

        public TempDir()
        {
            Directory.CreateDirectory(Local);
            Directory.CreateDirectory(Roaming);
        }

        public void Dispose() => Directory.Delete(Root, recursive: true);
    }
}
