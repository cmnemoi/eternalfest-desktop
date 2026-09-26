using System.Diagnostics;
using System.Globalization;

namespace EternalfestDesktop.Infrastructure.LocalServer;

/// <summary>
/// The Linux projector, with the GTK 2 and NSS it links to in <c>lib/</c>, and <c>libprojector-window.so</c>, which shapes
/// its window from inside its process (see native/projector-window).
/// </summary>
/// <param name="folder">Where the projector is.</param>
public sealed class LinuxFlashProjector(string folder) : FlashProjector
{
    /// <summary>Loaded in the projector both as a preloaded library and a GTK module.</summary>
    private const string WindowShapingLibrary = "libprojector-window.so";

    public ProcessStartInfo StartInfo(Uri loader, bool fullscreen, AvailableScreenArea? screen)
    {
        var start = new ProcessStartInfo(Path.Combine(folder, "flashplayer"));
        start.ArgumentList.Add(loader.AbsoluteUri);

        var libraries = Path.Combine(folder, "lib");
        var windowShaping = Path.Combine(libraries, WindowShapingLibrary);
        // Recent distributions no longer install GTK 2 and NSS: the projector's are bundled
        start.Environment.TryGetValue("LD_LIBRARY_PATH", out var systemLibraries);
        start.Environment["LD_LIBRARY_PATH"] = string.IsNullOrEmpty(systemLibraries) ? libraries : $"{libraries}{Path.PathSeparator}{systemLibraries}";
        start.Environment["LD_PRELOAD"] = windowShaping;
        start.Environment["GTK_MODULES"] = windowShaping;
        if (fullscreen)
            start.Environment["PROJECTOR_WINDOW_FULLSCREEN"] = "1";
        else if (screen is not null)
            // @spec play::fills-screen-height
            // X11 pixels, like the launcher's: both run under X11 or XWayland
            start.Environment["PROJECTOR_WINDOW_HEIGHT"] = screen.HeightUnderTitleBar.ToString(CultureInfo.InvariantCulture);
        return start;
    }

    public Task ShapeWindow(Process projector, bool fullscreen, AvailableScreenArea? screen, CancellationToken cancellationToken) =>
        Task.CompletedTask;
}
