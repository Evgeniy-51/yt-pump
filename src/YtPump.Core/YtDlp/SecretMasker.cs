using System.Text.RegularExpressions;

namespace YtPump.Core.YtDlp;

public static partial class SecretMasker
{
    [GeneratedRegex(@"(?<=://)([^/\s:@]+):([^/\s@]+)@", RegexOptions.CultureInvariant)]
    private static partial Regex UserInfoRegex();

    public static string Mask(string? text)
    {
        if (string.IsNullOrEmpty(text))
        {
            return text ?? "";
        }

        return UserInfoRegex().Replace(text, "***:***@");
    }
}
