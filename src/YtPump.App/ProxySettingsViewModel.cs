using System.Collections.ObjectModel;
using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using YtPump.App.Loc;
using YtPump.Core.Proxy;
using YtPump.Core.Settings;
using YtPump.Core.Tools;
using YtPump.Core.YtDlp;

namespace YtPump.App;

public partial class ProxySettingsViewModel : ObservableObject
{
    private readonly ToolsStatus _tools;
    private readonly ProxyTester _tester;
    private readonly string? _cookiesFromBrowser;
    private readonly AppSettings _appSettings;
    private readonly Action _persist;

    public ProxySettingsViewModel(
        AppSettings appSettings,
        Action persist,
        ToolsStatus tools,
        YtDlpProcess ytDlp,
        string? cookiesFromBrowser = null)
    {
        _tools = tools;
        _tester = new ProxyTester(ytDlp);
        _cookiesFromBrowser = cookiesFromBrowser;
        _appSettings = appSettings;
        _persist = persist;
        TypeOptions =
        [
            new OptionItem("http", "HTTP"),
            new OptionItem("https", "HTTPS"),
            new OptionItem("socks5h", "SOCKS5")
        ];
        ActiveId = appSettings.ActiveProxyId;
        Profiles = new ObservableCollection<ProxyProfileItem>(
            appSettings.Proxies.Select(p => ProxyProfileItem.From(p, this)));
        if (Profiles.Count > 0 && Profiles.All(p => p.Id != ActiveId))
        {
            ActiveId = Profiles[0].Id;
        }

        LocalizationService.Instance.PropertyChanged += (_, _) =>
        {
            OnPropertyChanged(nameof(IsRussian));
            OnPropertyChanged(nameof(IsEnglish));
            foreach (var profile in Profiles)
            {
                profile.NotifyHeader();
            }

            Draft?.NotifyHeader();
        };
    }

    public ObservableCollection<OptionItem> TypeOptions { get; }
    public ObservableCollection<ProxyProfileItem> Profiles { get; }
    public string ActiveId { get; private set; }

    [ObservableProperty] private ProxyProfileItem? draft;
    [ObservableProperty] private bool isBusy;

    public bool IsFormEnabled => !IsBusy;
    public bool HasDraft => Draft != null;
    public bool CanBeginDraft => !IsBusy && Draft == null && Profiles.Count < ProxyCatalog.MaxCount;

    public bool IsRussian
    {
        get => LocalizationService.Instance.Language == "ru";
        set
        {
            if (!value)
            {
                return;
            }

            LocalizationService.Instance.SetLanguage("ru");
            _appSettings.Language = "ru";
            _persist();
        }
    }

    public bool IsEnglish
    {
        get => LocalizationService.Instance.Language == "en";
        set
        {
            if (!value)
            {
                return;
            }

            LocalizationService.Instance.SetLanguage("en");
            _appSettings.Language = "en";
            _persist();
        }
    }

    public void SetActive(ProxyProfileItem item)
    {
        if (item.IsDraft)
        {
            item.NotifyActive();
            return;
        }

        ActiveId = item.Id;
        foreach (var profile in Profiles)
        {
            profile.NotifyActive();
        }

        PersistProfiles();
    }

    public void CollapseOthers(ProxyProfileItem opened)
    {
        foreach (var profile in Profiles)
        {
            if (!ReferenceEquals(profile, opened) && profile.IsExpanded)
            {
                profile.IsExpanded = false;
            }
        }
    }

    [RelayCommand(CanExecute = nameof(CanBeginDraft))]
    private void BeginDraft()
    {
        if (!CanBeginDraft)
        {
            return;
        }

        foreach (var profile in Profiles)
        {
            profile.IsExpanded = false;
        }

        var created = ProxyProfileItem.From(ProxyCatalog.Create(), this);
        created.IsDraft = true;
        Draft = created;
    }

    [RelayCommand]
    private void CancelDraft()
    {
        Draft = null;
    }

    [RelayCommand]
    private void SaveProfile(ProxyProfileItem? item)
    {
        if (item == null)
        {
            return;
        }

        if (!TryValidate(item, out var errorKey))
        {
            item.StatusText = LocalizationService.Instance[errorKey];
            return;
        }

        if (item.IsDraft)
        {
            if (Profiles.Count >= ProxyCatalog.MaxCount)
            {
                item.StatusText = LocalizationService.Instance["ProxyListFull"];
                return;
            }

            item.IsDraft = false;
            item.StatusText = "";
            Profiles.Add(item);
            Draft = null;
            if (Profiles.Count == 1)
            {
                ActiveId = item.Id;
            }

            item.IsExpanded = true;
        }
        else
        {
            item.StatusText = "";
        }

        PersistProfiles();
        NotifyListState();
        foreach (var profile in Profiles)
        {
            profile.NotifyActive();
        }
    }

    [RelayCommand]
    private void DeleteProfile(ProxyProfileItem? item)
    {
        if (item == null || item.IsDraft)
        {
            return;
        }

        var label = item.HeaderTitle;
        if (item.EndpointLabel.Length > 0 && !string.Equals(label, item.EndpointLabel, StringComparison.Ordinal))
        {
            label = $"{label} ({item.EndpointLabel})";
        }

        var confirm = MessageBox.Show(
            LocalizationService.Instance.Format("ProxyDeleteConfirm", label),
            LocalizationService.Instance["ProxyDelete"],
            MessageBoxButton.YesNo,
            MessageBoxImage.Question,
            MessageBoxResult.No);
        if (confirm != MessageBoxResult.Yes)
        {
            return;
        }

        var wasActive = string.Equals(ActiveId, item.Id, StringComparison.Ordinal);
        Profiles.Remove(item);
        if (wasActive)
        {
            ActiveId = Profiles.Count == 0 ? "" : Profiles[0].Id;
        }

        PersistProfiles();
        NotifyListState();
        foreach (var profile in Profiles)
        {
            profile.NotifyActive();
        }
    }

    [RelayCommand]
    private async Task TestProfileAsync(ProxyProfileItem? item)
    {
        if (item == null || IsBusy)
        {
            return;
        }

        if (!TryValidate(item, out var errorKey) || !int.TryParse(item.PortText.Trim(), out var port))
        {
            item.StatusText = LocalizationService.Instance[errorKey];
            return;
        }

        IsBusy = true;
        item.IsTesting = true;
        item.StatusText = LocalizationService.Instance["ProxyTesting"];
        NotifyListState();
        try
        {
            var result = await _tester.TestAsync(
                BuildUrl(item)!,
                item.Host.Trim(),
                port,
                _tools,
                CancellationToken.None,
                _cookiesFromBrowser);
            item.StatusText = result.Stage switch
            {
                ProxyTestStage.Ok => LocalizationService.Instance["ProxyTestOk"],
                ProxyTestStage.TcpFailed => LocalizationService.Instance["ProxyTestTcpFailed"],
                ProxyTestStage.YtDlpMissing => LocalizationService.Instance["ProxyTestYtDlpMissing"],
                _ => LocalizationService.Instance["ProxyTestYtDlpFailed"]
            };
        }
        finally
        {
            item.IsTesting = false;
            IsBusy = false;
            NotifyListState();
        }
    }

    partial void OnIsBusyChanged(bool value)
    {
        OnPropertyChanged(nameof(IsFormEnabled));
        NotifyListState();
    }

    partial void OnDraftChanged(ProxyProfileItem? value)
    {
        OnPropertyChanged(nameof(HasDraft));
        NotifyListState();
    }

    private void PersistProfiles()
    {
        _appSettings.Proxies = Profiles.Select(p => p.ToProfile()).ToList();
        _appSettings.ActiveProxyId = ActiveId;
        _appSettings.Proxy = null;
        ProxyCatalog.Normalize(_appSettings);
        ActiveId = _appSettings.ActiveProxyId;
        _persist();
    }

    private void NotifyListState()
    {
        OnPropertyChanged(nameof(CanBeginDraft));
        BeginDraftCommand.NotifyCanExecuteChanged();
    }

    private static bool TryValidate(ProxyProfileItem profile, out string errorKey)
    {
        errorKey = "ProxyInvalid";
        return TryBuildUrl(profile, out _, out errorKey);
    }

    private static string? BuildUrl(ProxyProfileItem profile) =>
        TryBuildUrl(profile, out var url, out _) ? url : null;

    private static bool TryBuildUrl(ProxyProfileItem profile, out string url, out string errorKey)
    {
        url = "";
        errorKey = "ProxyInvalid";
        if (!int.TryParse(profile.PortText.Trim(), out var port))
        {
            errorKey = "ProxyInvalidPort";
            return false;
        }

        if (!ProxyUrlBuilder.TryBuild(
                profile.TypeId,
                profile.Host,
                port,
                profile.Username,
                profile.Password,
                out url,
                out var error))
        {
            errorKey = error switch
            {
                ProxyUrlError.EmptyHost => "ProxyInvalidHost",
                ProxyUrlError.InvalidPort => "ProxyInvalidPort",
                _ => "ProxyInvalid"
            };
            return false;
        }

        return true;
    }
}
