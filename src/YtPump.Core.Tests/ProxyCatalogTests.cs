using YtPump.Core.Settings;

namespace YtPump.Core.Tests;

public sealed class ProxyCatalogTests
{
    [Fact]
    public void Normalize_MigratesLegacyProxy()
    {
        var settings = new AppSettings
        {
            Proxy = new ProxySettings
            {
                Enabled = true,
                Type = "socks5h",
                Host = "10.0.0.1",
                Port = 1080,
                Username = "u"
            }
        };

        ProxyCatalog.Normalize(settings);

        Assert.True(settings.ProxyEnabled);
        Assert.Null(settings.Proxy);
        var profile = Assert.Single(settings.Proxies);
        Assert.Equal("10.0.0.1", profile.Host);
        Assert.Equal("u", profile.Username);
        Assert.Equal(profile.Id, settings.ActiveProxyId);
        Assert.Equal("10.0.0.1:1080", ProxyCatalog.DisplayName(profile));
    }

    [Fact]
    public void Normalize_CapsAndRepairsActive()
    {
        var settings = new AppSettings
        {
            Proxies = Enumerable.Range(0, 12).Select(_ => ProxyCatalog.Create()).ToList(),
            ActiveProxyId = "missing"
        };

        ProxyCatalog.Normalize(settings);

        Assert.Equal(ProxyCatalog.MaxCount, settings.Proxies.Count);
        Assert.Equal(settings.Proxies[0].Id, settings.ActiveProxyId);
    }

    [Fact]
    public void Normalize_EmptyStaysEmpty()
    {
        var settings = new AppSettings { Proxies = [], ActiveProxyId = "gone" };
        ProxyCatalog.Normalize(settings);
        Assert.Empty(settings.Proxies);
        Assert.Equal("", settings.ActiveProxyId);
    }

    [Fact]
    public void Duplicate_CopiesEndpointAndRenames()
    {
        var source = new ProxyProfile { Name = "NL", Host = "1.1.1.1", Port = 1080, PasswordProtected = "x" };
        var copy = ProxyCatalog.Duplicate(source);
        Assert.NotEqual(source.Id, copy.Id);
        Assert.Equal("NL (2)", copy.Name);
        Assert.Equal("1.1.1.1", copy.Host);
        Assert.Equal("x", copy.PasswordProtected);
    }
}
