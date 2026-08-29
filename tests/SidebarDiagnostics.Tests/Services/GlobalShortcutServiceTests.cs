using SharpHook.Data;
using SidebarDiagnostics.App.Services.Shortcuts;
using Xunit;

namespace SidebarDiagnostics.Tests.Services;

public sealed class GlobalShortcutServiceTests
{
    [Fact]
    public void ParserAcceptsPortableGesture()
    {
        Assert.True(ShortcutGesture.TryParse("Ctrl+Alt+F12", out var gesture, out var error));
        Assert.Null(error);
        Assert.Equal(KeyCode.VcF12, gesture!.Key);
        Assert.True(gesture.Ctrl);
        Assert.True(gesture.Alt);
    }

    [Fact]
    public void ParserRejectsUnmodifiedKey()
    {
        Assert.False(ShortcutGesture.TryParse("S", out _, out var error));
        Assert.Contains("modifier", error, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void DuplicateGestureFailsWithoutStartingHook()
    {
        using var service = new GlobalShortcutService();
        var result = service.Apply([
            new("Show", "Ctrl+Alt+S", () => { }),
            new("Hide", "Ctrl+Alt+S", () => { })]);

        Assert.False(result);
        Assert.Contains("same shortcut", service.Status, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void EmptyConfigurationDisablesHookIdempotently()
    {
        using var service = new GlobalShortcutService();
        Assert.True(service.Apply([]));
        Assert.True(service.Apply([]));
        Assert.Contains("disabled", service.Status, StringComparison.OrdinalIgnoreCase);
    }
}
