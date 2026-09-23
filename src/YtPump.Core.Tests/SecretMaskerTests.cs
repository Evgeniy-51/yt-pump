using YtPump.Core.YtDlp;

namespace YtPump.Core.Tests;

public sealed class SecretMaskerTests
{
    [Theory]
    [InlineData("http://user:p%40ss@proxy.example:3128", "http://***:***@proxy.example:3128")]
    [InlineData("socks5h://u:secret@127.0.0.1:1080", "socks5h://***:***@127.0.0.1:1080")]
    [InlineData("https://youtu.be/w8-ZKfECtO4", "https://youtu.be/w8-ZKfECtO4")]
    [InlineData("", "")]
    public void Mask_UserInfo_Only(string input, string expected) =>
        Assert.Equal(expected, SecretMasker.Mask(input));
}
