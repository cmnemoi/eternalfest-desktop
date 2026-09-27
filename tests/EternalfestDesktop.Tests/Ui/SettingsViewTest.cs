using EternalfestDesktop.Tests.Support;
using EternalfestDesktop.Ui.Resources;

namespace EternalfestDesktop.Tests.Ui;

public sealed class SettingsViewTest : IDisposable
{
    private readonly TestLauncher _launcher = new();

    public void Dispose() => _launcher.Dispose();

    /// @spec store::clears-cache
    [Fact]
    public Task Clearing_the_cache_leaves_no_contree_downloaded_back_in_the_library() => HeadlessApp.Run(async () =>
    {
        var contree = PublishedContree.Named("Dojo");
        _launcher.Eternalfest.Publishing(contree);
        await _launcher.Download(contree);
        var window = await OpenSettings();

        window.Click(Strings.ClearCache);

        await window.WaitFor(Strings.CacheCleared);
        window.Click(Strings.Back);
        await window.WaitUntil(() => !window.Main.Library.IsLoading && window.Cards.Count == 1, "the library to reload");
        Assert.DoesNotContain(Strings.Downloaded, window.TextsOnCard("Dojo"));
    });

    /// @spec store::clears-cache
    [Fact]
    public Task Refuses_to_clear_the_cache_while_a_game_runs() => HeadlessApp.Run(async () =>
    {
        var contree = PublishedContree.Named("Dojo");
        _launcher.Eternalfest.Publishing(contree);
        _launcher.FlashPlayer.KeepWindowOpen();
        var window = await OpenLibrary();
        window.Click("Dojo");
        await window.WaitFor(Strings.Play);
        window.Click(Strings.Play);
        await window.WaitUntil(() => _launcher.FlashPlayer.Played.Count == 1, "the game to be played");
        window.Click(Strings.Back);
        await window.WaitFor(Strings.Settings);
        window.Click(Strings.Settings);
        await window.WaitFor(Strings.ClearCache);

        window.Click(Strings.ClearCache);

        await window.WaitFor(Strings.ErrorAlreadyPlaying);
        Assert.NotNull(await _launcher.FindDownloaded(contree));
        _launcher.FlashPlayer.CloseWindow();
    });

    /// @spec ui::settings
    [Fact]
    public Task Choosing_another_language_asks_to_restart() => HeadlessApp.Run(async () =>
    {
        var window = await OpenSettings();
        Assert.False(window.Shows(Strings.RestartNeeded));

        window.Choose("Français");

        Assert.True(window.Shows(Strings.RestartNeeded));
        Assert.Equal("fr", _launcher.Preferences.Current.UiLanguage);
    });

    /// @spec ui::settings
    [Fact]
    public Task Picking_no_folder_keeps_the_cache_folder() => HeadlessApp.Run(async () =>
    {
        var window = await OpenSettings();

        window.Click(Strings.ChangeFolder);

        Assert.False(window.Shows(Strings.RestartNeeded));
        Assert.Null(_launcher.Preferences.Current.CacheFolder);
    });

    /// @spec ui::settings
    [Fact]
    public Task Back_returns_to_the_library() => HeadlessApp.Run(async () =>
    {
        _launcher.Eternalfest.Publishing(PublishedContree.Named("Dojo"));
        var window = await OpenSettings();

        window.Click(Strings.Back);

        await window.WaitUntil(() => window.Cards.Count == 1, "the library");
        Assert.False(window.Shows(Strings.ClearCache));
    });

    private async Task<LauncherWindow> OpenSettings()
    {
        var window = await OpenLibrary();
        window.Click(Strings.Settings);
        await window.WaitFor(Strings.ClearCache);
        return window;
    }

    private async Task<LauncherWindow> OpenLibrary()
    {
        var window = LauncherWindow.Opening(_launcher.MainWindow());
        await window.WaitUntil(() => !window.Main.Library.IsLoading && window.Buttons.Contains(Strings.Settings), "the library to load");
        return window;
    }
}
