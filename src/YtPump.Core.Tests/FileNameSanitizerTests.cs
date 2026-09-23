using YtPump.Core.Validation;

namespace YtPump.Core.Tests;

public sealed class FileNameSanitizerTests
{
    [Fact]
    public void Sanitize_StripsInvalidAndReserved()
    {
        Assert.Equal("Clip name", FileNameSanitizer.Sanitize(@"Clip<>:""/\|?* name"));
        Assert.Equal("CON_", FileNameSanitizer.Sanitize("CON"));
        Assert.Equal("", FileNameSanitizer.Sanitize("   ...  "));
    }

    [Fact]
    public void OutputTemplate_UsesStemAndEscapesPercent()
    {
        Assert.Equal("%(title)s [%(id)s].%(ext)s", FileNameSanitizer.OutputTemplate(""));
        Assert.Equal("My clip [%(id)s].%(ext)s", FileNameSanitizer.OutputTemplate("My clip"));
        Assert.Equal("100%% [%(id)s].%(ext)s", FileNameSanitizer.OutputTemplate("100%"));
    }
}
