namespace YtPump.Core.Settings;

public static class ProxyCatalog
{
    public const int MaxCount = 10;

    public static void Normalize(AppSettings settings)
    {
        settings.Proxies ??= [];
        settings.ActiveProxyId ??= "";

        if (settings.Proxies.Count == 0 && settings.Proxy != null)
        {
            settings.Proxy.Normalize();
            settings.ProxyEnabled = settings.Proxy.Enabled;
            var migrated = FromLegacy(settings.Proxy);
            settings.Proxies.Add(migrated);
            settings.ActiveProxyId = migrated.Id;
        }

        settings.Proxy = null;

        var ids = new HashSet<string>(StringComparer.Ordinal);
        var cleaned = new List<ProxyProfile>();
        foreach (var item in settings.Proxies)
        {
            if (item == null)
            {
                continue;
            }

            item.Normalize();
            if (item.Id.Length == 0 || !ids.Add(item.Id))
            {
                item.Id = NewId();
                ids.Add(item.Id);
            }

            cleaned.Add(item);
            if (cleaned.Count == MaxCount)
            {
                break;
            }
        }

        if (cleaned.Count == 0)
        {
            settings.Proxies = cleaned;
            settings.ActiveProxyId = "";
            return;
        }

        settings.Proxies = cleaned;
        if (Find(cleaned, settings.ActiveProxyId) == null)
        {
            settings.ActiveProxyId = cleaned[0].Id;
        }
    }

    public static ProxyProfile Create() =>
        new()
        {
            Id = NewId(),
            Name = "",
            Type = "socks5h",
            Host = "127.0.0.1",
            Port = 1080,
            Username = ""
        };

    public static ProxyProfile Duplicate(ProxyProfile source)
    {
        source.Normalize();
        return new ProxyProfile
        {
            Id = NewId(),
            Name = DuplicateName(source.Name),
            Type = source.Type,
            Host = source.Host,
            Port = source.Port,
            Username = source.Username,
            PasswordProtected = source.PasswordProtected
        };
    }

    public static ProxyProfile? FindActive(AppSettings settings) =>
        Find(settings.Proxies, settings.ActiveProxyId) ?? settings.Proxies.FirstOrDefault();

    public static string DisplayName(ProxyProfile profile)
    {
        if (!string.IsNullOrWhiteSpace(profile.Name))
        {
            return profile.Name.Trim();
        }

        var host = (profile.Host ?? "").Trim();
        return host.Length == 0 ? "" : $"{host}:{profile.Port}";
    }

    public static string NewId() => Guid.NewGuid().ToString("N");

    public static string DuplicateName(string? name)
    {
        var trimmed = (name ?? "").Trim();
        return trimmed.Length == 0 ? "" : $"{trimmed} (2)";
    }

    private static ProxyProfile FromLegacy(ProxySettings legacy) =>
        new()
        {
            Id = NewId(),
            Name = "",
            Type = legacy.Type,
            Host = legacy.Host,
            Port = legacy.Port,
            Username = legacy.Username,
            PasswordProtected = legacy.PasswordProtected
        };

    private static ProxyProfile? Find(IEnumerable<ProxyProfile>? profiles, string? id)
    {
        if (profiles == null || string.IsNullOrWhiteSpace(id))
        {
            return null;
        }

        return profiles.FirstOrDefault(p => string.Equals(p.Id, id, StringComparison.Ordinal));
    }
}
