using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Platform;
using Avalonia.Threading;
using SidebarDiagnostics.App.Models;
using SidebarDiagnostics.App.Services.Windowing;
using SidebarDiagnostics.App.ViewModels;
using SidebarDiagnostics.App.Styling;

namespace SidebarDiagnostics.App.Views;

public partial class MainWindow : Window
{
    private bool _settingsInitialized;
    private bool _screensSubscribed;
    private MainViewModel? _viewModel;
    private readonly DispatcherTimer _placementTimer;
    private readonly double _preferredHeight;
    private readonly IReservedScreenSpaceService _reservedScreenSpace = ReservedScreenSpaceService.Create();
    private readonly IClickThroughService _clickThroughService = ClickThroughService.Create();
    private readonly Dictionary<string, MetricChartWindow> _metricCharts = new(StringComparer.Ordinal);
    private SettingsWindow? _settingsWindow;
    private AboutWindow? _aboutWindow;
    private readonly ApplicationThemeService _themeService;

    public MainWindow()
        : this(new ApplicationThemeService(Application.Current
            ?? throw new InvalidOperationException("The application is not initialized.")))
    {
    }

    public MainWindow(ApplicationThemeService themeService)
    {
        _themeService = themeService;
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
        Dispatcher.UIThread.Post(HideSidebar, DispatcherPriority.Background);
    }

    private void OnWindowPointerEntered(object? sender, PointerEventArgs e)
    {
        SetChromeVisibility(true);
    }

    private void OnWindowPointerExited(object? sender, PointerEventArgs e)
    {
        SetChromeVisibility(false);
    }

    private void SetChromeVisibility(bool isVisible)
    {
        HeaderChrome.IsVisible = isVisible;
        FooterChrome.IsVisible = isVisible;
        MainScroll.Classes.Set("chrome-visible", isVisible);
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
        if (!_screensSubscribed)
        {
            Screens.Changed += OnScreensChanged;
            _screensSubscribed = true;
        }

        RefreshDisplays();
        ApplyClickThrough();
        SchedulePlacement();
    }

    private void OnClosed(object? sender, EventArgs e)
    {
        if (_screensSubscribed)
        {
            Screens.Changed -= OnScreensChanged;
            _screensSubscribed = false;
        }

        _placementTimer.Stop();
        _reservedScreenSpace.Dispose();
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
        ApplyClickThrough();
        SchedulePlacement();
        if (!_settingsInitialized && viewModel.Settings.StartMinimized && IsVisible)
        {
            HideSidebar();
        }

        _settingsInitialized = true;
    }

    private void ApplyClickThrough()
    {
        if (_viewModel is null || !_clickThroughService.IsSupported)
        {
            return;
        }

        _clickThroughService.Apply(TryGetPlatformHandle()?.Handle ?? 0, _viewModel.Settings.ClickThrough);
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
        if (!IsVisible)
        {
            return;
        }

        _placementTimer.Start();
    }

    private void OnPlacementTimerTick(object? sender, EventArgs e)
    {
        _placementTimer.Stop();
        ApplyPlacement();
    }

    private void ApplyPlacement()
    {
        if (_viewModel is null || !IsVisible)
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

        var reserveSpace = _reservedScreenSpace.IsSupported
            && _viewModel.Settings.ReserveScreenSpace
            && _viewModel.Settings.DockEdge != DockEdge.None;

        if (reserveSpace && !_reservedScreenSpace.IsRegistered)
        {
            Position = new PixelPoint(placement.X, placement.Y);
        }

        if (reserveSpace)
        {
            var handle = TryGetPlatformHandle()?.Handle ?? 0;
            _reservedScreenSpace.Apply(handle, _viewModel.Settings.DockEdge, Width);
            return;
        }

        _reservedScreenSpace.Remove();

        Height = placement.Height;
        var position = new PixelPoint(placement.X, placement.Y);
        if (Position != position)
        {
            Position = position;
        }
    }

    public void ShowSidebar()
    {
        if (!IsVisible)
        {
            Show();
        }

        if (WindowState == WindowState.Minimized)
        {
            WindowState = WindowState.Normal;
        }

        SchedulePlacement();
    }

    public void HideSidebar()
    {
        _placementTimer.Stop();
        _reservedScreenSpace.Remove();
        Hide();
    }

    private void BeginWindowDrag(object? sender, PointerPressedEventArgs e)
    {
        if (e.GetCurrentPoint(this).Properties.IsLeftButtonPressed)
        {
            BeginMoveDrag(e);
        }
    }

    private void HideToTray(object? sender, RoutedEventArgs e)
    {
        HideSidebar();
    }

    private void OpenMetricChart(object? sender, RoutedEventArgs e)
    {
        if (sender is not Control { DataContext: DiagnosticMetric metric }
            || metric.SeriesId is null
            || _viewModel?.MetricSeries.Get(metric.SeriesId) is not { } series)
        {
            return;
        }

        if (_metricCharts.TryGetValue(metric.SeriesId, out var existing))
        {
            existing.Show();
            existing.Activate();
            return;
        }

        var window = new MetricChartWindow(series);
        _metricCharts.Add(metric.SeriesId, window);
        window.Closed += (_, _) => _metricCharts.Remove(metric.SeriesId);
        window.Show();
        window.Activate();
    }

    public async Task ShowSettingsAsync()
    {
        if (_settingsWindow is not null)
        {
            _settingsWindow.Activate();
            return;
        }

        if (DataContext is not MainViewModel mainViewModel)
        {
            return;
        }

        _settingsWindow = new SettingsWindow
        {
            DataContext = new SettingsViewModel(mainViewModel, _themeService)
        };

        try
        {
            var saved = await _settingsWindow.ShowDialog<bool>(this);
            if (saved)
            {
                Topmost = mainViewModel.Settings.AlwaysOnTop;
            }
        }
        finally
        {
            _settingsWindow = null;
        }
    }

    public async Task ShowAboutAsync()
    {
        if (_aboutWindow is not null)
        {
            _aboutWindow.Activate();
            return;
        }

        _aboutWindow = new AboutWindow();
        try
        {
            await _aboutWindow.ShowDialog(this);
        }
        finally
        {
            _aboutWindow = null;
        }
    }

    private async void OpenSettings(object? sender, RoutedEventArgs e) => await ShowSettingsAsync();

    private async void OpenAbout(object? sender, RoutedEventArgs e) => await ShowAboutAsync();
}
