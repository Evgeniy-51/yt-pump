using System.ComponentModel;
using System.Globalization;
using System.Resources;
using System.Runtime.CompilerServices;

namespace YtPump.App.Loc;

public sealed class LocalizationService : INotifyPropertyChanged
{
    private static readonly ResourceManager ResourceManager =
        new("YtPump.App.Loc.Strings", typeof(LocalizationService).Assembly);

    public static LocalizationService Instance { get; } = new();

    private CultureInfo _culture = new("ru");

    public event PropertyChangedEventHandler? PropertyChanged;

    public string Language { get; private set; } = "ru";

    public string this[string key] =>
        ResourceManager.GetString(key, _culture) ?? key;

    public void SetLanguage(string language)
    {
        var normalized = string.Equals(language, "en", StringComparison.OrdinalIgnoreCase)
            ? "en"
            : "ru";

        if (Language == normalized)
        {
            return;
        }

        Language = normalized;
        _culture = new CultureInfo(normalized);
        CultureInfo.CurrentUICulture = _culture;
        CultureInfo.CurrentCulture = _culture;
        RaiseAll();
    }

    public string Format(string key, params object[] args) =>
        string.Format(_culture, this[key], args);

    private void RaiseAll()
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(string.Empty));
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("Item[]"));
        OnPropertyChanged(nameof(Language));
    }

    private void OnPropertyChanged([CallerMemberName] string? name = null) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
}
