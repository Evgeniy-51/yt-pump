using YtPump.Core.Validation;

namespace YtPump.Core.Tests;

public sealed class DirectoryValidatorTests
{
    [Fact]
    public void Check_Empty_IsEmpty() =>
        Assert.Equal(DirectoryStatus.Empty, DirectoryValidator.Check("  "));

    [Fact]
    public void Check_Missing_IsMissing()
    {
        var path = Path.Combine(Path.GetTempPath(), "ytpump-missing-" + Guid.NewGuid().ToString("N"));
        Assert.Equal(DirectoryStatus.Missing, DirectoryValidator.Check(path));
    }

    [Fact]
    public void Check_File_IsNotADirectory()
    {
        var file = Path.GetTempFileName();
        try
        {
            Assert.Equal(DirectoryStatus.NotADirectory, DirectoryValidator.Check(file));
        }
        finally
        {
            File.Delete(file);
        }
    }

    [Fact]
    public void Check_WritableDirectory_IsOk()
    {
        var dir = Directory.CreateTempSubdirectory("ytpump-dir-");
        try
        {
            Assert.Equal(DirectoryStatus.Ok, DirectoryValidator.Check(dir.FullName));
        }
        finally
        {
            dir.Delete(recursive: true);
        }
    }
}
