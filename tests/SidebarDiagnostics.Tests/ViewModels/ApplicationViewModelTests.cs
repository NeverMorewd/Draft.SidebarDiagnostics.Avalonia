using Avalonia.Controls.ApplicationLifetimes;
using SidebarDiagnostics.App.ViewModels;
using Xunit;

namespace SidebarDiagnostics.Tests.ViewModels;

public sealed class ApplicationViewModelTests
{
    [Theory]
    [InlineData(ActivationKind.Reopen, true)]
    [InlineData(ActivationKind.Background, false)]
    [InlineData(ActivationKind.File, false)]
    [InlineData(ActivationKind.OpenUri, false)]
    public void RestoresWindowOnlyForReopenActivation(ActivationKind kind, bool expected)
    {
        Assert.Equal(expected, ApplicationViewModel.ShouldRestoreWindow(kind));
    }
}
