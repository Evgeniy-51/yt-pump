using System.Windows;
using YtPump.Core.Settings;
using YtPump.Core.Tools;
using YtPump.Core.YtDlp;

namespace YtPump.App;

public partial class ProxySettingsWindow : Window
{
    public ProxySettingsWindow(
        AppSettings appSettings,
        Action persist,
        ToolsStatus tools,
        YtDlpProcess ytDlp,
        string? cookiesFromBrowser = null)
    {
        DataContext = new ProxySettingsViewModel(appSettings, persist, tools, ytDlp, cookiesFromBrowser);
        InitializeComponent();
    }

    private void Close_Click(object sender, RoutedEventArgs e) => Close();
}
