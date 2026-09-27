using System.Diagnostics;
using System.Globalization;

namespace EternalfestDesktop.Infrastructure.LocalServer;

/// <summary>
/// The Mac projector, an Intel app running under Rosetta 2, with <c>libprojector-window.dylib</c>, which shapes its window
/// from inside its process (see native/projector-window).
/// </summary>
/// <param name="folder">Where the projector's app bundle is.</param>
public sealed class MacFlashProjector(string folder) : FlashProjector
{
    /// <summary>Its entitlements let the projector load it from the environment.</summary>
    private const string WindowShapingLibrary = "libprojector-window.dylib";

    public ProcessStartInfo StartInfo(Uri loader, bool fullscreen, AvailableScreenArea? screen)
    {
        var start = new ProcessStartInfo(Path.Combine(folder, "Flash Player.app", "Contents", "MacOS", "Flash Player"));
        start.ArgumentList.Add(loader.AbsoluteUri);
        start.Environment["DYLD_INSERT_LIBRARIES"] = Path.Combine(folder, WindowShapingLibrary);
        if (fullscreen)
            start.Environment["PROJECTOR_WINDOW_FULLSCREEN"] = "1";
        else if (screen is not null)
            // @spec play::fills-screen-height
            // Points, like AppKit's windows
            start.Environment["PROJECTOR_WINDOW_HEIGHT"] = screen.HeightUnderTitleBarUnscaled.ToString(CultureInfo.InvariantCulture);
        return start;
    }

    public Task ShapeWindow(Process projector, bool fullscreen, AvailableScreenArea? screen, CancellationToken cancellationToken) =>
        Task.CompletedTask;
}
