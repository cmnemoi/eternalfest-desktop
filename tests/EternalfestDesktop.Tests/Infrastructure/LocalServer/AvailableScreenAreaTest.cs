using EternalfestDesktop.Infrastructure.LocalServer;

namespace EternalfestDesktop.Tests.Infrastructure.LocalServer;

public sealed class AvailableScreenAreaTest
{
    /// @spec play::fills-screen-height
    [Fact]
    public void Measures_a_retina_screen_measured_in_points_in_physical_pixels()
    {
        var screen = AvailableScreenArea.InPoints(heightInPoints: 944, scaling: 2.0);

        Assert.Equal(new AvailableScreenArea(HeightInPixels: 1888, Scaling: 2.0), screen);
        Assert.Equal(1744, screen.HeightUnderTitleBar);
    }
}
