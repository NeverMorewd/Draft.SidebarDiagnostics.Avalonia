using Avalonia.Controls;
using Avalonia.Interactivity;
using SidebarDiagnostics.App.Models;

namespace SidebarDiagnostics.App.Views;

public partial class AboutWindow : Window
{
    public AboutWindow()
    {
        InitializeComponent();
        DataContext = AboutInfo.Create();
    }

    private async void OpenRepository(object? sender, RoutedEventArgs e) =>
        await OpenUriAsync(AboutInfo.Repository);

    private async void OpenIssues(object? sender, RoutedEventArgs e) =>
        await OpenUriAsync(AboutInfo.Issues);

    private async Task OpenUriAsync(string address)
    {
        var launcher = GetTopLevel(this)?.Launcher;
        if (launcher is not null)
        {
            await launcher.LaunchUriAsync(new Uri(address));
        }
    }

    private void CloseWindow(object? sender, RoutedEventArgs e) => Close();
}
