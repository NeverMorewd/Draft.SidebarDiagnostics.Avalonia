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
            var mainWindow = new MainWindow
            {
                DataContext = new MainViewModel(
                    metricsService,
                    settingsStore,
                    hardwareSensorService,
                    autoStartService,
                    new ExternalMetricService()),
            };
            desktop.MainWindow = mainWindow;
            DataContext = new ApplicationViewModel(desktop, mainWindow);
        }

        base.OnFrameworkInitializationCompleted();
    }
}
