using YtPump.Core.Errors;

namespace YtPump.Core.YtDlp;

public static class ErrorClassifier
{
    public static ErrorKind Classify(int exitCode, string? stderr, bool canceled)
    {
        if (canceled)
        {
            return ErrorKind.Cancelled;
        }

        var text = stderr ?? "";
        if (ContainsAny(text,
                "decrypt with DPAPI",
                "failed to decrypt"))
        {
            return ErrorKind.Cookies;
        }

        if (ContainsAny(text,
                "sign in to confirm",
                "please log in",
                "http error 401",
                "http error 403"))
        {
            return ErrorKind.Auth;
        }

        if (ContainsAny(text,
                "proxy",
                "tunnel connection failed",
                "407"))
        {
            return ErrorKind.Proxy;
        }

        if (ContainsAny(text,
                "js runtime",
                "javascript runtime",
                "deno",
                "n-challenge",
                "ejs"))
        {
            return ErrorKind.JsRuntime;
        }

        if (ContainsAny(text,
                "ffmpeg",
                "ffprobe",
                "postprocessor"))
        {
            return ErrorKind.Ffmpeg;
        }

        if (ContainsAny(text,
                "video unavailable",
                "private video",
                "has been removed",
                "is not available",
                "copyright",
                "http error 404"))
        {
            return ErrorKind.Unavailable;
        }

        if (ContainsAny(text,
                "unable to download",
                "urlopen error",
                "network is unreachable",
                "timed out",
                "name or service not known",
                "getaddrinfo",
                "connection refused",
                "temporarily unavailable",
                "sslerror",
                "violation of protocol",
                "_ssl.c"))
        {
            return ErrorKind.Network;
        }

        return exitCode == 0 ? ErrorKind.Unknown : ErrorKind.Unknown;
    }

    public static bool ShouldRetryCookies(ErrorKind kind) =>
        kind is ErrorKind.Auth or ErrorKind.Cookies;

    private static bool ContainsAny(string text, params string[] needles)
    {
        foreach (var needle in needles)
        {
            if (text.Contains(needle, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }

        return false;
    }
}
