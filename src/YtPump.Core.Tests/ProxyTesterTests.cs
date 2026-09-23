using YtPump.Core.Proxy;
using YtPump.Core.Tools;
using YtPump.Core.YtDlp;

namespace YtPump.Core.Tests;

public sealed class ProxyTesterTests
{
    [Fact]
    public async Task UnreachablePort_FailsAtTcp()
    {
        var tools = new ToolsStatus(@"C:\app", @"C:\app\tools", @"C:\missing\yt-dlp.exe",
            @"C:\missing\ffmpeg.exe", @"C:\missing\ffprobe.exe", @"C:\missing\deno.exe");
        var tester = new ProxyTester(new YtDlpProcess());
        var result = await tester.TestAsync("http://127.0.0.1:1", "127.0.0.1", 1, tools, CancellationToken.None);
        Assert.False(result.Success);
        Assert.Equal(ProxyTestStage.TcpFailed, result.Stage);
    }
}
