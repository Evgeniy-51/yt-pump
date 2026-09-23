using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Text.Json;
using System.Windows;
using System.Windows.Input;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Win32;
using YtPump.App.Loc;
using YtPump.Core.Cookies;
using YtPump.Core.Errors;
using YtPump.Core.Proxy;
using YtPump.Core.Settings;
using YtPump.Core.Tools;
using YtPump.Core.Validation;
using YtPump.Core.YtDlp;

namespace YtPump.App;

public partial class MainWindowViewModel : ObservableObject
{
    private const int MaxLogLines = 1500;
    private const int ProbeDebounceMs = 400;
    private readonly List<string> _logLines = [];
    private readonly ToolsStatus _tools;
    private readonly SettingsStore _store;
    private readonly AppSettings _settings;
    private readonly bool _settingsReadOnly;
    private readonly YtDlpProcess _ytDlp = new();
    private readonly IProgress<string> _stderrLog;
    private readonly IProgress<string> _downloadLog;
    private SettingsWarning _settingsWarning;
    private bool _loading = true;
    private bool _rebuildingSubtitles;
    private CancellationTokenSource? _runCts;
    private int _runGeneration;
    private VideoInfo? _videoInfo;
    private string? _probedUrl;
    private string? _outputPath;
    private readonly List<string> _outputPathCandidates = [];
    private bool _selectFileStem;
    private int _probeGeneration;
    private string? _lastAutoProbeUrl;
    private TaskCompletionSource _idle = CompletedIdle();

    public bool IsDownloading { get; private set; }

    internal event EventHandler? FileStemSelectRequested;
    internal event EventHandler? StatusErrorPulsed;

    public LocalizationService Loc { get; } = LocalizationService.Instance;

    public MainWindowViewModel()
        : this(ToolsLocator.Check(), SettingsStore.ForCurrentProcess())
    {
    }

    internal MainWindowViewModel(ToolsStatus tools, SettingsStore store)
    {
        _tools = tools;
        _store = store;
        _stderrLog = new Progress<string>(line => AppendLog(SecretMasker.Mask(line)));
        _downloadLog = new Progress<string>(OnDownloadLine);

        var loaded = _store.Load(UiLanguage.FromOs(), DefaultFolders.GetOutputDirectory());
        _settings = loaded.Settings;
        _settings.Normalize();
        _settingsReadOnly = loaded.IsReadOnly;
        _settingsWarning = loaded.Warning;

        Loc.SetLanguage(_settings.Language);

        QualityOptions = [];
        AudioOptions = [];
        RecentUrls = new ObservableCollection<string>(_settings.RecentUrls);
        ContainerOptions = new ObservableCollection<OptionItem>(CreateContainerOptions());
        qualityId = "";
        audioId = "";
        containerId = _settings.Container;
        outputDirectory = _settings.LastOutputDirectory;
        proxyEnabled = _settings.ProxyEnabled;
        writeSubtitles = false;
        _settings.WriteSubtitlesRu = false;
        subtitleLanguage = _settings.SubtitleLanguage;

        Loc.PropertyChanged += (_, _) =>
        {
            OnPropertyChanged(nameof(IsRussian));
            OnPropertyChanged(nameof(IsEnglish));
            OnPropertyChanged(nameof(ToolsBannerText));
            OnPropertyChanged(nameof(HasMissingTools));
            OnPropertyChanged(nameof(SettingsBannerText));
            OnPropertyChanged(nameof(HasSettingsWarning));
            OnPropertyChanged(nameof(ActiveProxyName));
            OnPropertyChanged(nameof(ShowActiveProxyName));
            RefreshOptionLabels();
            if (!IsBusy)
            {
                ShowCurrentStatus();
            }
        };

        _loading = false;
        WriteStartupLog();
        ShowCurrentStatus();
    }

    public ObservableCollection<OptionItem> QualityOptions { get; }
    public ObservableCollection<OptionItem> AudioOptions { get; }
    public ObservableCollection<OptionItem> ContainerOptions { get; }
    public ObservableCollection<string> RecentUrls { get; }
    public ObservableCollection<SubtitleLangItem> SubtitleChoices { get; } = [];

    public WindowSettings WindowSettings => _settings.Window!;
    public bool IsFormEnabled => !IsBusy;
    public bool AreSelectorsEnabled => !IsBusy && _videoInfo != null && ProbedUrlMatches();
    public bool IsSubtitlesEnabled => !IsBusy && ProbedUrlMatches() && SubtitleChoices.Count > 0;
    public bool ShowSubtitleRadios =>
        WriteSubtitles && SubtitleChoices.Count is > 0 and <= SubtitleLanguageCodes.RadioLimit;
    public bool ShowSubtitleCombo =>
        WriteSubtitles && SubtitleChoices.Count > SubtitleLanguageCodes.RadioLimit;
    public bool HasOutputPath => !string.IsNullOrWhiteSpace(_outputPath) && File.Exists(_outputPath);
    public bool IsProgressIndeterminate => IsBusy && !IsDownloading;
    public Cursor WindowCursor => IsBusy ? Cursors.Wait : Cursors.Arrow;
    public string ActiveProxyName
    {
        get
        {
            var profile = ProxyCatalog.FindActive(_settings);
            if (profile == null)
            {
                return Loc["Proxy"];
            }

            var name = ProxyCatalog.DisplayName(profile);
            return name.Length == 0 ? Loc["Proxy"] : name;
        }
    }

    public bool ShowActiveProxyName => ProxyEnabled;

    [ObservableProperty] private string url = "";
    [ObservableProperty] private string outputDirectory = "";
    [ObservableProperty] private string qualityId = "best";
    [ObservableProperty] private string audioId = "pending";
    [ObservableProperty] private string containerId = "mkv";
    [ObservableProperty] private string fileStem = "";
    [ObservableProperty] private bool writeSubtitles;
    [ObservableProperty] private string subtitleLanguage = "ru";
    [ObservableProperty] private bool proxyEnabled;
    [ObservableProperty] private bool isBusy;
    [ObservableProperty] private double progress;
    [ObservableProperty] private string statusText = "";
    [ObservableProperty] private bool isStatusError;
    [ObservableProperty] private string logText = "";
    [ObservableProperty] private bool isLogExpanded;

    public bool HasMissingTools => !_tools.AllPresent;
    public bool HasSettingsWarning => _settingsWarning != SettingsWarning.None || _settingsReadOnly;

    public string ToolsBannerText =>
        HasMissingTools
            ? Loc.Format("ToolsMissingFormat", string.Join(", ", _tools.MissingFileNames))
            : "";

    public string SettingsBannerText => _settingsWarning switch
    {
        SettingsWarning.CorruptJson => Loc["SettingsCorrupt"],
        SettingsWarning.PortableNotWritable => Loc["SettingsPortableReadOnly"],
        SettingsWarning.IoError => Loc["SettingsIoError"],
        _ when _settingsReadOnly => Loc["SettingsPortableReadOnly"],
        _ => ""
    };

    public bool IsRussian
    {
        get => Loc.Language == "ru";
        set
        {
            if (value)
            {
                Loc.SetLanguage("ru");
                _settings.Language = "ru";
                Persist();
            }
        }
    }

    public bool IsEnglish
    {
        get => Loc.Language == "en";
        set
        {
            if (value)
            {
                Loc.SetLanguage("en");
                _settings.Language = "en";
                Persist();
            }
        }
    }

    [RelayCommand]
    private async Task PasteUrlAsync()
    {
        try
        {
            if (Clipboard.ContainsText())
            {
                Url = Clipboard.GetText().Trim();
            }
        }
        catch (Exception ex)
        {
            AppendLog(ex.Message);
            return;
        }

        await ProbeAsync();
    }

    [RelayCommand]
    private async Task ProbeAsync()
    {
        if (!UrlValidator.TryNormalize(Url, out var normalized, out var urlStatus))
        {
            if (urlStatus == UrlStatus.Empty)
            {
                return;
            }

            SetStatus(Loc["UrlInvalid"], error: true, pulse: true);
            return;
        }

        Url = normalized;
        if (string.Equals(_probedUrl, normalized, StringComparison.OrdinalIgnoreCase) && _videoInfo != null && !IsBusy)
        {
            return;
        }

        _lastAutoProbeUrl = normalized;

        if (IsBusy)
        {
            return;
        }

        if (HasMissingTools)
        {
            SetStatus(Loc["StatusToolsMissing"], error: true, pulse: true);
            return;
        }

        if (!TryGetProxyArgument(out _))
        {
            OpenProxySettings();
            return;
        }

        await RunProbeAsync(normalized);
    }

    [RelayCommand]
    private void BrowseFolder()
    {
        var dialog = new OpenFolderDialog
        {
            Title = Loc["SaveToLabel"]
        };

        if (!string.IsNullOrWhiteSpace(OutputDirectory) && Directory.Exists(OutputDirectory))
        {
            dialog.InitialDirectory = OutputDirectory;
        }

        if (dialog.ShowDialog() != true)
        {
            return;
        }

        var status = DirectoryValidator.Check(dialog.FolderName);
        if (status != DirectoryStatus.Ok)
        {
            SetStatus(FolderStatusText(status), error: true, pulse: true);
            AppendLog(StatusText);
            return;
        }

        OutputDirectory = dialog.FolderName;
        _settings.LastOutputDirectory = OutputDirectory;
        Persist();
        ShowCurrentStatus();
    }

    [RelayCommand]
    private void OpenHelp()
    {
        var window = new HelpWindow
        {
            Owner = Application.Current.MainWindow
        };
        window.ShowDialog();
    }

    [RelayCommand]
    private void OpenProxySettings()
    {
        var cookies = BrowserCookieLocator.Installed(_settings.CookiesBrowser).FirstOrDefault();
        var window = new ProxySettingsWindow(_settings, Persist, _tools, _ytDlp, cookies)
        {
            Owner = Application.Current.MainWindow
        };
        window.ShowDialog();
        NotifyProxyHeader();
    }

    [RelayCommand(CanExecute = nameof(CanDownload))]
    private async Task DownloadAsync()
    {
        if (!UrlValidator.TryNormalize(Url, out var normalized, out _))
        {
            SetStatus(Loc["UrlInvalid"], error: true, pulse: true);
            return;
        }

        Url = normalized;
        var folder = DirectoryValidator.Check(OutputDirectory);
        if (folder != DirectoryStatus.Ok)
        {
            SetStatus(FolderStatusText(folder), error: true, pulse: true);
            AppendLog(StatusText);
            return;
        }

        if (HasMissingTools)
        {
            SetStatus(Loc["StatusToolsMissing"], error: true, pulse: true);
            return;
        }

        if (!TryGetProxyArgument(out _))
        {
            OpenProxySettings();
            return;
        }

        if (_videoInfo == null || !string.Equals(_probedUrl, normalized, StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        await RunDownloadAsync(normalized);
    }

    private bool CanDownload() =>
        !IsBusy && !HasMissingTools && _videoInfo != null && ProbedUrlMatches();

    private bool ProbedUrlMatches() =>
        UrlValidator.TryNormalize(Url, out var normalized, out _)
        && string.Equals(_probedUrl, normalized, StringComparison.OrdinalIgnoreCase);

    [RelayCommand(CanExecute = nameof(CanOpenFolder))]
    private void OpenFolder()
    {
        if (!HasOutputPath || _outputPath is null)
        {
            return;
        }

        Process.Start(new ProcessStartInfo
        {
            FileName = "explorer.exe",
            Arguments = "/select,\"" + _outputPath + "\"",
            UseShellExecute = true
        });
    }

    private bool CanOpenFolder() => HasOutputPath;

    [RelayCommand(CanExecute = nameof(CanCancel))]
    private void Cancel() => CancelInFlight();

    private bool CanCancel() => IsBusy;

    internal void CancelInFlight() => _runCts?.Cancel();

    internal Task WaitUntilIdleAsync(TimeSpan timeout)
    {
        if (!IsBusy)
        {
            return Task.CompletedTask;
        }

        return _idle.Task.WaitAsync(timeout);
    }

    private void BeginWork(bool downloading)
    {
        IsDownloading = downloading;
        _idle = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        IsBusy = true;
    }

    private void EndWork()
    {
        IsDownloading = false;
        IsBusy = false;
        _idle.TrySetResult();
    }

    private static TaskCompletionSource CompletedIdle()
    {
        var idle = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        idle.SetResult();
        return idle;
    }

    partial void OnUrlChanged(string value)
    {
        DownloadCommand.NotifyCanExecuteChanged();
        OnPropertyChanged(nameof(AreSelectorsEnabled));
        OnPropertyChanged(nameof(IsSubtitlesEnabled));
        if (_loading)
        {
            return;
        }

        if (!UrlValidator.TryNormalize(value, out var normalized, out _)
            || !string.Equals(_lastAutoProbeUrl, normalized, StringComparison.OrdinalIgnoreCase))
        {
            _lastAutoProbeUrl = null;
        }

        var generation = ++_probeGeneration;
        _ = ProbeAfterPauseAsync(generation);
    }

    private async Task ProbeAfterPauseAsync(int generation)
    {
        await Task.Delay(ProbeDebounceMs);
        if (generation != _probeGeneration || _loading || IsBusy)
        {
            return;
        }

        if (!UrlValidator.TryNormalize(Url, out var normalized, out _))
        {
            return;
        }

        if (string.Equals(_lastAutoProbeUrl, normalized, StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        if (string.Equals(_probedUrl, normalized, StringComparison.OrdinalIgnoreCase) && _videoInfo != null)
        {
            return;
        }

        _lastAutoProbeUrl = normalized;
        await ProbeAsync();
    }

    partial void OnIsBusyChanged(bool value)
    {
        DownloadCommand.NotifyCanExecuteChanged();
        CancelCommand.NotifyCanExecuteChanged();
        OpenFolderCommand.NotifyCanExecuteChanged();
        OnPropertyChanged(nameof(IsFormEnabled));
        OnPropertyChanged(nameof(AreSelectorsEnabled));
        OnPropertyChanged(nameof(IsSubtitlesEnabled));
        OnPropertyChanged(nameof(HasOutputPath));
        OnPropertyChanged(nameof(IsProgressIndeterminate));
        OnPropertyChanged(nameof(WindowCursor));
    }

    partial void OnProxyEnabledChanged(bool value)
    {
        _settings.ProxyEnabled = value;
        Persist();
        NotifyProxyHeader();
        if (!_loading && value)
        {
            var active = ProxyCatalog.FindActive(_settings);
            if (active == null || string.IsNullOrWhiteSpace(active.Host))
            {
                OpenProxySettings();
            }
        }
    }

    partial void OnContainerIdChanged(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return;
        }

        _settings.Container = value;
        Persist();
    }

    partial void OnQualityIdChanged(string value)
    {
        if (_loading || string.IsNullOrWhiteSpace(value) || value == "pending")
        {
            return;
        }

        _settings.PreferredHeight = value == "best" ? null : int.TryParse(value, out var height) ? height : null;
        Persist();
    }

    partial void OnWriteSubtitlesChanged(bool value)
    {
        NotifySubtitlePicker();
    }

    partial void OnSubtitleLanguageChanged(string value)
    {
        if (_rebuildingSubtitles)
        {
            return;
        }

        var normalized = SubtitleLanguageCodes.Normalize(value) ?? "ru";
        if (!string.Equals(normalized, value, StringComparison.Ordinal))
        {
            SubtitleLanguage = normalized;
            return;
        }

        SyncSubtitleChecks();
        if (_loading)
        {
            return;
        }

        _settings.SubtitleLanguage = normalized;
        Persist();
    }

    public void SelectSubtitle(string code)
    {
        if (string.Equals(SubtitleLanguage, code, StringComparison.Ordinal))
        {
            SyncSubtitleChecks();
            return;
        }

        SubtitleLanguage = code;
    }

    public void PersistWindow(WindowSettings window)
    {
        _settings.Window = window;
        Persist();
    }

    private bool TryGetProxyArgument(out string? proxyUrl)
    {
        proxyUrl = null;
        if (!_settings.ProxyEnabled)
        {
            return true;
        }

        var proxy = ProxyCatalog.FindActive(_settings);
        if (proxy == null)
        {
            SetStatus(Loc["ProxyInvalid"], error: true, pulse: true);
            AppendLog(StatusText);
            return false;
        }

        var password = DpapiSecretStore.Unprotect(proxy.PasswordProtected);
        if (!ProxyUrlBuilder.TryBuild(
                proxy.Type,
                proxy.Host,
                proxy.Port,
                proxy.Username,
                password,
                out var built,
                out _))
        {
            SetStatus(Loc["ProxyInvalid"], error: true, pulse: true);
            AppendLog(StatusText);
            return false;
        }

        proxyUrl = built;
        return true;
    }

    private IEnumerable<string?> CookieCandidates()
    {
        var installed = BrowserCookieLocator.Installed(_settings.CookiesBrowser);
        if (installed.Count == 0)
        {
            yield return null;
            yield break;
        }

        foreach (var name in installed)
        {
            yield return name;
        }
    }

    private void RememberCookiesBrowser(string? browser)
    {
        if (string.IsNullOrWhiteSpace(browser))
        {
            return;
        }

        AppendLog(Loc.Format("LogCookiesBrowser", browser));
        _settings.CookiesBrowser = browser;
        Persist();
    }

    private async Task RunProbeAsync(string url)
    {
        var generation = ++_runGeneration;
        _runCts?.Cancel();
        _runCts?.Dispose();
        var runCts = new CancellationTokenSource();
        _runCts = runCts;

        Progress = 0;
        BeginWork(downloading: false);
        SetOutputPath(null);
        SetStatus(Loc["StatusProbing"]);
        WriteSubtitles = false;
        _videoInfo = null;
        _probedUrl = null;
        FileStem = "";
        RebuildQualityAndAudio();

        if (!TryGetProxyArgument(out var proxyUrl))
        {
            EndWork();
            return;
        }

        var lastKind = ErrorKind.Unknown;
        var sawAuth = false;
        try
        {
            foreach (var browser in CookieCandidates())
            {
                if (generation != _runGeneration)
                {
                    return;
                }

                if (runCts.IsCancellationRequested)
                {
                    SetStatus(ErrorText(ErrorKind.Cancelled));
                    AppendLog(StatusText);
                    return;
                }

                if (browser != null)
                {
                    SetStatus(Loc.Format("StatusCookies", browser));
                }

                var args = CommandBuilder.Probe(_tools, url, proxyUrl, browser);
                AppendLog("yt-dlp " + string.Join(' ', args.Select(SecretMasker.Mask)));

                using var attemptCts = CancellationTokenSource.CreateLinkedTokenSource(runCts.Token);
                attemptCts.CancelAfter(TimeSpan.FromSeconds(60));
                var result = await _ytDlp.RunAsync(_tools.YtDlpPath, args, _stderrLog, attemptCts.Token);
                if (generation != _runGeneration)
                {
                    return;
                }

                if (result.Canceled)
                {
                    if (runCts.IsCancellationRequested)
                    {
                        SetStatus(ErrorText(ErrorKind.Cancelled));
                        AppendLog(StatusText);
                        return;
                    }

                    SetStatus(ErrorText(ErrorKind.Network), error: true, pulse: true);
                    AppendLog(StatusText);
                    return;
                }

                if (result.ExitCode != 0)
                {
                    lastKind = ErrorClassifier.Classify(result.ExitCode, result.StandardError, canceled: false);
                    if (lastKind == ErrorKind.Auth)
                    {
                        sawAuth = true;
                    }
                    if (browser != null && lastKind == ErrorKind.Cookies)
                    {
                        AppendLog(Loc.Format("LogCookiesFailed", browser));
                    }

                    if (ErrorClassifier.ShouldRetryCookies(lastKind) && browser != null)
                    {
                        continue;
                    }

                    SetStatus(ErrorText(lastKind), error: true, pulse: true);
                    AppendLog(StatusText);
                    return;
                }

                RememberCookiesBrowser(browser);
                _videoInfo = MetadataParser.Parse(result.StandardOutput);
                _probedUrl = url;
                FileStem = FileNameSanitizer.Sanitize(_videoInfo.Title);
                RememberRecentUrl(url);
                RebuildQualityAndAudio();
                ApplyPreferredQuality();
                SetStatus("");
                _selectFileStem = true;
                return;
            }

            SetStatus(ErrorText(sawAuth ? ErrorKind.Auth : lastKind), error: true, pulse: true);
            AppendLog(StatusText);
        }
        catch (FileNotFoundException ex)
        {
            SetStatus(ErrorText(ErrorKind.JsRuntime), error: true, pulse: true);
            AppendLog(SecretMasker.Mask(ex.Message));
        }
        catch (JsonException)
        {
            SetStatus(ErrorText(ErrorKind.Unknown), error: true, pulse: true);
            AppendLog(StatusText);
        }
        finally
        {
            if (generation == _runGeneration)
            {
                EndWork();
                if (_selectFileStem)
                {
                    _selectFileStem = false;
                    FileStemSelectRequested?.Invoke(this, EventArgs.Empty);
                }
            }
        }
    }

    private async Task RunDownloadAsync(string url)
    {
        var generation = ++_runGeneration;
        _runCts?.Cancel();
        _runCts?.Dispose();
        var runCts = new CancellationTokenSource();
        _runCts = runCts;
        SetOutputPath(null);
        Progress = 0;
        BeginWork(downloading: true);
        SetStatus(Loc["StatusDownloadingStart"]);

        if (!TryGetProxyArgument(out var proxyUrl))
        {
            EndWork();
            return;
        }

        var format = FormatSelector.Build(ContainerId, QualityId, AudioId);
        var subtitle = WriteSubtitles && _videoInfo?.SubtitleLanguages.Contains(SubtitleLanguage) == true
            ? SubtitleLanguage
            : null;
        var cookies = BrowserCookieLocator.NormalizeName(_settings.CookiesBrowser)
                      ?? BrowserCookieLocator.Installed().FirstOrDefault();
        var args = CommandBuilder.Download(
            _tools, url, OutputDirectory, format, ContainerId, proxyUrl, subtitle, cookies, FileStem);
        AppendLog("yt-dlp " + string.Join(' ', args.Select(SecretMasker.Mask)));

        try
        {
            var result = await _ytDlp.RunAsync(
                _tools.YtDlpPath,
                args,
                _downloadLog,
                runCts.Token,
                _downloadLog);

            CaptureOutputPath(result.StandardOutput);
            CaptureOutputPath(result.StandardError);

            if (generation != _runGeneration)
            {
                return;
            }

            if (result.Canceled)
            {
                SetStatus(Loc["StatusPartFiles"], error: true, pulse: true);
                AppendLog(StatusText);
                return;
            }

            if (result.ExitCode != 0)
            {
                var kind = ErrorClassifier.Classify(result.ExitCode, result.StandardError, canceled: false);
                SetStatus(ErrorText(kind), error: kind != ErrorKind.Cancelled, pulse: kind != ErrorKind.Cancelled);
                AppendLog(StatusText);
                return;
            }

            Progress = 100;
            _outputPath = OutputPathResolver.Resolve(
                _outputPathCandidates,
                OutputDirectory,
                _videoInfo?.Id);
            OnPropertyChanged(nameof(HasOutputPath));
            OpenFolderCommand.NotifyCanExecuteChanged();
            if (HasOutputPath)
            {
                SetStatus(Loc.Format("StatusDone", _outputPath!));
            }
            else
            {
                SetStatus(Loc["StatusDoneUnknownPath"]);
            }
        }
        catch (FileNotFoundException ex)
        {
            SetStatus(ErrorText(ErrorKind.JsRuntime), error: true, pulse: true);
            AppendLog(SecretMasker.Mask(ex.Message));
        }
        finally
        {
            if (generation == _runGeneration)
            {
                EndWork();
            }
        }
    }

    private void OnDownloadLine(string line)
    {
        var masked = SecretMasker.Mask(line);
        if (ProgressParser.TryParseProgress(masked, out var progress))
        {
            if (progress.Percent is double percent)
            {
                Progress = Math.Clamp(percent, 0, 100);
            }

            SetStatus(Loc.Format(
                "StatusDownloading",
                progress.Percent?.ToString("0.0") ?? "?",
                progress.Speed ?? "—",
                progress.Eta ?? "—"));
            return;
        }

        if (ProgressParser.TryParseOutputPath(masked, out var path))
        {
            SetOutputPath(path);
            return;
        }

        AppendLog(masked);
        if (masked.Contains("Merging formats", StringComparison.OrdinalIgnoreCase)
            || masked.Contains("[ffmpeg]", StringComparison.OrdinalIgnoreCase)
            || masked.Contains("Merger", StringComparison.OrdinalIgnoreCase))
        {
            SetStatus(Loc["StatusMerging"]);
        }
    }

    private void CaptureOutputPath(string? text)
    {
        if (string.IsNullOrEmpty(text))
        {
            return;
        }

        foreach (var line in text.Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries))
        {
            if (ProgressParser.TryParseOutputPath(line, out var path))
            {
                SetOutputPath(path);
            }
        }
    }

    private void SetOutputPath(string? path)
    {
        if (path is null)
        {
            _outputPathCandidates.Clear();
            _outputPath = null;
        }
        else
        {
            _outputPathCandidates.Add(path);
            _outputPath = path;
        }

        OnPropertyChanged(nameof(HasOutputPath));
        OpenFolderCommand.NotifyCanExecuteChanged();
    }

    private void ApplyPreferredQuality()
    {
        var previous = _loading;
        _loading = true;
        if (_settings.PreferredHeight is int height
            && QualityOptions.Any(o => o.Id == height.ToString()))
        {
            QualityId = height.ToString();
        }
        else
        {
            QualityId = "best";
        }

        var original = AudioOptions.FirstOrDefault(o => o.Id.EndsWith("|orig", StringComparison.Ordinal));
        AudioId = original?.Id ?? AudioOptions.FirstOrDefault()?.Id ?? "";
        _loading = previous;
    }

    private void RememberRecentUrl(string url)
    {
        var current = Url;
        var next = UrlHistory.Remember(RecentUrls, url);
        _settings.RecentUrls = next;
        if (!RecentUrls.SequenceEqual(next, StringComparer.OrdinalIgnoreCase))
        {
            RecentUrls.Clear();
            foreach (var item in next)
            {
                RecentUrls.Add(item);
            }
        }

        if (!string.Equals(Url, current, StringComparison.Ordinal))
        {
            Url = current;
        }

        Persist();
    }

    private void NotifyProxyHeader()
    {
        OnPropertyChanged(nameof(ActiveProxyName));
        OnPropertyChanged(nameof(ShowActiveProxyName));
    }

    private void Persist()
    {
        if (_loading)
        {
            return;
        }

        if (!_store.TrySave(_settings, out var error))
        {
            _settingsWarning = error;
            OnPropertyChanged(nameof(HasSettingsWarning));
            OnPropertyChanged(nameof(SettingsBannerText));
            AppendLog(SettingsBannerText);
        }
    }

    private void RefreshOptionLabels()
    {
        var previousLoading = _loading;
        _loading = true;
        var id = ContainerId;
        ContainerOptions.Clear();
        foreach (var item in CreateContainerOptions())
        {
            ContainerOptions.Add(item);
        }

        ContainerId = id;
        RebuildQualityAndAudio();
        _loading = previousLoading;
    }

    private void RebuildQualityAndAudio()
    {
        var qid = QualityId;
        var aid = AudioId;
        QualityOptions.Clear();
        AudioOptions.Clear();
        if (_videoInfo == null)
        {
            QualityId = "";
            AudioId = "";
            FileStem = "";
            RebuildSubtitles();
            OnPropertyChanged(nameof(AreSelectorsEnabled));
            DownloadCommand.NotifyCanExecuteChanged();
            return;
        }

        QualityOptions.Add(new OptionItem("best", Loc["QualityBest"]));
        foreach (var height in _videoInfo.Heights)
        {
            QualityOptions.Add(new OptionItem(height.ToString(), $"{height}p"));
        }

        if (_videoInfo.AudioTracks.Count == 0)
        {
            AudioOptions.Add(new OptionItem("pending", Loc["AudioPending"]));
        }
        else
        {
            foreach (var track in _videoInfo.AudioTracks)
            {
                AudioOptions.Add(new OptionItem(track.Id, FormatAudioLabel(track)));
            }
        }

        QualityId = QualityOptions.Any(o => o.Id == qid) ? qid : "best";
        AudioId = AudioOptions.Any(o => o.Id == aid) ? aid : AudioOptions[0].Id;
        RebuildSubtitles();
        OnPropertyChanged(nameof(AreSelectorsEnabled));
        DownloadCommand.NotifyCanExecuteChanged();
    }

    private void RebuildSubtitles()
    {
        var codes = _videoInfo?.SubtitleLanguages ?? [];
        var preferred = SubtitleLanguage;
        _rebuildingSubtitles = true;
        try
        {
            SubtitleChoices.Clear();
            foreach (var code in codes)
            {
                SubtitleChoices.Add(new SubtitleLangItem(code));
            }

            var selected = codes.Contains(preferred) ? preferred
                : codes.Contains("ru") ? "ru"
                : codes.Contains("en") ? "en"
                : codes.FirstOrDefault() ?? preferred;

            SubtitleLanguage = selected;
            SyncSubtitleChecks();
        }
        finally
        {
            _rebuildingSubtitles = false;
        }

        if (!_loading && !string.Equals(_settings.SubtitleLanguage, SubtitleLanguage, StringComparison.Ordinal))
        {
            _settings.SubtitleLanguage = SubtitleLanguage;
            Persist();
        }

        NotifySubtitlePicker();
    }

    private void SyncSubtitleChecks()
    {
        foreach (var item in SubtitleChoices)
        {
            item.SetSelected(item.Code == SubtitleLanguage);
        }
    }

    private void NotifySubtitlePicker()
    {
        OnPropertyChanged(nameof(IsSubtitlesEnabled));
        OnPropertyChanged(nameof(ShowSubtitleRadios));
        OnPropertyChanged(nameof(ShowSubtitleCombo));
    }

    private string FormatAudioLabel(AudioTrackInfo track)
    {
        var lang = string.IsNullOrWhiteSpace(track.Language) ? "und" : track.Language;
        return Loc.Format(track.IsOriginal ? "AudioOriginal" : "AudioDub", lang);
    }

    private IEnumerable<OptionItem> CreateContainerOptions() =>
    [
        new("mkv", Loc["ContainerMkv"]),
        new("mp4", Loc["ContainerMp4"]),
        new("audio", Loc["ContainerAudio"])
    ];

    private string BrowserSessionHelp() =>
        BrowserCookieLocator.Installed().Count == 0
            ? Loc["ErrorCookiesMissing"]
            : Loc["ErrorAuth"];

    private string ErrorText(ErrorKind kind) => kind switch
    {
        ErrorKind.Network => Loc["ErrorNetwork"],
        ErrorKind.Proxy => Loc["ErrorProxy"],
        ErrorKind.Unavailable => Loc["ErrorUnavailable"],
        ErrorKind.Auth or ErrorKind.Cookies => BrowserSessionHelp(),
        ErrorKind.JsRuntime => Loc["ErrorJsRuntime"],
        ErrorKind.Ffmpeg => Loc["ErrorFfmpeg"],
        ErrorKind.Cancelled => Loc["ErrorCancelled"],
        _ => Loc["ErrorUnknown"]
    };

    private void ShowCurrentStatus()
    {
        var text = CurrentStatus();
        var error = text.Length > 0 && (HasMissingTools || !HasOutputPath);
        SetStatus(text, error);
    }

    private void SetStatus(string text, bool error = false, bool pulse = false)
    {
        var changed = !string.Equals(StatusText, text, StringComparison.Ordinal) || IsStatusError != error;
        StatusText = text;
        IsStatusError = error;
        if (error && (pulse || changed))
        {
            StatusErrorPulsed?.Invoke(this, EventArgs.Empty);
        }
    }

    private string CurrentStatus()
    {
        if (HasMissingTools)
        {
            return Loc["StatusToolsMissing"];
        }

        if (HasOutputPath)
        {
            return Loc.Format("StatusDone", _outputPath!);
        }

        if (_videoInfo != null)
        {
            return "";
        }

        var folder = DirectoryValidator.Check(OutputDirectory);
        if (folder != DirectoryStatus.Ok)
        {
            return FolderStatusText(folder);
        }

        return "";
    }

    private string FolderStatusText(DirectoryStatus status) => status switch
    {
        DirectoryStatus.Empty => Loc["FolderEmpty"],
        DirectoryStatus.NotADirectory => Loc["FolderNotADirectory"],
        DirectoryStatus.NotWritable => Loc["FolderNotWritable"],
        DirectoryStatus.Missing => Loc["FolderUnavailable"],
        _ => ""
    };

    private void WriteStartupLog()
    {
        AppendLog(Loc.Format("LogAppDir", _tools.AppDirectory));
        AppendLog(Loc.Format("LogToolsDir", _tools.ToolsDirectory));
        AppendLog(Loc[_store.IsPortable ? "LogPortableMode" : "LogAppDataMode"]);
        AppendLog(Loc.Format("LogSettingsPath", _store.SettingsPath));
        if (_tools.AllPresent)
        {
            AppendLog(Loc["LogAllTools"]);
        }
        else
        {
            foreach (var name in _tools.MissingFileNames)
            {
                AppendLog(Loc.Format("LogMissing", name));
            }
        }

        if (HasSettingsWarning)
        {
            AppendLog(SettingsBannerText);
        }

        var folder = DirectoryValidator.Check(OutputDirectory);
        if (folder != DirectoryStatus.Ok)
        {
            AppendLog(FolderStatusText(folder));
        }
    }

    private void AppendLog(string line)
    {
        if (string.IsNullOrWhiteSpace(line))
        {
            return;
        }

        _logLines.Add($"{DateTime.Now:HH:mm:ss}  {SecretMasker.Mask(line)}");
        if (_logLines.Count > MaxLogLines)
        {
            _logLines.RemoveRange(0, _logLines.Count - MaxLogLines);
        }

        var builder = new StringBuilder();
        foreach (var entry in _logLines)
        {
            builder.AppendLine(entry);
        }

        LogText = builder.ToString();
    }
}
