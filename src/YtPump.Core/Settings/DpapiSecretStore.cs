using System.Security.Cryptography;
using System.Text;

namespace YtPump.Core.Settings;

public static class DpapiSecretStore
{
    private static readonly byte[] Entropy = "YtPump.proxy.v1"u8.ToArray();

    public static string? Protect(string? plaintext)
    {
        if (string.IsNullOrEmpty(plaintext))
        {
            return null;
        }

        var bytes = ProtectedData.Protect(
            Encoding.UTF8.GetBytes(plaintext),
            Entropy,
            DataProtectionScope.CurrentUser);

        return Convert.ToBase64String(bytes);
    }

    public static string? Unprotect(string? protectedBase64)
    {
        if (string.IsNullOrWhiteSpace(protectedBase64))
        {
            return null;
        }

        try
        {
            var bytes = ProtectedData.Unprotect(
                Convert.FromBase64String(protectedBase64),
                Entropy,
                DataProtectionScope.CurrentUser);
            return Encoding.UTF8.GetString(bytes);
        }
        catch (CryptographicException)
        {
            return null;
        }
        catch (FormatException)
        {
            return null;
        }
    }
}
