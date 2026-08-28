using Avalonia.Controls;
using SidebarDiagnostics.App.ViewModels;

namespace SidebarDiagnostics.App.Views;

public partial class SettingsWindow : Window
{
    public SettingsWindow()
    {
        InitializeComponent();
        DataContextChanged += OnDataContextChanged;
    }

    private void OnDataContextChanged(object? sender, EventArgs e)
    {
        if (DataContext is not SettingsViewModel viewModel)
        {
            return;
        }

        viewModel.Saved += OnCloseRequested;
        viewModel.Cancelled += OnCloseRequested;
    }

    private void OnCloseRequested(object? sender, EventArgs e) => Close(true);
}
