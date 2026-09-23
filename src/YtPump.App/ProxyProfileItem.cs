using CommunityToolkit.Mvvm.ComponentModel;
using YtPump.App.Loc;
using YtPump.Core.Settings;

namespace YtPump.App;

public partial class ProxyProfileItem : ObservableObject
{
    private ProxySettingsViewModel? _owner;

    [ObservableProperty] private string id = "";
    [ObservableProperty] private string name = "";
    [ObservableProperty] private string typeId = "socks5h";
    [ObservableProperty] private string host = "";
    [ObservableProperty] private string portText = "1080";
    [ObservableProperty] private string username = "";
    [ObservableProperty] private string password = "";
    [ObservableProperty] private bool showPassword;
    [ObservableProperty] private bool isExpanded;
    [ObservableProperty] private bool isDraft;
    [ObservableProperty] private string statusText = "";
    [ObservableProperty] private bool isTesting;

    public bool IsActive
    {
        get => _owner != null && string.Equals(Id, _owner.ActiveId, StringComparison.Ordinal);
        set
        {
            if (value)
            {
                _owner?.SetActive(this);
            }
            else
            {
                OnPropertyChanged();
            }
        }
    }

    public string HeaderTitle =>
        string.IsNullOrWhiteSpace(Name)
            ? LocalizationService.Instance["Proxy"]
            : Name.Trim();

    public string EndpointLabel
    {
        get
        {
            var trimmedHost = Host.Trim();
            return trimmedHost.Length == 0 ? "" : $"{trimmedHost}:{PortText.Trim()}";
        }
    }

    public static ProxyProfileItem From(ProxyProfile profile, ProxySettingsViewModel owner)
    {
        var item = new ProxyProfileItem
        {
            Id = profile.Id,
            Name = profile.Name,
            TypeId = profile.Type,
            Host = profile.Host,
            PortText = profile.Port.ToString(),
            Username = profile.Username,
            Password = DpapiSecretStore.Unprotect(profile.PasswordProtected) ?? ""
        };
        item.Attach(owner);
        return item;
    }

    public void Attach(ProxySettingsViewModel owner) => _owner = owner;

    public void NotifyActive() => OnPropertyChanged(nameof(IsActive));

    public void NotifyHeader()
    {
        OnPropertyChanged(nameof(HeaderTitle));
        OnPropertyChanged(nameof(EndpointLabel));
    }

    public ProxyProfile ToProfile()
    {
        _ = int.TryParse(PortText.Trim(), out var port);
        return new ProxyProfile
        {
            Id = Id,
            Name = Name.Trim(),
            Type = TypeId,
            Host = Host.Trim(),
            Port = port is >= 1 and <= 65535 ? port : 1080,
            Username = Username.Trim(),
            PasswordProtected = DpapiSecretStore.Protect(string.IsNullOrEmpty(Password) ? null : Password)
        };
    }

    partial void OnNameChanged(string value) => OnPropertyChanged(nameof(HeaderTitle));

    partial void OnHostChanged(string value) => OnPropertyChanged(nameof(EndpointLabel));

    partial void OnPortTextChanged(string value) => OnPropertyChanged(nameof(EndpointLabel));

    partial void OnIsExpandedChanged(bool value)
    {
        if (value)
        {
            _owner?.CollapseOthers(this);
        }
    }
}
