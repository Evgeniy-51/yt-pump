namespace YtPump.Core.Errors;

public enum ErrorKind
{
    Network,
    Proxy,
    Unavailable,
    Auth,
    Cookies,
    JsRuntime,
    Ffmpeg,
    Cancelled,
    Unknown
}
