using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;

namespace YtPump.App;

public partial class ProxyProfileFields : UserControl
{
    private bool _sync;

    public ProxyProfileFields()
    {
        InitializeComponent();
    }

    private void OnPasswordRevealChanged(object sender, RoutedEventArgs e)
    {
        if (!IsLoaded || sender is not ToggleButton toggle)
        {
            return;
        }

        if (toggle.IsChecked == true)
        {
            ShowPlain(SecretBox.Password);
            return;
        }

        ShowSecret(SecretText.Text);
    }

    private void OnSecretTextChanged(object sender, TextChangedEventArgs e)
    {
        if (_sync || SecretText.Visibility != Visibility.Visible)
        {
            return;
        }

        var value = SecretText.Text;
        _sync = true;
        if (DataContext is ProxyProfileItem item && item.Password != value)
        {
            item.Password = value;
        }

        if (SecretBox.Password != value)
        {
            SecretBox.Password = value;
        }

        _sync = false;
    }

    private void ShowPlain(string value)
    {
        _sync = true;
        if (DataContext is ProxyProfileItem item && item.Password != value)
        {
            item.Password = value;
        }

        SecretText.Text = value;
        SecretText.Visibility = Visibility.Visible;
        SecretBox.Opacity = 0;
        SecretBox.IsHitTestVisible = false;
        _sync = false;
        SecretText.Focus();
        SecretText.CaretIndex = SecretText.Text.Length;
    }

    private void ShowSecret(string value, bool focus = true)
    {
        _sync = true;
        if (DataContext is ProxyProfileItem item && item.Password != value)
        {
            item.Password = value;
        }

        if (SecretBox.Password != value)
        {
            SecretBox.Password = value;
        }

        SecretText.Visibility = Visibility.Collapsed;
        SecretBox.Opacity = 1;
        SecretBox.IsHitTestVisible = true;
        _sync = false;
        if (focus)
        {
            SecretBox.Focus();
        }
    }
}
