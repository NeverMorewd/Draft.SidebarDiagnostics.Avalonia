using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using SidebarDiagnostics.App.Services;
using SidebarDiagnostics.App.Services.Hardware;
using SidebarDiagnostics.App.Services.ExternalMetrics;
using SidebarDiagnostics.App.Services.Startup;
using SidebarDiagnostics.App.ViewModels;
using SidebarDiagnostics.App.Views;
using SidebarDiagnostics.App.Services.Shortcuts;

namespace SidebarDiagnostics.App;

public partial class App : Application
{
    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
    }

    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            desktop.ShutdownMode = ShutdownMode.OnExplicitShutdown;
            var metricsService = new SystemMetricsService();
            var settingsStore = new JsonSettingsStore();
            var hardwareSensorService = HardwareSensorServiceFactory.Create();
            var autoStartService = AutoStartServiceFactory.Create();
            var mainViewModel = new MainViewModel(
                metricsService,
                settingsStore,
                hardwareSensorService,
                autoStartService,
                new ExternalMetricService());
            var mainWindow = new MainWindow
            {
                DataContext = mainViewModel,
            };
            desktop.MainWindow = mainWindow;
            var applicationViewModel = new ApplicationViewModel(desktop, mainWindow, mainViewModel, new GlobalShortcutService());
            applicationViewModel.Initialize();
            DataContext = applicationViewModel;
        }

        base.OnFrameworkInitializationCompleted();
    }
}
