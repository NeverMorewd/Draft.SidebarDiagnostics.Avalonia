using Avalonia.Controls.ApplicationLifetimes;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SidebarDiagnostics.App.Views;

namespace SidebarDiagnostics.App.ViewModels;

public sealed partial class ApplicationViewModel(
    IClassicDesktopStyleApplicationLifetime lifetime,
    MainWindow mainWindow) : ObservableObject
{
    [RelayCommand]
    private void ShowWindow()
    {
        mainWindow.ShowSidebar();
        mainWindow.WindowState = Avalonia.Controls.WindowState.Normal;
        mainWindow.Activate();
    }

    [RelayCommand]
    private void HideWindow() => mainWindow.HideSidebar();

    [RelayCommand]
    private void Exit()
    {
        mainWindow.AllowClose = true;
        lifetime.Shutdown();
    }
}
