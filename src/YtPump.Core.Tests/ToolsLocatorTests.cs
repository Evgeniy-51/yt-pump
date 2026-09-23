using YtPump.Core.Tools;

namespace YtPump.Core.Tests;

public sealed class ToolsLocatorTests
{
    [Fact]
    public void Check_AllMissing_WhenToolsDirEmpty()
    {
        var root = Directory.CreateTempSubdirectory("ytpump-tools-missing-");
        try
        {
            Directory.CreateDirectory(Path.Combine(root.FullName, "tools"));
            var status = ToolsLocator.Check(root.FullName);
            Assert.False(status.AllPresent);
            Assert.Equal(4, status.MissingFileNames.Count);
            Assert.Contains(ToolsLocator.YtDlpFileName, status.MissingFileNames);
            Assert.Contains(ToolsLocator.DenoFileName, status.MissingFileNames);
        }
        finally
        {
            Directory.Delete(root.FullName, recursive: true);
        }
    }

    [Fact]
    public void Check_AllPresent_WhenFourExesExist()
    {
        var root = Directory.CreateTempSubdirectory("ytpump-tools-ok-");
        try
        {
            var tools = Directory.CreateDirectory(Path.Combine(root.FullName, "tools"));
            File.WriteAllBytes(Path.Combine(tools.FullName, ToolsLocator.YtDlpFileName), [0]);
            File.WriteAllBytes(Path.Combine(tools.FullName, ToolsLocator.FfmpegFileName), [0]);
            File.WriteAllBytes(Path.Combine(tools.FullName, ToolsLocator.FfprobeFileName), [0]);
            File.WriteAllBytes(Path.Combine(tools.FullName, ToolsLocator.DenoFileName), [0]);

            var status = ToolsLocator.Check(root.FullName);
            Assert.True(status.AllPresent);
            Assert.Empty(status.MissingFileNames);
        }
        finally
        {
            Directory.Delete(root.FullName, recursive: true);
        }
    }

    [Fact]
    public void GetAppDirectory_IsProcessDirectory_NotBaseDirectory()
    {
        var appDir = ToolsLocator.GetAppDirectory();
        var processDir = Path.GetDirectoryName(Environment.ProcessPath);
        Assert.Equal(processDir, appDir);
    }
}
