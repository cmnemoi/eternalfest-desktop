using System.Diagnostics;

namespace EternalfestDesktop.Infrastructure.LocalServer;

/// <summary>The Windows projector, whose window is shaped from the launcher once it's open.</summary>
/// <param name="folder">Where the projector is.</param>
/// <param name="loaderStage">The loader's stage, which the projector sizes its window to.</param>
public sealed class WindowsFlashProjector(string folder, StageSize loaderStage) : FlashProjector
{
    public ProcessStartInfo StartInfo(Uri loader, bool fullscreen, AvailableScreenArea? screen)
    {
        var start = new ProcessStartInfo(Path.Combine(folder, "flashplayer.exe"));
        start.ArgumentList.Add(loader.AbsoluteUri);
        return start;
    }

    /// <summary>
    /// The projector sizes its window to the stage once the loader is loaded, soon after it opens: the shape is kept
    /// that long, then the player may resize the window.
    /// </summary>
    private static readonly TimeSpan ShapingTime = TimeSpan.FromSeconds(5);

    private static readonly TimeSpan ShapingInterval = TimeSpan.FromMilliseconds(100);

    /// <summary>The loader's stage in the bundled loader, which the projector sizes its window to.</summary>
    public static WindowsFlashProjector NextTo(string folder, BundledFlashFiles flash)
    {
        using var loader = flash.OpenLoader();
        return new WindowsFlashProjector(folder, StageSize.Of(loader));
    }

    /// @spec play::fills-screen-height
    public async Task ShapeWindow(Process projector, bool fullscreen, AvailableScreenArea? screen, CancellationToken cancellationToken)
    {
        if (!OperatingSystem.IsWindows())
            return;
        var window = await OpenedWindow(projector, cancellationToken);
        if (window is null)
            return;
        var content = ContentSize(loaderStage, screen);
        using var shaping = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        shaping.CancelAfter(ShapingTime);
        while (!shaping.IsCancellationRequested && !projector.HasExited)
        {
            window.RemoveMenu();
            if (fullscreen)
                window.FillScreen();
            else
                window.FitContent(content);
            await Task.Delay(ShapingInterval, CancellationToken.None);
        }
    }

    private static async Task<Win32ProjectorWindow?> OpenedWindow(Process projector, CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested && !projector.HasExited)
        {
            projector.Refresh();
            if (projector.MainWindowHandle != 0)
                return new Win32ProjectorWindow(projector.MainWindowHandle);
            await Task.Delay(ShapingInterval, CancellationToken.None);
        }
        return null;
    }

    /// <summary>The game's size in the window, in the projector's pixels.</summary>
    /// @spec play::fills-screen-height
    public static StageSize ContentSize(StageSize stage, AvailableScreenArea? screen) =>
        screen is null ? stage : stage.ScaledToHeight(screen.HeightUnderTitleBarUnscaled);
}
