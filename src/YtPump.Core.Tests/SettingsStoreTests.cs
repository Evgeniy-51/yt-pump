using YtPump.Core.Settings;
using YtPump.Core.Validation;

namespace YtPump.Core.Tests;

public sealed class SettingsStoreTests
{
    [Fact]
    public void Load_MissingFile_ReturnsDefaults()
    {
        using var tmp = new TempHome();
        File.WriteAllText(Path.Combine(tmp.AppDir, SettingsStore.PortableFlagName), "");
        var store = new SettingsStore(tmp.AppDir, tmp.UserDir);

        var result = store.Load("ru", @"D:\Media");

        Assert.True(result.UsedDefaults);
        Assert.True(result.IsPortable);
        Assert.Equal("ru", result.Settings.Language);
        Assert.Equal(@"D:\Media", result.Settings.LastOutputDirectory);
        Assert.Equal(SettingsWarning.None, result.Warning);
    }

    [Fact]
    public void SaveAndLoad_Roundtrip_Portable()
    {
        using var tmp = new TempHome();
        File.WriteAllText(Path.Combine(tmp.AppDir, SettingsStore.PortableFlagName), "");
        var store = new SettingsStore(tmp.AppDir, tmp.UserDir);
        var settings = new AppSettings
        {
            LastOutputDirectory = @"E:\YouTube",
            Container = "mp4",
            Language = "en",
            CookiesBrowser = "firefox",
            RecentUrls = ["https://youtu.be/a", "https://youtu.be/b"],
            ProxyEnabled = true,
            ActiveProxyId = "local",
            Proxies =
            [
                new ProxyProfile
                {
                    Id = "local",
                    Name = "Local",
                    Type = "socks5h",
                    Host = "127.0.0.1",
                    Port = 1080,
                    PasswordProtected = DpapiSecretStore.Protect("s3cret")
                }
            ]
        };

        Assert.True(store.TrySave(settings, out var error));
        Assert.Equal(SettingsWarning.None, error);

        var loaded = store.Load("ru", @"D:\Media");
        Assert.False(loaded.UsedDefaults);
        Assert.Equal(@"E:\YouTube", loaded.Settings.LastOutputDirectory);
        Assert.Equal("mp4", loaded.Settings.Container);
        Assert.Equal("en", loaded.Settings.Language);
        Assert.Equal("firefox", loaded.Settings.CookiesBrowser);
        Assert.Equal(new[] { "https://youtu.be/a", "https://youtu.be/b" }, loaded.Settings.RecentUrls);
        Assert.True(loaded.Settings.ProxyEnabled);
        Assert.Equal("local", loaded.Settings.ActiveProxyId);
        var profile = Assert.Single(loaded.Settings.Proxies);
        Assert.Equal("Local", profile.Name);
        Assert.Equal("s3cret", DpapiSecretStore.Unprotect(profile.PasswordProtected));
        Assert.Null(loaded.Settings.Proxy);
        Assert.False(File.Exists(Path.Combine(tmp.UserDir, SettingsStore.FileName)));
        var json = File.ReadAllText(loaded.SettingsPath);
        Assert.DoesNotContain("s3cret", json);
    }

    [Fact]
    public void Load_CorruptJson_WritesBackupAndDefaults()
    {
        using var tmp = new TempHome();
        File.WriteAllText(Path.Combine(tmp.AppDir, SettingsStore.PortableFlagName), "");
        var data = Directory.CreateDirectory(Path.Combine(tmp.AppDir, SettingsStore.DataFolderName));
        var path = Path.Combine(data.FullName, SettingsStore.FileName);
        File.WriteAllText(path, "{not-json");

        var store = new SettingsStore(tmp.AppDir, tmp.UserDir);
        var result = store.Load("en", @"C:\x");

        Assert.True(result.UsedDefaults);
        Assert.Equal(SettingsWarning.CorruptJson, result.Warning);
        Assert.True(File.Exists(Path.Combine(data.FullName, SettingsStore.BackupFileName)));
        Assert.Equal("en", result.Settings.Language);
    }

    [Fact]
    public void Save_PortableWhenDataIsFile_DoesNotWriteAppData()
    {
        using var tmp = new TempHome();
        File.WriteAllText(Path.Combine(tmp.AppDir, SettingsStore.PortableFlagName), "");
        File.WriteAllText(Path.Combine(tmp.AppDir, SettingsStore.DataFolderName), "blocked");

        var store = new SettingsStore(tmp.AppDir, tmp.UserDir);
        var loaded = store.Load("ru", @"D:\Media");
        Assert.True(loaded.IsReadOnly);
        Assert.Equal(SettingsWarning.PortableNotWritable, loaded.Warning);

        Assert.False(store.TrySave(loaded.Settings, out var error));
        Assert.Equal(SettingsWarning.PortableNotWritable, error);
        Assert.False(Directory.Exists(tmp.UserDir) && File.Exists(Path.Combine(tmp.UserDir, SettingsStore.FileName)));
    }

    [Fact]
    public void Load_MigratesLegacyProxyJson()
    {
        using var tmp = new TempHome();
        File.WriteAllText(Path.Combine(tmp.AppDir, SettingsStore.PortableFlagName), "");
        var data = Directory.CreateDirectory(Path.Combine(tmp.AppDir, SettingsStore.DataFolderName));
        File.WriteAllText(
            Path.Combine(data.FullName, SettingsStore.FileName),
            """
            {
              "language": "ru",
              "lastOutputDirectory": "D:\\Media",
              "proxy": {
                "enabled": true,
                "type": "socks5h",
                "host": "10.0.0.1",
                "port": 1080,
                "username": "u"
              }
            }
            """);

        var store = new SettingsStore(tmp.AppDir, tmp.UserDir);
        var loaded = store.Load("en", @"D:\Media");

        Assert.True(loaded.Settings.ProxyEnabled);
        Assert.Null(loaded.Settings.Proxy);
        var profile = Assert.Single(loaded.Settings.Proxies);
        Assert.Equal("10.0.0.1", profile.Host);
        Assert.Equal("u", profile.Username);
        Assert.Equal(profile.Id, loaded.Settings.ActiveProxyId);
    }

    [Fact]
    public void Load_WithoutPortableFlag_UsesUserConfigDir()
    {
        using var tmp = new TempHome();
        var store = new SettingsStore(tmp.AppDir, tmp.UserDir);
        Assert.True(store.TrySave(new AppSettings { Language = "en", LastOutputDirectory = @"Z:\" }, out _));
        Assert.False(store.IsPortable);
        Assert.Equal(Path.Combine(tmp.UserDir, SettingsStore.FileName), store.SettingsPath);
        Assert.True(File.Exists(store.SettingsPath));
    }

    private sealed class TempHome : IDisposable
    {
        public string Root { get; } = Directory.CreateTempSubdirectory("ytpump-set-").FullName;
        public string AppDir => Path.Combine(Root, "app");
        public string UserDir => Path.Combine(Root, "user");

        public TempHome()
        {
            Directory.CreateDirectory(AppDir);
            Directory.CreateDirectory(UserDir);
        }

        public void Dispose() => Directory.Delete(Root, recursive: true);
    }
}
