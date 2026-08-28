using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Platform;
using Avalonia.Threading;
using SidebarDiagnostics.App.Models;
using SidebarDiagnostics.App.Services.Windowing;
using SidebarDiagnostics.App.ViewModels;

namespace SidebarDiagnostics.App.Views;

public partial class MainWindow : Window
{
    private bool _settingsInitialized;
    private MainViewModel? _viewModel;
    private readonly DispatcherTimer _placementTimer;
    private readonly double _preferredHeight;

    public MainWindow()
    {
        InitializeComponent();
        _preferredHeight = Height;
        _placementTimer = new DispatcherTimer
        {
            Interval = TimeSpan.FromMilliseconds(150)
        };
        _placementTimer.Tick += OnPlacementTimerTick;
        Opened += OnOpened;
        Closed += OnClosed;
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
        if (_viewModel is not null)
        {
            _viewModel.SettingsApplied -= OnSettingsApplied;
        }

        _viewModel = DataContext as MainViewModel;
        if (_viewModel is not null)
        {
            _viewModel.SettingsApplied += OnSettingsApplied;
        }
    }

    private void OnOpened(object? sender, EventArgs e)
    {
        Screens.Changed += OnScreensChanged;
        RefreshDisplays();
        SchedulePlacement();
    }

    private void OnClosed(object? sender, EventArgs e)
    {
        Screens.Changed -= OnScreensChanged;
        _placementTimer.Stop();
        if (_viewModel is not null)
        {
            _viewModel.SettingsApplied -= OnSettingsApplied;
            _viewModel.Dispose();
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
        SchedulePlacement();
        if (!_settingsInitialized && viewModel.Settings.StartMinimized && IsVisible)
        {
            Hide();
        }

        _settingsInitialized = true;
    }

    private void OnScreensChanged(object? sender, EventArgs e)
    {
        RefreshDisplays();
        SchedulePlacement();
    }

    private void RefreshDisplays()
    {
        if (_viewModel is null)
        {
            return;
        }

        var primary = Screens.Primary;
        var screens = Screens.All.ToArray();
        var displays = screens.Select((screen, index) =>
        {
            var area = screen.WorkingArea;
            var hasName = !string.IsNullOrWhiteSpace(screen.DisplayName);
            var name = hasName ? screen.DisplayName! : $"Display {index + 1}";
            var duplicateIndex = screens
                .Take(index)
                .Count(candidate => string.Equals(candidate.DisplayName, screen.DisplayName, StringComparison.Ordinal));
            var id = hasName
                ? $"display:{name}:{duplicateIndex}"
                : $"display:{screen.Bounds.X},{screen.Bounds.Y}:{screen.Bounds.Width}x{screen.Bounds.Height}";
            return new DisplayDescriptor(
                id,
                name,
                area.X,
                area.Y,
                area.Width,
                area.Height,
                screen.Scaling,
                ReferenceEquals(screen, primary));
        }).ToArray();
        _viewModel.UpdateDisplays(displays);
    }

    private void SchedulePlacement()
    {
        _placementTimer.Stop();
        _placementTimer.Start();
    }

    private void OnPlacementTimerTick(object? sender, EventArgs e)
    {
        _placementTimer.Stop();
        ApplyPlacement();
    }

    private void ApplyPlacement()
    {
        if (_viewModel is null)
        {
            return;
        }

        var placement = DisplayPlacementPolicy.Calculate(
            _viewModel.AvailableDisplays,
            _viewModel.Settings.DisplayId,
            _viewModel.Settings.DockEdge,
            _viewModel.Settings.VerticalPosition,
            Width,
            _preferredHeight);
        if (placement is null)
        {
            return;
        }

        Height = placement.Height;
        var position = new PixelPoint(placement.X, placement.Y);
        if (Position != position)
        {
            Position = position;
        }
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
