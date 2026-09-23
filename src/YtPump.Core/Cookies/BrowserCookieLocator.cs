namespace YtPump.Core.Cookies;

public static class BrowserCookieLocator
{
    public static readonly string[] Priority =
    [
        "firefox",
        "edge",
        "brave",
        "vivaldi",
        "opera",
        "chrome",
        "chromium",
        "whale"
    ];

    public static string? NormalizeName(string? name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            return null;
        }

        var trimmed = name.Trim().ToLowerInvariant();
        return Priority.Contains(trimmed, StringComparer.Ordinal) ? trimmed : null;
    }

    public static IReadOnlyList<string> Installed(string? preferred = null)
    {
        var local = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        var roaming = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        return Installed(preferred, local, roaming);
    }

    public static IReadOnlyList<string> Installed(string? preferred, string localAppData, string roamingAppData)
    {
        var found = new List<string>(Priority.Length);
        foreach (var name in Priority)
        {
            var dir = GetProfileDirectory(name, localAppData, roamingAppData);
            if (dir != null && Directory.Exists(dir))
            {
                found.Add(name);
            }
        }

        var first = NormalizeName(preferred);
        if (first != null && found.Remove(first))
        {
            found.Insert(0, first);
        }

        return found;
    }

    public static string? GetProfileDirectory(string browser, string localAppData, string roamingAppData)
    {
        var name = NormalizeName(browser);
        if (name is null)
        {
            return null;
        }

        return name switch
        {
            "firefox" => Path.Combine(roamingAppData, @"Mozilla\Firefox\Profiles"),
            "edge" => Path.Combine(localAppData, @"Microsoft\Edge\User Data"),
            "brave" => Path.Combine(localAppData, @"BraveSoftware\Brave-Browser\User Data"),
            "vivaldi" => Path.Combine(localAppData, @"Vivaldi\User Data"),
            "opera" => Path.Combine(roamingAppData, @"Opera Software\Opera Stable"),
            "chrome" => Path.Combine(localAppData, @"Google\Chrome\User Data"),
            "chromium" => Path.Combine(localAppData, @"Chromium\User Data"),
            "whale" => Path.Combine(localAppData, @"Naver\Naver Whale\User Data"),
            _ => null
        };
    }
}
