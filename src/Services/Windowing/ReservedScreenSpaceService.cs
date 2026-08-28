using System.Runtime.InteropServices;
using SidebarDiagnostics.App.Models;

namespace SidebarDiagnostics.App.Services.Windowing;

public interface IReservedScreenSpaceService : IDisposable
{
    bool IsSupported { get; }
    bool IsRegistered { get; }
    void Apply(nint windowHandle, DockEdge edge, double width);
    void Remove();
}

public static class ReservedScreenSpaceService
{
    public static IReservedScreenSpaceService Create() =>
        OperatingSystem.IsWindows()
            ? new WindowsAppBarService()
            : new UnsupportedReservedScreenSpaceService();
}

internal sealed class UnsupportedReservedScreenSpaceService : IReservedScreenSpaceService
{
    public bool IsSupported => false;
    public bool IsRegistered => false;
    public void Apply(nint windowHandle, DockEdge edge, double width) { }
    public void Remove() { }
    public void Dispose() { }
}

internal sealed class WindowsAppBarService : IReservedScreenSpaceService
{
    private const uint AbmNew = 0;
    private const uint AbmRemove = 1;
    private const uint AbmQueryPos = 2;
    private const uint AbmSetPos = 3;
    private const uint AbmWindowPosChanged = 9;
    private const uint AbnPosChanged = 1;
    private const uint WmWindowPosChanged = 0x0047;
    private const uint MonitorDefaultToNearest = 2;
    private const uint SwpNoActivate = 0x0010;
    private const uint SwpShowWindow = 0x0040;
    private static readonly nint HwndTopmost = new(-1);

    private nint _windowHandle;
    private uint _callbackMessage;
    private DockEdge _edge;
    private double _width;
    private bool _applying;
    private readonly SubclassProcedure _subclassProcedure;

    public WindowsAppBarService()
    {
        _subclassProcedure = WindowProcedure;
    }

    public bool IsSupported => true;
    public bool IsRegistered { get; private set; }

    public void Apply(nint windowHandle, DockEdge edge, double width)
    {
        if (_applying)
        {
            return;
        }

        if (windowHandle == 0 || edge == DockEdge.None)
        {
            Remove();
            return;
        }

        if (IsRegistered && _windowHandle != windowHandle)
        {
            Remove();
        }

        _windowHandle = windowHandle;
        _edge = edge;
        _width = width;
        if (!IsRegistered)
        {
            _callbackMessage = RegisterWindowMessage($"SidebarDiagnostics.AppBar.{Environment.ProcessId}");
            var registration = CreateData();
            SHAppBarMessage(AbmNew, ref registration);
            SetWindowSubclass(windowHandle, _subclassProcedure, 1, 0);
            IsRegistered = true;
        }

        _applying = true;
        try
        {
            ApplyPosition(windowHandle, edge, width);
        }
        finally
        {
            _applying = false;
        }
    }

    private void ApplyPosition(nint windowHandle, DockEdge edge, double width)
    {

        var monitor = MonitorFromWindow(windowHandle, MonitorDefaultToNearest);
        var monitorInfo = new MonitorInfo { Size = Marshal.SizeOf<MonitorInfo>() };
        if (monitor == 0 || !GetMonitorInfo(monitor, ref monitorInfo))
        {
            return;
        }

        var dpi = GetDpiForWindow(windowHandle);
        var physicalWidth = Math.Max(1, (int)Math.Round(width * (dpi == 0 ? 1 : dpi / 96d)));
        var data = CreateData();
        data.Edge = edge == DockEdge.Left ? 0u : 2u;
        data.Rectangle = monitorInfo.Monitor;
        if (edge == DockEdge.Left)
        {
            data.Rectangle.Right = data.Rectangle.Left + physicalWidth;
        }
        else
        {
            data.Rectangle.Left = data.Rectangle.Right - physicalWidth;
        }

        SHAppBarMessage(AbmQueryPos, ref data);
        if (edge == DockEdge.Left)
        {
            data.Rectangle.Right = data.Rectangle.Left + physicalWidth;
        }
        else
        {
            data.Rectangle.Left = data.Rectangle.Right - physicalWidth;
        }

        SHAppBarMessage(AbmSetPos, ref data);
        SetWindowPos(
            windowHandle,
            HwndTopmost,
            data.Rectangle.Left,
            data.Rectangle.Top,
            data.Rectangle.Width,
            data.Rectangle.Height,
            SwpNoActivate | SwpShowWindow);
    }

    public void Remove()
    {
        if (!IsRegistered)
        {
            return;
        }

        RemoveWindowSubclass(_windowHandle, _subclassProcedure, 1);
        var data = CreateData();
        SHAppBarMessage(AbmRemove, ref data);
        IsRegistered = false;
        _windowHandle = 0;
        _callbackMessage = 0;
    }

    public void Dispose() => Remove();

    private AppBarData CreateData() => new()
    {
        Size = (uint)Marshal.SizeOf<AppBarData>(),
        WindowHandle = _windowHandle,
        CallbackMessage = _callbackMessage
    };

    private nint WindowProcedure(
        nint windowHandle,
        uint message,
        nuint wordParameter,
        nint longParameter,
        nuint subclassId,
        nuint referenceData)
    {
        if (message == _callbackMessage && wordParameter == AbnPosChanged)
        {
            Apply(windowHandle, _edge, _width);
            return 0;
        }

        if (message == WmWindowPosChanged && IsRegistered && !_applying)
        {
            var data = CreateData();
            SHAppBarMessage(AbmWindowPosChanged, ref data);
        }

        return DefSubclassProc(windowHandle, message, wordParameter, longParameter);
    }

    private delegate nint SubclassProcedure(
        nint windowHandle,
        uint message,
        nuint wordParameter,
        nint longParameter,
        nuint subclassId,
        nuint referenceData);

    [StructLayout(LayoutKind.Sequential)]
    private struct AppBarData
    {
        public uint Size;
        public nint WindowHandle;
        public uint CallbackMessage;
        public uint Edge;
        public NativeRectangle Rectangle;
        public nint Parameter;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct NativeRectangle
    {
        public int Left;
        public int Top;
        public int Right;
        public int Bottom;
        public readonly int Width => Right - Left;
        public readonly int Height => Bottom - Top;
    }

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
    private struct MonitorInfo
    {
        public int Size;
        public NativeRectangle Monitor;
        public NativeRectangle WorkArea;
        public uint Flags;
    }

    [DllImport("shell32.dll", SetLastError = true)]
    private static extern nuint SHAppBarMessage(uint message, ref AppBarData data);

    [DllImport("user32.dll")]
    private static extern nint MonitorFromWindow(nint windowHandle, uint flags);

    [DllImport("user32.dll", CharSet = CharSet.Auto)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool GetMonitorInfo(nint monitor, ref MonitorInfo monitorInfo);

    [DllImport("user32.dll")]
    private static extern uint GetDpiForWindow(nint windowHandle);

    [DllImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool SetWindowPos(
        nint windowHandle,
        nint insertAfter,
        int x,
        int y,
        int width,
        int height,
        uint flags);

    [DllImport("user32.dll", CharSet = CharSet.Unicode)]
    private static extern uint RegisterWindowMessage(string message);

    [DllImport("comctl32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool SetWindowSubclass(
        nint windowHandle,
        SubclassProcedure callback,
        nuint subclassId,
        nuint referenceData);

    [DllImport("comctl32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool RemoveWindowSubclass(
        nint windowHandle,
        SubclassProcedure callback,
        nuint subclassId);

    [DllImport("comctl32.dll")]
    private static extern nint DefSubclassProc(
        nint windowHandle,
        uint message,
        nuint wordParameter,
        nint longParameter);
}
