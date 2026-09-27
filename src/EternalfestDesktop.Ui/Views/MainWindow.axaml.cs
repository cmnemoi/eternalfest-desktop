using Avalonia.Controls;
using Avalonia.Threading;
using EternalfestDesktop.Infrastructure.LocalServer;

namespace EternalfestDesktop.Ui.Views;

public sealed partial class MainWindow : Window
{
    public MainWindow() => InitializeComponent();

    /// <summary>The screen the launcher is on, where the game window opens. Can be asked from any thread.</summary>
    /// @spec play::fills-screen-height
    public AvailableScreenArea? ScreenAvailableToTheGame() =>
        Dispatcher.UIThread.Invoke(() => Screens.ScreenFromWindow(this) switch
        {
            null => null,
            // Avalonia measures macOS screens in points, with a scaling of 1: only the window knows the Retina scaling
            var screen when OperatingSystem.IsMacOS() => AvailableScreenArea.InPoints(screen.WorkingArea.Height, RenderScaling),
            var screen => new AvailableScreenArea(HeightInPixels: screen.WorkingArea.Height, Scaling: screen.Scaling),
        });
}
