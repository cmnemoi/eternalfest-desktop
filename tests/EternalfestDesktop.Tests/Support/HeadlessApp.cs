using Avalonia;
using Avalonia.Headless;
using EternalfestDesktop.Ui;

namespace EternalfestDesktop.Tests.Support;

/// <summary>The app's own styles and views, rendered by Skia without a screen, on one UI thread shared by every test.</summary>
public static class HeadlessApp
{
    private static readonly HeadlessUnitTestSession Session = HeadlessUnitTestSession.StartNew(typeof(HeadlessApp));

    public static AppBuilder BuildAvaloniaApp() =>
        AppBuilder.Configure<App>()
            .UseSkia()
            .WithInterFont()
            .UseHeadless(new AvaloniaHeadlessPlatformOptions { UseHeadlessDrawing = false });

    /// <summary>Runs <paramref name="test"/> on the app's UI thread, as the player's clicks would, until it ends.</summary>
    public static Task Run(Func<Task> test) =>
        // Returns a value so the session awaits the test: dispatching a bare Func<Task> would only await its start
        Session.Dispatch(async () =>
        {
            await test();
            return true;
        }, TestContext.Current.CancellationToken);
}
