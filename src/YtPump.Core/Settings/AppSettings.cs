using YtPump.Core.YtDlp;

namespace YtPump.Core.Settings;

public sealed class AppSettings
{
    public string LastOutputDirectory { get; set; } = "";
    public string Container { get; set; } = "mkv";
    public int? PreferredHeight { get; set; }
    public bool WriteSubtitlesRu { get; set; }
    public string SubtitleLanguage { get; set; } = "ru";
    public string Language { get; set; } = "ru";
    public WindowSettings? Window { get; set; }
    public bool ProxyEnabled { get; set; }
    public string ActiveProxyId { get; set; } = "";
    public List<ProxyProfile> Proxies { get; set; } = [];
    public ProxySettings? Proxy { get; set; }
    public string CookiesBrowser { get; set; } = "";
    public List<string> RecentUrls { get; set; } = [];

    public void Normalize()
    {
        Language = UiLanguage.Normalize(Language);
        SubtitleLanguage = SubtitleLanguageCodes.Normalize(SubtitleLanguage) ?? "ru";
        Container = Container switch
        {
            "mp4" => "mp4",
            "audio" => "audio",
            _ => "mkv"
        };
        Window ??= new();
        Window.Normalize();
        ProxyCatalog.Normalize(this);
        CookiesBrowser = Cookies.BrowserCookieLocator.NormalizeName(CookiesBrowser) ?? "";
        RecentUrls = UrlHistory.Normalize(RecentUrls);
    }
}
