namespace EternalfestDesktop.Infrastructure.LocalServer;

/// <summary>The part of a screen windows may take, the taskbar excluded.</summary>
/// <param name="HeightInPixels">In physical pixels, like the OS measures it.</param>
/// <param name="Scaling">How many physical pixels a logical pixel takes on this screen.</param>
public sealed record AvailableScreenArea(int HeightInPixels, double Scaling)
{
    /// <summary>
    /// In logical pixels. The window manager draws the title bar above the window's content, and doesn't tell
    /// its height before the window exists: this one leaves room for the usual Windows, GNOME and KDE title bars.
    /// </summary>
    private const int TitleBarHeight = 40;

    /// <summary>
    /// In logical pixels, above and below the window: window managers don't place it flush against the screen's edges,
    /// and each places it a little differently.
    /// </summary>
    private const int ScreenEdgeMargin = 16;

    /// <summary>In physical pixels, the height left to a window's content under its title bar.</summary>
    public int HeightUnderTitleBar => HeightInPixels - (int)Math.Ceiling((TitleBarHeight + (2 * ScreenEdgeMargin)) * Scaling);

    /// <summary>
    /// In logical pixels, the height left to a window's content under its title bar: the pixels of an app that ignores the
    /// screen's scaling, which Windows scales up.
    /// </summary>
    public int HeightUnderTitleBarUnscaled => (int)Math.Floor(HeightUnderTitleBar / Scaling);
}
