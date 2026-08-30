using Avalonia.Controls;
using SidebarDiagnostics.App.ViewModels;

namespace SidebarDiagnostics.App.Views;

public partial class SettingsWindow : Window
{
    private SettingsViewModel? _viewModel;
    private bool _isCommitted;

    public SettingsWindow()
    {
        InitializeComponent();
        DataContextChanged += OnDataContextChanged;
        Closing += OnClosing;
    }

    private void OnDataContextChanged(object? sender, EventArgs e)
    {
        if (_viewModel is not null)
        {
            _viewModel.Saved -= OnSaved;
            _viewModel.Cancelled -= OnCancelled;
        }

        if (DataContext is not SettingsViewModel viewModel)
        {
            _viewModel = null;
            return;
        }

        _viewModel = viewModel;
        viewModel.Saved += OnSaved;
        viewModel.Cancelled += OnCancelled;
    }

    private void OnSaved(object? sender, EventArgs e)
    {
        _isCommitted = true;
        Close(true);
    }

    private void OnCancelled(object? sender, EventArgs e) => Close(false);

    private void OnClosing(object? sender, WindowClosingEventArgs e)
    {
        if (!_isCommitted)
        {
            _viewModel?.RevertThemePreview();
        }
    }
}
