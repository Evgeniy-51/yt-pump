using System.Windows;
using YtPump.Core.Settings;

namespace YtPump.App;

internal static class WindowLayout
{
    public const double MinWidth = 520;
    public const double MinHeight = 400;

    public static void Apply(Window window, WindowSettings settings)
    {
        var work = SystemParameters.WorkArea;
        var maxW = Math.Max(MinWidth, work.Width);
        var maxH = Math.Max(MinHeight, work.Height);
        var width = Math.Clamp(settings.Width, MinWidth, maxW);
        var height = Math.Clamp(settings.Height, MinHeight, maxH);

        window.Width = width;
        window.Height = height;

        if (settings.Left is double left && settings.Top is double top)
        {
            window.WindowStartupLocation = WindowStartupLocation.Manual;
            if (left + width > work.Right)
            {
                left = work.Right - width;
            }

            if (top + height > work.Bottom)
            {
                top = work.Bottom - height;
            }

            if (left < work.Left)
            {
                left = work.Left;
            }

            if (top < work.Top)
            {
                top = work.Top;
            }

            window.Left = left;
            window.Top = top;
        }

        window.WindowState = settings.Maximized ? WindowState.Maximized : WindowState.Normal;
    }

    public static WindowSettings Capture(Window window)
    {
        var bounds = window.WindowState == WindowState.Maximized
            ? window.RestoreBounds
            : new Rect(window.Left, window.Top, window.Width, window.Height);

        return new WindowSettings
        {
            Width = bounds.Width,
            Height = bounds.Height,
            Left = bounds.Left,
            Top = bounds.Top,
            Maximized = window.WindowState == WindowState.Maximized
        };
    }
}
