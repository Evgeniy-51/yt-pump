using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Animation;
using System.Windows.Threading;
using YtPump.App.Loc;

namespace YtPump.App;

public partial class MainWindow : Window
{
    private bool _exitConfirmed;
    private bool _shutdownInProgress;

    public MainWindow()
    {
        InitializeComponent();
        Closing += OnClosing;
        if (DataContext is MainWindowViewModel vm)
        {
            vm.FileStemSelectRequested += OnFileStemSelectRequested;
            vm.StatusErrorPulsed += (_, _) => PulseStatus();
            vm.PropertyChanged += OnViewModelPropertyChanged;
        }

        if (Application.Current is { } app)
        {
            app.SessionEnding += OnSessionEnding;
        }
    }

    protected override void OnSourceInitialized(EventArgs e)
    {
        base.OnSourceInitialized(e);
        if (DataContext is MainWindowViewModel vm)
        {
            WindowLayout.Apply(this, vm.WindowSettings);
        }
    }

    private void OnViewModelPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(MainWindowViewModel.IsStatusError)
            && sender is MainWindowViewModel { IsStatusError: false })
        {
            StatusLine.BeginAnimation(OpacityProperty, null);
            StatusLine.Opacity = 1;
        }
    }

    private void PulseStatus()
    {
        var anim = new DoubleAnimation
        {
            From = 1,
            To = 0.15,
            Duration = TimeSpan.FromMilliseconds(140),
            AutoReverse = true,
            RepeatBehavior = new RepeatBehavior(2)
        };
        StatusLine.BeginAnimation(OpacityProperty, anim);
    }

    private void SubtitleLang_Checked(object sender, RoutedEventArgs e)
    {
        if (sender is RadioButton { DataContext: SubtitleLangItem item }
            && DataContext is MainWindowViewModel vm)
        {
            vm.SelectSubtitle(item.Code);
        }
    }

    private void UrlBox_LostFocus(object sender, RoutedEventArgs e)
    {
        ProbeFromUrlBox();
    }

    private void UrlBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (sender is ComboBox { IsDropDownOpen: true, SelectedItem: string })
        {
            ProbeFromUrlBox();
        }
    }

    private void ProbeFromUrlBox()
    {
        if (DataContext is MainWindowViewModel vm && vm.ProbeCommand.CanExecute(null))
        {
            vm.ProbeCommand.Execute(null);
        }
    }

    private void OnClosing(object? sender, System.ComponentModel.CancelEventArgs e)
    {
        if (DataContext is not MainWindowViewModel vm)
        {
            return;
        }

        if (_exitConfirmed)
        {
            Teardown(vm);
            return;
        }

        if (_shutdownInProgress)
        {
            e.Cancel = true;
            return;
        }

        if (vm.IsDownloading)
        {
            var confirm = MessageBox.Show(
                this,
                LocalizationService.Instance["CloseWhileDownloading"],
                LocalizationService.Instance["AppTitle"],
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning,
                MessageBoxResult.No);
            if (confirm != MessageBoxResult.Yes)
            {
                e.Cancel = true;
                return;
            }
        }

        if (vm.IsBusy)
        {
            e.Cancel = true;
            _shutdownInProgress = true;
            vm.CancelInFlight();
            _ = FinishCloseAsync(vm);
            return;
        }

        Teardown(vm);
    }

    private async Task FinishCloseAsync(MainWindowViewModel vm)
    {
        try
        {
            await vm.WaitUntilIdleAsync(TimeSpan.FromSeconds(5));
        }
        catch (TimeoutException)
        {
        }

        await Dispatcher.InvokeAsync(() =>
        {
            _exitConfirmed = true;
            Close();
        });
    }

    private void OnSessionEnding(object sender, SessionEndingCancelEventArgs e)
    {
        if (DataContext is MainWindowViewModel vm)
        {
            vm.CancelInFlight();
        }
    }

    private void Teardown(MainWindowViewModel vm)
    {
        vm.FileStemSelectRequested -= OnFileStemSelectRequested;
        if (Application.Current is { } app)
        {
            app.SessionEnding -= OnSessionEnding;
        }

        vm.PersistWindow(WindowLayout.Capture(this));
    }

    private void OnFileStemSelectRequested(object? sender, EventArgs e)
    {
        Dispatcher.BeginInvoke(() =>
        {
            if (!FileStemBox.IsEnabled)
            {
                return;
            }

            FileStemBox.Focus();
            FileStemBox.SelectAll();
        }, DispatcherPriority.Input);
    }
}
