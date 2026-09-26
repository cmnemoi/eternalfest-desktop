using EternalfestDesktop.Infrastructure.LocalServer;

namespace EternalfestDesktop.Tests.Infrastructure.LocalServer;

public sealed class LinuxFlashProjectorTest
{
    private static readonly string Folder = Path.Combine("opt", "eternalfest-desktop", "flash-player");
    private static readonly Uri Loader = new("http://127.0.0.1:50317/assets/loader.swf?game=0dc0d559");
    private readonly LinuxFlashProjector _projector = new(Folder);

    /// @spec play::launches-flash-projector
    [Fact]
    public void Runs_the_bundled_projector_on_the_loader_with_its_libraries_and_the_window_shaping_library()
    {
        var start = _projector.StartInfo(Loader, fullscreen: false, screen: null);

        Assert.Equal(Path.Combine(Folder, "flashplayer"), start.FileName);
        Assert.Equal([Loader.AbsoluteUri], start.ArgumentList);
        Assert.Equal(Path.Combine(Folder, "lib", "libprojector-window.so"), start.Environment["LD_PRELOAD"]);
        Assert.Equal(Path.Combine(Folder, "lib", "libprojector-window.so"), start.Environment["GTK_MODULES"]);
    }

    /// @spec play::launches-flash-projector
    [Theory]
    [InlineData(null, "")]
    [InlineData("/opt/steam/lib", ":/opt/steam/lib")]
    public void Looks_for_libraries_in_its_own_before_those_the_system_points_to(string? systemLibraries, string after)
    {
        var launcherLibraries = Environment.GetEnvironmentVariable("LD_LIBRARY_PATH");
        Environment.SetEnvironmentVariable("LD_LIBRARY_PATH", systemLibraries);
        try
        {
            var start = _projector.StartInfo(Loader, fullscreen: false, screen: null);

            Assert.Equal(Path.Combine(Folder, "lib") + after.Replace(':', Path.PathSeparator), start.Environment["LD_LIBRARY_PATH"]);
        }
        finally
        {
            Environment.SetEnvironmentVariable("LD_LIBRARY_PATH", launcherLibraries);
        }
    }

    /// @spec play::fills-screen-height
    [Theory]
    [InlineData(1040, 1.0, "968")]
    [InlineData(1560, 1.5, "1452")]
    public void Fills_the_screen_height_under_the_title_bar(int availableHeight, double scaling, string gameHeight)
    {
        var screen = new AvailableScreenArea(HeightInPixels: availableHeight, Scaling: scaling);

        var start = _projector.StartInfo(Loader, fullscreen: false, screen);

        Assert.Equal(gameHeight, start.Environment["PROJECTOR_WINDOW_HEIGHT"]);
        Assert.False(start.Environment.ContainsKey("PROJECTOR_WINDOW_FULLSCREEN"));
    }

    /// @spec play::fills-screen-height
    [Fact]
    public void Lets_the_projector_size_the_window_when_the_screen_is_unknown()
    {
        var start = _projector.StartInfo(Loader, fullscreen: false, screen: null);

        Assert.False(start.Environment.ContainsKey("PROJECTOR_WINDOW_HEIGHT"));
    }

    /// @spec play::launches-flash-projector
    [Fact]
    public void Makes_the_window_fullscreen_when_chosen()
    {
        var start = _projector.StartInfo(Loader, fullscreen: true, new AvailableScreenArea(HeightInPixels: 1040, Scaling: 1.0));

        Assert.Equal("1", start.Environment["PROJECTOR_WINDOW_FULLSCREEN"]);
        Assert.False(start.Environment.ContainsKey("PROJECTOR_WINDOW_HEIGHT"));
    }
}
