using EternalfestDesktop.Tests.Support;
using EternalfestDesktop.Ui.Resources;

namespace EternalfestDesktop.Tests.Ui;

public sealed class SettingsViewModelTest : IDisposable
{
    private readonly TestLauncher _launcher = new();

    public void Dispose() => _launcher.Dispose();

    /// @spec ui::settings
    [Fact]
    public void Follows_the_system_language_by_default()
    {
        var settings = _launcher.Settings(() => Task.CompletedTask);

        Assert.Null(settings.SelectedLanguage.Code);
        Assert.False(settings.IsRestartNeeded);
    }

    /// @spec ui::settings
    [Fact]
    public void Keeps_the_chosen_language_for_the_next_start()
    {
        var settings = _launcher.Settings(() => Task.CompletedTask);

        settings.SelectedLanguage = settings.Languages.Single(language => language.Code == "fr");
        _launcher.Restart();

        Assert.True(settings.IsRestartNeeded);
        Assert.Equal("fr", _launcher.Preferences.Current.UiLanguage);
    }

    /// @spec ui::settings
    [Fact]
    public void Keeps_the_chosen_cache_folder_for_the_next_start()
    {
        var settings = _launcher.Settings(() => Task.CompletedTask);

        settings.ChangeCacheFolder("/games/eternalfest");
        _launcher.Restart();

        Assert.True(settings.IsRestartNeeded);
        Assert.Equal("/games/eternalfest", _launcher.Preferences.Current.CacheFolder);
    }

    /// @spec store::clears-cache
    [Fact]
    public async Task Clears_the_downloaded_contrees()
    {
        var contree = PublishedContree.Named("Dojo");
        _launcher.Eternalfest.Publishing(contree);
        await _launcher.Download(contree);
        var settings = _launcher.Settings(() => Task.CompletedTask);

        await settings.ClearCacheCommand.ExecuteAsync(null);

        Assert.Equal(Strings.CacheCleared, settings.Message);
        Assert.Null(await _launcher.FindDownloaded(contree));
    }

    /// @spec store::clears-cache
    [Fact]
    public async Task Refuses_to_clear_the_cache_while_a_game_runs()
    {
        var contree = PublishedContree.Named("Dojo");
        _launcher.Eternalfest.Publishing(contree);
        _launcher.FlashPlayer.KeepWindowOpen();
        var playing = _launcher.Play(contree);
        using (var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(5)))
            while (_launcher.FlashPlayer.Played.Count == 0)
                await Task.Delay(10, timeout.Token);
        var settings = _launcher.Settings(() => Task.CompletedTask);

        await settings.ClearCacheCommand.ExecuteAsync(null);

        Assert.Equal(Strings.ErrorAlreadyPlaying, settings.Message);
        Assert.NotNull(await _launcher.FindDownloaded(contree));
        _launcher.FlashPlayer.CloseWindow();
        await playing;
    }
}
