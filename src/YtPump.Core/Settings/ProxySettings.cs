namespace YtPump.Core.Settings;

public sealed class ProxySettings
{
    public bool Enabled { get; set; }
    public string Type { get; set; } = "socks5h";
    public string Host { get; set; } = "127.0.0.1";
    public int Port { get; set; } = 1080;
    public string Username { get; set; } = "";
    public string? PasswordProtected { get; set; }

    public void Normalize()
    {
        Type = Type?.Trim().ToLowerInvariant() switch
        {
            "http" => "http",
            "https" => "https",
            "socks5" or "socks5h" => "socks5h",
            _ => "socks5h"
        };

        Host ??= "";
        Username ??= "";
        if (Port is < 1 or > 65535)
        {
            Port = 1080;
        }
    }
}
