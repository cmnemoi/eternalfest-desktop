using EternalfestDesktop.Application;
using EternalfestDesktop.Domain;
using EternalfestDesktop.Tests.Support;

namespace EternalfestDesktop.Tests.Application;

public sealed class PlayGameTest : IDisposable
{
    private readonly TestLauncher _launcher = new();

    public void Dispose() => _launcher.Dispose();

    /// @spec play::launches-ruffle
    /// @spec play::creates-local-run
    [Fact]
    public async Task Plays_a_contree_with_its_default_choices()
    {
        var contree = Published("Accumulation");

        await _launcher.Play(contree);

        var played = Assert.Single(_launcher.FlashPlayer.Played);
        Assert.Equal("127.0.0.1", played.Origin.Host);
        Assert.Equal(contree.Id, played.Game.Id);
        Assert.Equal(contree.Id, played.Run.GameId);
        Assert.Equal("main", played.Run.ChannelKey);
        Assert.Equal("solo", played.Run.Mode);
        Assert.Empty(played.Run.Options);
        Assert.Equal(new RunSettings("fr-FR", Volume: 100), played.Run.Settings);
        Assert.False(played.Fullscreen);
    }

    /// @spec play::valid-mode-and-options
    /// @spec play::creates-local-run
    [Fact]
    public async Task Plays_the_chosen_mode_options_and_settings()
    {
        var contree = Published("Accumulation");

        await _launcher.Play(contree, new RunChoices("multicoop", ["lifesharing"], Locale: "en-US", Volume: 40, Fullscreen: true));

        var played = Assert.Single(_launcher.FlashPlayer.Played);
        Assert.Equal("multicoop", played.Run.Mode);
        Assert.Equal(["lifesharing"], played.Run.Options);
        Assert.Equal(new RunSettings("en-US", Volume: 40), played.Run.Settings);
        Assert.True(played.Fullscreen);
    }

    /// @spec play::requires-downloaded-contree
    [Fact]
    public async Task Downloads_the_contree_before_playing_it()
    {
        var contree = Published("Accumulation");

        await _launcher.Play(contree);

        Assert.NotNull(await _launcher.FindDownloaded(contree));
    }

    /// @spec play::requires-downloaded-contree
    [Fact]
    public async Task Cant_play_a_contree_never_downloaded_without_network()
    {
        var contree = Published("Accumulation");
        _launcher.Eternalfest.IsUnreachable = true;

        await Assert.ThrowsAsync<EternalfestUnreachableException>(() => _launcher.Play(contree));

        Assert.Empty(_launcher.FlashPlayer.Played);
    }

    /// @spec play::requires-downloaded-contree
    [Fact]
    public async Task Plays_a_downloaded_contree_without_network()
    {
        var contree = Published("Accumulation");
        await _launcher.Download(contree);
        _launcher.Eternalfest.IsUnreachable = true;

        await _launcher.Play(contree);

        Assert.Single(_launcher.FlashPlayer.Played);
    }

    /// @spec play::valid-mode-and-options
    [Theory]
    [InlineData("nightmare", new string[0])]
    [InlineData("solo", new[] { "debug" })]
    [InlineData("solo", new[] { "lifesharing" })]
    public async Task Refuses_a_mode_or_option_the_contree_doesnt_offer(string mode, string[] options)
    {
        var contree = Published("Accumulation");

        await Assert.ThrowsAsync<InvalidRunChoiceException>(() => _launcher.Play(contree, new RunChoices(mode, options)));

        Assert.Empty(_launcher.FlashPlayer.Played);
    }

    /// @spec play::valid-mode-and-options
    [Fact]
    public async Task Allows_an_option_that_progression_locks_online()
    {
        var contree = Published("Accumulation");

        await _launcher.Play(contree, new RunChoices("solo", ["ninja"]));

        Assert.Equal(["ninja"], Assert.Single(_launcher.FlashPlayer.Played).Run.Options);
    }

    /// @spec play::launches-ruffle
    [Fact]
    public async Task Serves_the_loader_while_the_game_runs()
    {
        var contree = Published("Accumulation");

        await _launcher.Play(contree);

        Assert.Equal(await File.ReadAllBytesAsync(Path.Combine(AppContext.BaseDirectory, "flash", "loader.swf"), TestContext.Current.CancellationToken), Assert.Single(_launcher.FlashPlayer.LoadersFetched));
    }

    /// @spec play::one-game-at-a-time
    [Fact]
    public async Task Refuses_a_second_game_while_one_runs()
    {
        var first = Published("Accumulation");
        var second = Published("Dojo");
        await _launcher.Download(second);
        _launcher.FlashPlayer.KeepWindowOpen();
        var running = _launcher.Play(first);
        await WaitUntil(() => _launcher.FlashPlayer.Played.Count == 1);

        await Assert.ThrowsAsync<GameAlreadyRunningException>(() => _launcher.Play(second));

        _launcher.FlashPlayer.CloseWindow();
        await running;
    }

    /// @spec play::tears-down-on-exit
    [Fact]
    public async Task Stops_the_backend_once_the_game_window_closes()
    {
        var contree = Published("Accumulation");

        await _launcher.Play(contree);

        using var http = new HttpClient { BaseAddress = _launcher.FlashPlayer.Played[0].Origin };
        await Assert.ThrowsAsync<HttpRequestException>(() => http.GetAsync("/assets/loader.swf", TestContext.Current.CancellationToken));
    }

    /// @spec play::tears-down-on-exit
    [Fact]
    public async Task Can_play_again_after_the_player_crashed()
    {
        var contree = Published("Accumulation");
        _launcher.FlashPlayer.Crashes = true;
        await Assert.ThrowsAsync<InvalidOperationException>(() => _launcher.Play(contree));

        _launcher.FlashPlayer.Crashes = false;
        await _launcher.Play(contree);

        Assert.Equal(2, _launcher.FlashPlayer.Played.Count);
    }

    /// @spec play::warns-newer-loader
    [Theory]
    [InlineData("5.1.2", false)]
    [InlineData("5.0.0", false)]
    [InlineData("5.1.3", true)]
    [InlineData("6.0.0", true)]
    public async Task Tells_when_a_contree_requires_a_newer_loader(string required, bool warns)
    {
        var contree = Published("Accumulation", contree => contree.RequiringLoader(required));
        var game = await _launcher.Download(contree);

        Assert.Equal(warns, game.Build.RequiresNewerLoaderThan(new Version(5, 1, 2)));
    }

    private PublishedContree Published(string name, Func<PublishedContree, PublishedContree>? customize = null)
    {
        var contree = (customize ?? (contree => contree))(PublishedContree.Named(name));
        _launcher.Eternalfest.Publishing(contree);
        return contree;
    }

    private static async Task WaitUntil(Func<bool> condition)
    {
        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(5));
        while (!condition())
            await Task.Delay(10, timeout.Token);
    }
}
