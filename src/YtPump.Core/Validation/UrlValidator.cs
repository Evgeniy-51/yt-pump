namespace YtPump.Core.Validation;

public enum UrlStatus
{
    Ok,
    Empty,
    Invalid
}

public static class UrlValidator
{
    public static bool TryNormalize(string? input, out string url, out UrlStatus status)
    {
        url = (input ?? "").Trim();
        if (url.Length == 0)
        {
            status = UrlStatus.Empty;
            return false;
        }

        if (!Uri.TryCreate(url, UriKind.Absolute, out var uri)
            || (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps)
            || string.IsNullOrWhiteSpace(uri.Host))
        {
            status = UrlStatus.Invalid;
            return false;
        }

        url = uri.ToString();
        status = UrlStatus.Ok;
        return true;
    }
}
