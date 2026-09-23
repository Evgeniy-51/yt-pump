using System.Net.Sockets;
using YtPump.Core.Tools;
using YtPump.Core.YtDlp;

namespace YtPump.Core.Proxy;

public enum ProxyTestStage
{
    TcpFailed,
    YtDlpMissing,
    YtDlpFailed,
    Ok
}

public readonly record struct ProxyTestResult(bool Success, ProxyTestStage Stage);

public sealed class ProxyTester
{
    public const string TestVideoUrl = "https://www.youtube.com/watch?v=jNQXAC9IVRw";

    private readonly YtDlpProcess _ytDlp;

    public ProxyTester(YtDlpProcess ytDlp)
    {
        _ytDlp = ytDlp;
    }

    public async Task<ProxyTestResult> TestAsync(
        string proxyUrl,
        string host,
        int port,
        ToolsStatus tools,
        CancellationToken cancellationToken,
        string? cookiesFromBrowser = null)
    {
        try
        {
            using var tcp = new TcpClient();
            using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            timeout.CancelAfter(TimeSpan.FromSeconds(5));
            await tcp.ConnectAsync(host, port, timeout.Token).ConfigureAwait(false);
        }
        catch (Exception ex) when (ex is SocketException or OperationCanceledException or ArgumentException)
        {
            return new ProxyTestResult(false, ProxyTestStage.TcpFailed);
        }

        if (!tools.YtDlpExists)
        {
            return new ProxyTestResult(false, ProxyTestStage.YtDlpMissing);
        }

        var args = CommandBuilder.Probe(tools, TestVideoUrl, proxyUrl, cookiesFromBrowser);
        using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        cts.CancelAfter(TimeSpan.FromSeconds(45));
        try
        {
            var result = await _ytDlp.RunAsync(tools.YtDlpPath, args, stderrProgress: null, cts.Token)
                .ConfigureAwait(false);
            if (result.Canceled || result.ExitCode != 0)
            {
                return new ProxyTestResult(false, ProxyTestStage.YtDlpFailed);
            }

            return new ProxyTestResult(true, ProxyTestStage.Ok);
        }
        catch (Exception ex) when (ex is FileNotFoundException or OperationCanceledException)
        {
            return new ProxyTestResult(false, ProxyTestStage.YtDlpFailed);
        }
    }
}
