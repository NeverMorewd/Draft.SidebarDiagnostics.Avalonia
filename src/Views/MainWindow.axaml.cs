using Avalonia.Controls;
using Avalonia.Interactivity;
using SidebarDiagnostics.App.ViewModels;

namespace SidebarDiagnostics.App.Views;

public partial class MainWindow : Window
{
    private bool _settingsInitialized;

    public MainWindow()
    {
        InitializeComponent();
        Closed += (_, _) => (DataContext as IDisposable)?.Dispose();
        Closing += OnClosing;
        DataContextChanged += OnDataContextChanged;
    }

    public bool AllowClose { get; set; }

    private void OnClosing(object? sender, WindowClosingEventArgs e)
    {
        if (AllowClose)
        {
            return;
        }

        e.Cancel = true;
        Hide();
    }

    private void OnDataContextChanged(object? sender, EventArgs e)
    {
        if (DataContext is MainViewModel viewModel)
        {
            viewModel.SettingsApplied += OnSettingsApplied;
        }
    }

    private void OnSettingsApplied(object? sender, EventArgs e)
    {
        if (DataContext is not MainViewModel viewModel)
        {
            return;
        }

        Topmost = viewModel.Settings.AlwaysOnTop;
        Width = viewModel.Settings.SidebarWidth;
        Opacity = viewModel.Settings.BackgroundOpacity;
        if (!_settingsInitialized && viewModel.Settings.StartMinimized && IsVisible)
        {
            Hide();
        }

        _settingsInitialized = true;
    }

    private async void OpenSettings(object? sender, RoutedEventArgs e)
    {
        if (DataContext is not MainViewModel mainViewModel)
        {
            return;
        }

        var settingsWindow = new SettingsWindow
        {
            DataContext = new SettingsViewModel(mainViewModel)
        };

        var saved = await settingsWindow.ShowDialog<bool>(this);
        if (saved)
        {
            Topmost = mainViewModel.Settings.AlwaysOnTop;
        }
    }
}
