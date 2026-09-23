using System.Windows;
using System.Windows.Controls;

namespace YtPump.App;

public static class PasswordBoxBinder
{
    public static readonly DependencyProperty PasswordProperty =
        DependencyProperty.RegisterAttached(
            "Password",
            typeof(string),
            typeof(PasswordBoxBinder),
            new FrameworkPropertyMetadata(string.Empty, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault, OnPasswordChanged));

    private static readonly DependencyProperty IsUpdatingProperty =
        DependencyProperty.RegisterAttached(
            "IsUpdating",
            typeof(bool),
            typeof(PasswordBoxBinder));

    public static string GetPassword(DependencyObject obj) => (string)obj.GetValue(PasswordProperty);

    public static void SetPassword(DependencyObject obj, string value) => obj.SetValue(PasswordProperty, value);

    private static void OnPasswordChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is not PasswordBox box)
        {
            return;
        }

        box.PasswordChanged -= HandlePasswordChanged;
        if (!(bool)box.GetValue(IsUpdatingProperty))
        {
            box.Password = e.NewValue as string ?? "";
        }

        box.PasswordChanged += HandlePasswordChanged;
    }

    private static void HandlePasswordChanged(object sender, RoutedEventArgs e)
    {
        if (sender is not PasswordBox box)
        {
            return;
        }

        box.SetValue(IsUpdatingProperty, true);
        SetPassword(box, box.Password);
        box.SetValue(IsUpdatingProperty, false);
    }
}
