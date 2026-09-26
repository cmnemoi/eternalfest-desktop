using EternalfestDesktop.Application;
using EternalfestDesktop.Domain;
using EternalfestDesktop.Infrastructure.Quests;
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

    /// @spec profile::complete-by-default
    /// @spec profile::complete-inventory
    /// @spec backend::starts-run-player-inventory
    [Fact]
    public async Task Plays_the_complete_profile_by_default()
    {
        var contree = Published("Hammerfest", contree => contree.WithContent(CavernesContent));

        await _launcher.Play(contree);

        var start = Assert.Single(_launcher.FlashPlayer.RunsStarted);
        var families = start["families"]!.GetValue<string>().Split(',').Select(int.Parse).ToList();
        Assert.Superset(new HashSet<int> { 100, 102, 103, 104, 105, 108 }, families.ToHashSet());
        var items = start["items"]!.AsObject();
        Assert.Equal(9999, items["1000"]!.GetValue<int>());
        Assert.Equal(9999, items["102"]!.GetValue<int>());
        Assert.All(items, item => Assert.Equal(9999, item.Value!.GetValue<int>()));
    }

    /// @spec profile::new-player-as-published
    /// @spec backend::starts-run-player-inventory
    [Fact]
    public async Task Plays_a_new_player_with_the_contree_as_published()
    {
        var contree = Published("Hammerfest", contree => contree.WithContent(CavernesContent).WithFamilies("0,7,1000"));

        await _launcher.Play(contree, new RunChoices(Profile: PlayerProfile.NewPlayer));

        var start = Assert.Single(_launcher.FlashPlayer.RunsStarted);
        Assert.Equal("0,7,1000", start["families"]!.GetValue<string>());
        Assert.Empty(start["items"]!.AsObject());
    }

    /// @spec play::valid-mode-and-options
    /// @spec backend::serves-game-full-options
    [Fact]
    public async Task Plays_an_option_the_complete_profile_unlocks()
    {
        var contree = Published("Hammerfest", contree => contree.WithOption("solo", "insight", visible: false));

        await _launcher.Play(contree, new RunChoices("solo", ["insight"]));

        Assert.Equal(["insight"], Assert.Single(_launcher.FlashPlayer.Played).Run.Options);
        var insight = _launcher.FlashPlayer.GamesServed.Single()["channels"]!["active"]!["build"]!["modes"]!["solo"]!["options"]!["insight"]!;
        Assert.True(insight["is_visible"]!.GetValue<bool>());
        Assert.True(insight["is_enabled"]!.GetValue<bool>());
    }

    /// @spec play::valid-mode-and-options
    [Fact]
    public async Task Refuses_an_option_a_new_player_hasnt_unlocked()
    {
        var contree = Published("Hammerfest", contree => contree.WithOption("solo", "insight", visible: false));

        await Assert.ThrowsAsync<InvalidRunChoiceException>(() =>
            _launcher.Play(contree, new RunChoices("solo", ["insight"], Profile: PlayerProfile.NewPlayer)));

        Assert.Empty(_launcher.FlashPlayer.Played);
    }

    /// @spec profile::unreadable-content
    [Fact]
    public async Task Plays_the_complete_profile_even_when_the_content_cant_be_read()
    {
        var contree = Published("Hammerfest");

        await _launcher.Play(contree);

        var start = Assert.Single(_launcher.FlashPlayer.RunsStarted);
        Assert.Contains("108", start["families"]!.GetValue<string>().Split(','));
        Assert.Equal(
            new EmbeddedQuestBook().ProgressionOf("hammerfest").RequiredItems.Order(),
            start["items"]!.AsObject().Select(item => int.Parse(item.Key, System.Globalization.CultureInfo.InvariantCulture)).Order());
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

    /// @spec play::closes-on-game-end
    [Fact]
    public async Task Closes_the_game_window_when_the_game_ends()
    {
        var contree = Published("Accumulation");
        _launcher.FlashPlayer.LosesAtLevel(11, scores: 12345);

        var result = await _launcher.Play(contree);

        Assert.True(_launcher.FlashPlayer.WindowClosedByLauncher);
        Assert.NotNull(result);
        Assert.False(result.IsVictory);
        Assert.Equal(11, result.HighestLevel);
        Assert.Equal([12345], result.Scores);
    }

    /// @spec play::closes-on-game-end
    /// @spec play::tears-down-on-exit
    [Fact]
    public async Task Stops_the_backend_once_the_game_ends()
    {
        var contree = Published("Accumulation");
        _launcher.FlashPlayer.LosesAtLevel(11, scores: 12345);

        await _launcher.Play(contree);

        using var http = new HttpClient { BaseAddress = _launcher.FlashPlayer.Played[0].Origin };
        await Assert.ThrowsAsync<HttpRequestException>(() => http.GetAsync("/assets/loader.swf", TestContext.Current.CancellationToken));
    }

    /// @spec play::closes-on-game-end
    [Fact]
    public async Task Has_no_result_when_the_player_closes_the_window()
    {
        var contree = Published("Accumulation");

        var result = await _launcher.Play(contree);

        Assert.Null(result);
        Assert.False(_launcher.FlashPlayer.WindowClosedByLauncher);
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

    /// <summary>A content XML listing a special item, the carrot (102), and a score item (1000).</summary>
    private const string CavernesContent = """
        <game><items type="special"><family id="0"><item id="0"/><item id="102"/></family></items><items type="score"><family id="1000"><item id="1000"/></family></items></game>
        """;

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
