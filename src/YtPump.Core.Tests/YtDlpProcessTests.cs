using System.Diagnostics;
using YtPump.Core.YtDlp;

namespace YtPump.Core.Tests;

public sealed class YtDlpProcessTests
{
    [Fact]
    public async Task Cancel_KillsProcessTree()
    {
        var ping = Path.Combine(Environment.SystemDirectory, "ping.exe");
        Assert.True(File.Exists(ping));

        var runner = new YtDlpProcess();
        using var cts = new CancellationTokenSource();
        var run = runner.RunAsync(ping, ["-t", "127.0.0.1"], stderrProgress: null, cts.Token);
        await Task.Delay(300);
        await cts.CancelAsync();

        var result = await run;
        Assert.True(result.Canceled);
    }

    [Fact]
    public void KillOnCloseJob_Dispose_KillsAssignedProcess()
    {
        var ping = Path.Combine(Environment.SystemDirectory, "ping.exe");
        using var process = new Process
        {
            StartInfo = new ProcessStartInfo
            {
                FileName = ping,
                ArgumentList = { "-t", "127.0.0.1" },
                CreateNoWindow = true,
                UseShellExecute = false
            }
        };

        Assert.True(process.Start());
        using (var job = KillOnCloseJob.TryAssign(process))
        {
            Assert.NotNull(job);
            Assert.False(process.HasExited);
        }

        Assert.True(process.WaitForExit(5000));
    }
}

