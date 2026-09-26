using System.Diagnostics;
using EternalfestDesktop.Infrastructure.LocalServer;

namespace EternalfestDesktop.Tests.Infrastructure.LocalServer;

public sealed class WindowsFlashProjectorTest
{
    private static readonly string Folder = Path.Combine("C:", "Games", "EternalfestDesktop", "flash-player");
    private static readonly Uri Loader = new("http://127.0.0.1:50317/assets/loader.swf?game=0dc0d559");
    private static readonly StageSize LoaderStage = new(Width: 420, Height: 520);

    /// @spec play::launches-flash-projector
    [Fact]
    public void Runs_the_bundled_projector_on_the_loader()
    {
        var start = new WindowsFlashProjector(Folder, LoaderStage).StartInfo(Loader, fullscreen: false, screen: null);

        Assert.Equal(Path.Combine(Folder, "flashplayer.exe"), start.FileName);
        Assert.Equal([Loader.AbsoluteUri], start.ArgumentList);
        Assert.False(start.Environment.ContainsKey("LD_PRELOAD"));
    }

    /// @spec play::fills-screen-height
    [Theory]
    [InlineData(1040, 1.0)]
    [InlineData(1560, 1.5)]
    public void Fills_the_screen_height_under_the_title_bar_in_the_projectors_unscaled_pixels(int availableHeight, double scaling)
    {
        var screen = new AvailableScreenArea(HeightInPixels: availableHeight, Scaling: scaling);

        var content = WindowsFlashProjector.ContentSize(LoaderStage, screen);

        // The projector ignores the screen's scaling: Windows scales its 968 pixels up to the 1452 the screen leaves at 150 %
        Assert.Equal(new StageSize(Width: 782, Height: 968), content);
    }

    /// @spec play::tears-down-on-exit
    [Fact]
    public async Task Stops_shaping_when_the_projector_exits_without_a_window()
    {
        using var exited = Process.Start(new ProcessStartInfo("dotnet", "--version") { RedirectStandardOutput = true })!;
        await exited.WaitForExitAsync(TestContext.Current.CancellationToken);

        await new WindowsFlashProjector(Folder, LoaderStage)
            .ShapeWindow(exited, fullscreen: false, screen: null, TestContext.Current.CancellationToken)
            .WaitAsync(TimeSpan.FromSeconds(10), TestContext.Current.CancellationToken);
    }

    /// @spec play::fills-screen-height
    [Fact]
    public void Shows_the_stage_as_it_is_when_the_screen_is_unknown()
    {
        Assert.Equal(LoaderStage, WindowsFlashProjector.ContentSize(LoaderStage, screen: null));
    }
}
