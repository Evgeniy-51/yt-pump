using System.ComponentModel;

namespace YtPump.App;

public sealed class SubtitleLangItem : INotifyPropertyChanged
{
    public SubtitleLangItem(string code) => Code = code;

    public string Code { get; }

    public string Label => Code;

    public bool IsSelected { get; private set; }

    public event PropertyChangedEventHandler? PropertyChanged;

    public override string ToString() => Code;

    public void SetSelected(bool value)
    {
        if (IsSelected == value)
        {
            return;
        }

        IsSelected = value;
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(IsSelected)));
    }
}
