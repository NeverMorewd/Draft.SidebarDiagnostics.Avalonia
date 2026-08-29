using Avalonia.Threading;
using SharpHook;
using SharpHook.Data;

namespace SidebarDiagnostics.App.Services.Shortcuts;

public sealed record ShortcutBinding(string Name, string? Gesture, Action Action);

public sealed class GlobalShortcutService : IDisposable
{
    private readonly SimpleGlobalHook _hook = new();
    private IReadOnlyList<(ShortcutGesture Gesture, Action Action)> _bindings = [];
    private Task? _runTask;

    public string Status { get; private set; } = "Global shortcuts are not configured.";
    public event EventHandler? StatusChanged;

    public GlobalShortcutService()
    {
        _hook.KeyPressed += OnKeyPressed;
    }

    public bool Apply(IEnumerable<ShortcutBinding> bindings)
    {
        var parsed = new List<(ShortcutGesture Gesture, Action Action)>();
        var errors = new List<string>();
        foreach (var binding in bindings)
        {
            if (!ShortcutGesture.TryParse(binding.Gesture, out var gesture, out var error))
            {
                errors.Add($"{binding.Name}: {error}");
            }
            else if (gesture is not null)
            {
                parsed.Add((gesture, binding.Action));
            }
        }
        if (parsed.GroupBy(item => item.Gesture).Any(group => group.Count() > 1)) errors.Add("Two actions cannot use the same shortcut.");
        if (errors.Count > 0)
        {
            SetStatus(string.Join(" ", errors));
            return false;
        }

        _bindings = parsed;
        if (parsed.Count == 0)
        {
            Stop();
            SetStatus("Global shortcuts are disabled.");
            return true;
        }
        if (_runTask is null)
        {
            try
            {
                _runTask = _hook.RunAsync(GlobalHookType.Keyboard, useBackgroundThread: true);
                _ = ObserveAsync(_runTask);
            }
            catch (Exception exception)
            {
                SetStatus(CapabilityFailure(exception));
                return false;
            }
        }
        SetStatus(PlatformStatus());
        return true;
    }

    private void OnKeyPressed(object? sender, KeyboardHookEventArgs e)
    {
        var action = _bindings.FirstOrDefault(binding => binding.Gesture.Matches(e.Data, e.RawEvent.Mask)).Action;
        if (action is not null) Dispatcher.UIThread.Post(action);
    }

    private async Task ObserveAsync(Task task)
    {
        try { await task; }
        catch (Exception exception) { SetStatus(CapabilityFailure(exception)); }
        finally { _runTask = null; }
    }

    private static string PlatformStatus()
    {
        if (OperatingSystem.IsMacOS()) return "Active. macOS Accessibility permission is required.";
        if (OperatingSystem.IsLinux() && !string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("WAYLAND_DISPLAY"))) return "Active through the Linux input backend. Wayland may require elevated input permissions.";
        if (OperatingSystem.IsLinux()) return "Active in the X11 session.";
        return "Active.";
    }

    private static string CapabilityFailure(Exception exception) => OperatingSystem.IsMacOS()
        ? $"Unavailable: grant Accessibility permission. {exception.Message}"
        : OperatingSystem.IsLinux() && !string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("WAYLAND_DISPLAY"))
            ? $"Unavailable in this Wayland session: global input permission was denied. {exception.Message}"
            : $"Global shortcut registration failed: {exception.Message}";

    private void SetStatus(string value) { Status = value; StatusChanged?.Invoke(this, EventArgs.Empty); }
    private void Stop() { if (_runTask is null) return; _hook.Stop(); _runTask = null; }
    public void Dispose() { _hook.KeyPressed -= OnKeyPressed; _hook.Dispose(); }
}
