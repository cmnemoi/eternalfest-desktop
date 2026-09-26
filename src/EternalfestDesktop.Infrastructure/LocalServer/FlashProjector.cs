using System.Diagnostics;

namespace EternalfestDesktop.Infrastructure.LocalServer;

/// <summary>Adobe's Flash projector for one OS: how it starts, and how its window gets the shape the launcher wants.</summary>
public interface FlashProjector
{
    /// <param name="loader">The loader's URL, FlashVars included.</param>
    /// <param name="screen">Where the launcher is, when the game window should fill its height; <c>null</c> when unknown.</param>
    ProcessStartInfo StartInfo(Uri loader, bool fullscreen, AvailableScreenArea? screen);

    /// <summary>Gives the running projector's window its shape, when the OS doesn't let <see cref="StartInfo" /> do it.</summary>
    Task ShapeWindow(Process projector, bool fullscreen, AvailableScreenArea? screen, CancellationToken cancellationToken);
}
