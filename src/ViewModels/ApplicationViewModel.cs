using Avalonia.Controls.ApplicationLifetimes;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SidebarDiagnostics.App.Views;
using SidebarDiagnostics.App.Services.Shortcuts;
using SidebarDiagnostics.App.Styling;

namespace SidebarDiagnostics.App.ViewModels;

public sealed partial class ApplicationViewModel(
    IClassicDesktopStyleApplicationLifetime lifetime,
    MainWindow mainWindow,
    MainViewModel mainViewModel,
    GlobalShortcutService shortcuts,
    ApplicationThemeService themeService) : ObservableObject, IDisposable
{
    private bool _isDisposed;

    public void Initialize()
    {
        mainViewModel.SettingsApplied += OnSettingsApplied;
        shortcuts.StatusChanged += OnShortcutStatusChanged;
        lifetime.Exit += OnExit;
        themeService.Apply(mainViewModel.Settings.Theme);
        mainViewModel.RefreshThemeResources();
        ApplyShortcuts();
    }

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
    private async Task ShowSettingsAsync()
    {
        ShowWindow();
        await mainWindow.ShowSettingsAsync();
    }

    [RelayCommand]
    private async Task ShowAboutAsync()
    {
        ShowWindow();
        await mainWindow.ShowAboutAsync();
    }

    [RelayCommand]
    private void Exit()
    {
        mainWindow.AllowClose = true;
        lifetime.Shutdown();
    }

    private void ToggleWindow()
    {
        if (mainWindow.IsVisible) HideWindow(); else ShowWindow();
    }

    private void OnSettingsApplied(object? sender, EventArgs e)
    {
        themeService.Apply(mainViewModel.Settings.Theme);
        mainViewModel.RefreshThemeResources();
        ApplyShortcuts();
    }
    private void OnShortcutStatusChanged(object? sender, EventArgs e) => mainViewModel.UpdateShortcutStatus(shortcuts.Status);
    private void ApplyShortcuts()
    {
        var settings = mainViewModel.Settings;
        shortcuts.Apply([
            new("Show and focus", settings.ShowShortcut, ShowWindow),
            new("Hide", settings.HideShortcut, HideWindow),
            new("Toggle", settings.ToggleShortcut, ToggleWindow)]);
        mainViewModel.UpdateShortcutStatus(shortcuts.Status);
    }
    private void OnExit(object? sender, ControlledApplicationLifetimeExitEventArgs e) => Dispose();
    public void Dispose()
    {
        if (_isDisposed)
        {
            return;
        }

        _isDisposed = true;
        mainViewModel.SettingsApplied -= OnSettingsApplied;
        shortcuts.StatusChanged -= OnShortcutStatusChanged;
        lifetime.Exit -= OnExit;
        shortcuts.Dispose();
    }
}
