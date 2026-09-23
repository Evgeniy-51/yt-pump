using System.Globalization;

namespace YtPump.Core.Settings;

public static class UiLanguage
{
    public static string Normalize(string? value) =>
        string.Equals(value, "en", StringComparison.OrdinalIgnoreCase) ? "en" : "ru";

    public static string FromOs(CultureInfo? culture = null)
    {
        var current = culture ?? CultureInfo.CurrentUICulture;
        return current.TwoLetterISOLanguageName.Equals("en", StringComparison.OrdinalIgnoreCase)
            ? "en"
            : "ru";
    }
}
