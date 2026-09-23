using System.ComponentModel;
using System.Windows;
using YtPump.App.Loc;

namespace YtPump.App;

public partial class HelpWindow : Window
{
    public HelpWindow()
    {
        InitializeComponent();
        LocalizationService.Instance.PropertyChanged += OnLanguageChanged;
        Closed += (_, _) => LocalizationService.Instance.PropertyChanged -= OnLanguageChanged;
        RenderHelp();
    }

    private void OnLanguageChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(LocalizationService.Language))
        {
            RenderHelp();
        }
    }

    private void RenderHelp()
    {
        Viewer.Document = HelpMarkdown.ToDocument(HelpMarkdown.Load(LocalizationService.Instance.Language));
    }
}
