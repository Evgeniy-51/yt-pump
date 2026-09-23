using YtPump.Core.Settings;

namespace YtPump.Core.Tests;

public sealed class DpapiSecretStoreTests
{
    [Fact]
    public void Protect_Empty_ReturnsNull()
    {
        Assert.Null(DpapiSecretStore.Protect(null));
        Assert.Null(DpapiSecretStore.Protect(""));
    }

    [Fact]
    public void ProtectUnprotect_Roundtrip()
    {
        const string secret = "p@ss:word/юникод";
        var blob = DpapiSecretStore.Protect(secret);
        Assert.NotNull(blob);
        Assert.DoesNotContain(secret, blob, StringComparison.Ordinal);
        Assert.Equal(secret, DpapiSecretStore.Unprotect(blob));
    }

    [Fact]
    public void Unprotect_Garbage_ReturnsNull()
    {
        Assert.Null(DpapiSecretStore.Unprotect("%%%not-base64%%%"));
        Assert.Null(DpapiSecretStore.Unprotect("AAAA"));
    }
}
