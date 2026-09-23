using System.Globalization;
using YtPump.Core.Settings;

namespace YtPump.Core.Tests;

public sealed class UiLanguageTests
{
    [Theory]
    [InlineData("en", "en")]
    [InlineData("EN", "en")]
    [InlineData("ru", "ru")]
    [InlineData("de", "ru")]
    [InlineData(null, "ru")]
    public void Normalize_FallsBackToRu(string? value, string expected) =>
        Assert.Equal(expected, UiLanguage.Normalize(value));

    [Fact]
    public void FromOs_EnglishCulture_IsEn() =>
        Assert.Equal("en", UiLanguage.FromOs(new CultureInfo("en-US")));

    [Fact]
    public void FromOs_OtherCulture_IsRu() =>
        Assert.Equal("ru", UiLanguage.FromOs(new CultureInfo("de-DE")));
}
