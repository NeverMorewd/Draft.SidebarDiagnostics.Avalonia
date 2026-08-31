using System.ComponentModel;
using System.Runtime.InteropServices;

namespace SidebarDiagnostics.App.Services.Windowing;

public interface IClickThroughService
{
    bool IsSupported { get; }
    void Apply(nint windowHandle, bool enabled);
}

public static class ClickThroughService
{
    public static IClickThroughService Create() => OperatingSystem.IsWindows()
        ? new WindowsClickThroughService()
        : new UnsupportedClickThroughService();
}

internal sealed class UnsupportedClickThroughService : IClickThroughService
{
    public bool IsSupported => false;
    public void Apply(nint windowHandle, bool enabled) { }
}

internal sealed class WindowsClickThroughService : IClickThroughService
{
    private const int ExtendedStyleIndex = -20;
    private const long TransparentStyle = 0x00000020L;

    public bool IsSupported => true;

    public void Apply(nint windowHandle, bool enabled)
    {
        if (windowHandle == 0)
        {
            return;
        }

        Marshal.SetLastPInvokeError(0);
        var currentStyle = GetWindowLongPtr(windowHandle, ExtendedStyleIndex).ToInt64();
        if (currentStyle == 0 && Marshal.GetLastPInvokeError() != 0)
        {
            throw new Win32Exception(Marshal.GetLastPInvokeError());
        }

        var nextStyle = enabled
            ? currentStyle | TransparentStyle
            : currentStyle & ~TransparentStyle;
        if (nextStyle == currentStyle)
        {
            return;
        }

        Marshal.SetLastPInvokeError(0);
        var previousStyle = SetWindowLongPtr(windowHandle, ExtendedStyleIndex, new nint(nextStyle));
        if (previousStyle == 0 && Marshal.GetLastPInvokeError() != 0)
        {
            throw new Win32Exception(Marshal.GetLastPInvokeError());
        }
    }

    [DllImport("user32.dll", EntryPoint = "GetWindowLongPtrW", SetLastError = true)]
    private static extern nint GetWindowLongPtr(nint windowHandle, int index);

    [DllImport("user32.dll", EntryPoint = "SetWindowLongPtrW", SetLastError = true)]
    private static extern nint SetWindowLongPtr(nint windowHandle, int index, nint newValue);
}
