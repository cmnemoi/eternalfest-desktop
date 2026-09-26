using Avalonia;
using EternalfestDesktop.Infrastructure.FileSystem;
using Velopack;

namespace EternalfestDesktop.Ui;

internal static class Program
{
    // Don't use Avalonia, third-party APIs or SynchronizationContext-reliant code before AppMain is called.
    [STAThread]
    public static void Main(string[] args)
    {
        // @spec packaging::windows-installer
        // First: the installer and the uninstaller start the app to let it set itself up, and it must exit right away
        VelopackApp.Build().Run();
        BuildAvaloniaApp().StartWithClassicDesktopLifetime(args);
    }

    // Also used by the visual designer.
    public static AppBuilder BuildAvaloniaApp() =>
        AppBuilder.Configure<App>()
            .UsePlatformDetect()
            // @spec packaging::linux-desktop-entry
            .With(new X11PlatformOptions { WmClass = XdgDesktopEntry.WindowClass })
#if DEBUG
            .WithDeveloperTools()
#endif
            .WithInterFont()
            .LogToTrace();
}
