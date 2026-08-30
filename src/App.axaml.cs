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
using SidebarDiagnostics.App.Styling;

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
            var themeService = new ApplicationThemeService(this);
            themeService.Apply(Models.ApplicationTheme.Sidebar);
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
            var mainWindow = new MainWindow(themeService)
            {
                DataContext = mainViewModel,
            };
            desktop.MainWindow = mainWindow;
            var applicationViewModel = new ApplicationViewModel(
                desktop,
                desktop as IActivatableLifetime,
                mainWindow,
                mainViewModel,
                new GlobalShortcutService(),
                themeService);
            applicationViewModel.Initialize();
            DataContext = applicationViewModel;
        }

        base.OnFrameworkInitializationCompleted();
    }
}
