using EternalfestDesktop.Infrastructure.LocalServer;

namespace EternalfestDesktop.Tests.Infrastructure.LocalServer;

public sealed class MacFlashProjectorTest
{
    private static readonly string Folder = Path.Combine("Applications", "Eternalfest Desktop.app", "Contents", "Resources", "flash-player");
    private static readonly Uri Loader = new("http://127.0.0.1:50317/assets/loader.swf?game=0dc0d559");
    private readonly MacFlashProjector _projector = new(Folder);

    /// @spec play::launches-flash-projector
    [Fact]
    public void Runs_the_bundled_projector_on_the_loader_with_the_window_shaping_library()
    {
        var start = _projector.StartInfo(Loader, fullscreen: false, screen: null);

        Assert.Equal(Path.Combine(Folder, "Flash Player.app", "Contents", "MacOS", "Flash Player"), start.FileName);
        Assert.Equal([Loader.AbsoluteUri], start.ArgumentList);
        Assert.Equal(Path.Combine(Folder, "libprojector-window.dylib"), start.Environment["DYLD_INSERT_LIBRARIES"]);
    }

    /// @spec play::fills-screen-height
    [Theory]
    [InlineData(944, 2.0, "872")]
    [InlineData(944, 1.0, "872")]
    public void Fills_the_screen_height_under_the_title_bar_in_points(int availableHeightInPoints, double scaling, string gameHeight)
    {
        var screen = AvailableScreenArea.InPoints(availableHeightInPoints, scaling);

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
        var start = _projector.StartInfo(Loader, fullscreen: true, AvailableScreenArea.InPoints(944, scaling: 2.0));

        Assert.Equal("1", start.Environment["PROJECTOR_WINDOW_FULLSCREEN"]);
        Assert.False(start.Environment.ContainsKey("PROJECTOR_WINDOW_HEIGHT"));
    }
}
