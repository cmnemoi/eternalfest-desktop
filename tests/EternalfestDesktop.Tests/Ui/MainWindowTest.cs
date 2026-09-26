using EternalfestDesktop.Infrastructure.LocalServer;
using EternalfestDesktop.Tests.Support;
using EternalfestDesktop.Ui.Views;

namespace EternalfestDesktop.Tests.Ui;

public sealed class MainWindowTest
{
    /// @spec play::fills-screen-height
    [Fact]
    public Task Gives_the_game_the_screen_the_launcher_is_on() => HeadlessApp.Run(async () =>
    {
        var window = new MainWindow();
        window.Show();
        var screen = window.Screens.ScreenFromWindow(window)!;

        // Asked while a game starts, away from the UI thread
        var available = await Task.Run(window.ScreenAvailableToTheGame, TestContext.Current.CancellationToken);

        Assert.Equal(new AvailableScreenArea(HeightInPixels: screen.WorkingArea.Height, Scaling: screen.Scaling), available);
        window.Close();
    });
}
