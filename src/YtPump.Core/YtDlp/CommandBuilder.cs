using YtPump.Core.Tools;
using YtPump.Core.Validation;

namespace YtPump.Core.YtDlp;

public static class CommandBuilder
{
    public static List<string> CommonArgs(ToolsStatus tools)
    {
        return
        [
            "--ffmpeg-location", tools.ToolsDirectory,
            "--js-runtimes", $"deno:{tools.DenoPath}",
            "--windows-filenames",
            "--no-playlist",
            "--encoding", "utf-8",
            "--newline"
        ];
    }

    public static List<string> Probe(ToolsStatus tools, string url, string? proxyUrl = null, string? cookiesFromBrowser = null)
    {
        var args = CommonArgs(tools);
        args.Add("--skip-download");
        args.Add("-J");
        if (!string.IsNullOrWhiteSpace(proxyUrl))
        {
            args.Add("--proxy");
            args.Add(proxyUrl);
        }

        AddCookiesFromBrowser(args, cookiesFromBrowser);
        args.Add(url);
        return args;
    }

    public const string ProgressTemplate =
        "download:YTP|%(progress.downloaded_bytes)s|%(progress.total_bytes)s|%(progress.eta)s|%(progress.speed)s|%(progress._percent_str)s";

    public static List<string> Download(
        ToolsStatus tools,
        string url,
        string outputDirectory,
        string formatSelector,
        string container,
        string? proxyUrl = null,
        string? subtitleLanguage = null,
        string? cookiesFromBrowser = null,
        string? fileStem = null)
    {
        var args = CommonArgs(tools);
        args.Add("--force-overwrites");
        args.Add("--progress");
        args.Add("--progress-template");
        args.Add(ProgressTemplate);
        args.Add("--print");
        args.Add("after_video:YTPFILE:%(filepath)s");
        args.Add("--print");
        args.Add("after_move:YTPFILE:%(filepath)s");
        args.Add("-f");
        args.Add(formatSelector);
        if (container is "mkv" or "mp4")
        {
            args.Add("--merge-output-format");
            args.Add(container);
        }
        else if (container == "audio")
        {
            args.Add("--extract-audio");
            args.Add("--audio-format");
            args.Add("m4a");
        }

        var subtitle = SubtitleLanguageCodes.Normalize(subtitleLanguage);
        if (subtitle != null)
        {
            args.Add("--write-subs");
            args.Add("--write-auto-subs");
            args.Add("--sub-langs");
            args.Add(subtitle + ".*");
            args.Add("--convert-subs");
            args.Add("srt");
        }

        if (!string.IsNullOrWhiteSpace(proxyUrl))
        {
            args.Add("--proxy");
            args.Add(proxyUrl);
        }

        AddCookiesFromBrowser(args, cookiesFromBrowser);
        args.Add("-P");
        args.Add(outputDirectory);
        args.Add("-o");
        args.Add(FileNameSanitizer.OutputTemplate(fileStem));
        args.Add(url);
        return args;
    }

    private static void AddCookiesFromBrowser(List<string> args, string? cookiesFromBrowser)
    {
        if (string.IsNullOrWhiteSpace(cookiesFromBrowser))
        {
            return;
        }

        args.Add("--cookies-from-browser");
        args.Add(cookiesFromBrowser.Trim());
    }
}
