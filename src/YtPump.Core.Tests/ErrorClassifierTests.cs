using YtPump.Core.Errors;
using YtPump.Core.YtDlp;

namespace YtPump.Core.Tests;

public sealed class ErrorClassifierTests
{
    [Fact]
    public void Canceled_Wins() =>
        Assert.Equal(ErrorKind.Cancelled, ErrorClassifier.Classify(1, "video unavailable", canceled: true));

    [Theory]
    [InlineData("Sign in to confirm you’re not a bot", ErrorKind.Auth)]
    [InlineData("Failed to decrypt with DPAPI", ErrorKind.Cookies)]
    [InlineData("Unable to connect to proxy", ErrorKind.Proxy)]
    [InlineData("No supported JavaScript runtime found", ErrorKind.JsRuntime)]
    [InlineData("ERROR: ffmpeg not found", ErrorKind.Ffmpeg)]
    [InlineData("ERROR: Video unavailable", ErrorKind.Unavailable)]
    [InlineData("urlopen error [Errno 11001] getaddrinfo failed", ErrorKind.Network)]
    [InlineData("EOF occurred in violation of protocol (_ssl.c:1007)", ErrorKind.Network)]
    [InlineData("Extracting cookies from firefox\nSSLError('EOF occurred in violation of protocol (_ssl.c:1007)')", ErrorKind.Network)]
    public void Signatures(string stderr, ErrorKind expected) =>
        Assert.Equal(expected, ErrorClassifier.Classify(1, stderr, canceled: false));

    [Fact]
    public void ShouldRetryCookies_AuthAndDecrypt()
    {
        Assert.True(ErrorClassifier.ShouldRetryCookies(ErrorKind.Auth));
        Assert.True(ErrorClassifier.ShouldRetryCookies(ErrorKind.Cookies));
        Assert.False(ErrorClassifier.ShouldRetryCookies(ErrorKind.Network));
    }
}
