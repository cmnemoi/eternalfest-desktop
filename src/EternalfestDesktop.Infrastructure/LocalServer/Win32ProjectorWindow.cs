using System.Runtime.InteropServices;

namespace EternalfestDesktop.Infrastructure.LocalServer;

/// <summary>
/// The Windows projector's window, seen from the launcher. The projector ignores the screen's scaling, so every call is made
/// in its unscaled pixels.
/// </summary>
internal sealed partial class Win32ProjectorWindow(nint handle)
{
    private const int StyleIndex = -16;
    private const long CaptionStyle = 0x00C00000;
    private const long ResizingFrameStyle = 0x00040000;
    private const uint KeepZOrder = 0x0004;
    private const uint KeepActivation = 0x0010;
    private const uint FrameChanged = 0x0020;
    private const uint NearestMonitor = 0x00000002;
    private static readonly nint UnscaledDpiAwareness = -1;

    /// <summary>The projector's menu bar would take room from the game.</summary>
    public void RemoveMenu() => Unscaled(() =>
    {
        if (GetMenu(handle) != 0)
            SetMenu(handle, 0);
    });

    /// <summary>Sizes the window so that its content is <paramref name="content" />, within the work area of its screen.</summary>
    public void FitContent(StageSize content) => Unscaled(() =>
    {
        if (!GetClientRect(handle, out var client) || !GetWindowRect(handle, out var window))
            return;
        if (client.Width == content.Width && client.Height == content.Height)
            return;
        var width = content.Width + window.Width - client.Width;
        var height = content.Height + window.Height - client.Height;
        var workArea = MonitorOf().Work;
        var left = workArea.Left + ((workArea.Width - width) / 2);
        var top = Math.Max(workArea.Top, Math.Min(window.Top, workArea.Bottom - height));
        SetWindowPos(handle, 0, left, top, width, height, KeepZOrder | KeepActivation);
    });

    /// <summary>Removes the title bar and borders, and covers the whole screen, taskbar included.</summary>
    public void FillScreen() => Unscaled(() =>
    {
        var style = GetWindowLongPtrW(handle, StyleIndex);
        var borderless = style & ~(CaptionStyle | ResizingFrameStyle);
        if (borderless != style)
            SetWindowLongPtrW(handle, StyleIndex, (nint)borderless);
        var screen = MonitorOf().Screen;
        if (GetWindowRect(handle, out var window) && window == screen && borderless == style)
            return;
        SetWindowPos(handle, 0, screen.Left, screen.Top, screen.Width, screen.Height, KeepZOrder | KeepActivation | FrameChanged);
    });

    private MonitorInformation MonitorOf()
    {
        var monitor = new MonitorInformation { Size = Marshal.SizeOf<MonitorInformation>() };
        GetMonitorInfoW(MonitorFromWindow(handle, NearestMonitor), ref monitor);
        return monitor;
    }

    private static void Unscaled(Action calls)
    {
        var launcherAwareness = SetThreadDpiAwarenessContext(UnscaledDpiAwareness);
        try
        {
            calls();
        }
        finally
        {
            SetThreadDpiAwarenessContext(launcherAwareness);
        }
    }

    [StructLayout(LayoutKind.Sequential)]
    private record struct Rectangle(int Left, int Top, int Right, int Bottom)
    {
        public readonly int Width => Right - Left;
        public readonly int Height => Bottom - Top;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct MonitorInformation
    {
        public int Size;
        public Rectangle Screen;
        public Rectangle Work;
        public int Flags;
    }

    [LibraryImport("user32.dll")]
    private static partial nint GetMenu(nint window);

    [LibraryImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static partial bool SetMenu(nint window, nint menu);

    [LibraryImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static partial bool GetClientRect(nint window, out Rectangle rectangle);

    [LibraryImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static partial bool GetWindowRect(nint window, out Rectangle rectangle);

    [LibraryImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static partial bool SetWindowPos(nint window, nint insertAfter, int left, int top, int width, int height, uint flags);

    [LibraryImport("user32.dll")]
    private static partial long GetWindowLongPtrW(nint window, int index);

    [LibraryImport("user32.dll")]
    private static partial nint SetWindowLongPtrW(nint window, int index, nint value);

    [LibraryImport("user32.dll")]
    private static partial nint MonitorFromWindow(nint window, uint flags);

    [LibraryImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static partial bool GetMonitorInfoW(nint monitor, ref MonitorInformation information);

    [LibraryImport("user32.dll")]
    private static partial nint SetThreadDpiAwarenessContext(nint context);
}
