using YtPump.Core.Proxy;

namespace YtPump.Core.Tests;

public sealed class ProxyUrlBuilderTests
{
    [Fact]
    public void Socks5h_DefaultWithoutAuth()
    {
        Assert.True(ProxyUrlBuilder.TryBuild("socks5h", "127.0.0.1", 1080, "", "", out var url, out var error));
        Assert.Equal(ProxyUrlError.None, error);
        Assert.Equal("socks5h://127.0.0.1:1080", url);
    }

    [Fact]
    public void EncodesReservedCharactersInUserInfo()
    {
        Assert.True(ProxyUrlBuilder.TryBuild(
            "http",
            "proxy.example.net",
            3128,
            "u@ser",
            "p@ss:w d",
            out var url,
            out _));
        Assert.StartsWith("http://", url);
        Assert.Contains("u%40ser", url);
        Assert.Contains("p%40ss%3Aw%20d", url);
        Assert.Contains("@proxy.example.net:3128", url);
        Assert.DoesNotContain("p@ss", url);
    }

    [Fact]
    public void UnicodePassword_IsEscaped()
    {
        Assert.True(ProxyUrlBuilder.TryBuild("socks5", "127.0.0.1", 1080, "user", "пароль", out var url, out _));
        Assert.StartsWith("socks5h://user:", url);
        Assert.DoesNotContain("пароль", url);
        Assert.Contains("@127.0.0.1:1080", url);
    }

    [Fact]
    public void IPv6_UsesBrackets()
    {
        Assert.True(ProxyUrlBuilder.TryBuild("http", "::1", 8080, null, null, out var url, out _));
        Assert.Equal("http://[::1]:8080", url);
    }

    [Fact]
    public void EmptyHost_Fails()
    {
        Assert.False(ProxyUrlBuilder.TryBuild("http", "  ", 8080, null, null, out _, out var error));
        Assert.Equal(ProxyUrlError.EmptyHost, error);
    }

    [Fact]
    public void InvalidPort_Fails()
    {
        Assert.False(ProxyUrlBuilder.TryBuild("http", "127.0.0.1", 0, null, null, out _, out var error));
        Assert.Equal(ProxyUrlError.InvalidPort, error);
    }
}
