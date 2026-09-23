using YtPump.Core.Proxy;

namespace YtPump.Core.Settings;

public sealed class ProxyProfile
{
    public string Id { get; set; } = "";
    public string Name { get; set; } = "";
    public string Type { get; set; } = "socks5h";
    public string Host { get; set; } = "127.0.0.1";
    public int Port { get; set; } = 1080;
    public string Username { get; set; } = "";
    public string? PasswordProtected { get; set; }

    public void Normalize()
    {
        Id = (Id ?? "").Trim();
        Name = (Name ?? "").Trim();
        Type = ProxyUrlBuilder.NormalizeScheme(Type) ?? "socks5h";
        Host = (Host ?? "").Trim();
        Username ??= "";
        if (Port is < 1 or > 65535)
        {
            Port = 1080;
        }
    }
}
