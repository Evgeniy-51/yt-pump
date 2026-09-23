using System.Net;

namespace YtPump.Core.Proxy;

public enum ProxyUrlError
{
    None,
    EmptyHost,
    InvalidPort,
    InvalidType
}

public static class ProxyUrlBuilder
{
    public static bool TryBuild(
        string? type,
        string? host,
        int port,
        string? username,
        string? password,
        out string url,
        out ProxyUrlError error)
    {
        url = "";
        var scheme = NormalizeScheme(type);
        if (scheme is null)
        {
            error = ProxyUrlError.InvalidType;
            return false;
        }

        var trimmedHost = NormalizeHost(host);
        if (trimmedHost.Length == 0)
        {
            error = ProxyUrlError.EmptyHost;
            return false;
        }

        if (port is < 1 or > 65535)
        {
            error = ProxyUrlError.InvalidPort;
            return false;
        }

        var builder = new UriBuilder
        {
            Scheme = scheme == "socks5h" ? "socks5" : scheme,
            Host = trimmedHost,
            Port = port,
            Path = string.Empty
        };

        if (!string.IsNullOrEmpty(username))
        {
            builder.UserName = Uri.EscapeDataString(username);
            builder.Password = Uri.EscapeDataString(password ?? "");
        }

        url = builder.Uri.GetComponents(
            UriComponents.SchemeAndServer | UriComponents.UserInfo,
            UriFormat.UriEscaped);

        if (url.EndsWith('/'))
        {
            url = url.TrimEnd('/');
        }

        if (scheme == "socks5h")
        {
            url = "socks5h" + url[url.IndexOf(':', StringComparison.Ordinal)..];
        }

        error = ProxyUrlError.None;
        return true;
    }

    public static string? NormalizeScheme(string? type) =>
        type?.Trim().ToLowerInvariant() switch
        {
            "http" => "http",
            "https" => "https",
            "socks5" or "socks5h" => "socks5h",
            _ => null
        };

    private static string NormalizeHost(string? host)
    {
        var trimmed = (host ?? "").Trim();
        if (trimmed.StartsWith('[') && trimmed.EndsWith(']') && trimmed.Length > 2)
        {
            trimmed = trimmed[1..^1];
        }

        if (IPAddress.TryParse(trimmed, out var address))
        {
            return address.ToString();
        }

        return trimmed;
    }
}
