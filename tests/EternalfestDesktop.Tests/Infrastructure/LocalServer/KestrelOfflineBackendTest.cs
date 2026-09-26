using System.Net;
using System.Text.Json.Nodes;
using EternalfestDesktop.Domain;
using EternalfestDesktop.Tests.Support;

namespace EternalfestDesktop.Tests.Infrastructure.LocalServer;

public sealed class KestrelOfflineBackendTest : IDisposable
{
    private readonly TestLauncher _launcher = new();

    public void Dispose() => _launcher.Dispose();

    /// @spec backend::serves-bundled-assets
    [Theory]
    [InlineData("/assets/loader.swf", "loader.swf")]
    [InlineData("/assets/game.swf", "game.swf")]
    public async Task Serves_the_bundled_loader_and_base_engine(string path, string bundled)
    {
        await using var session = await Play(PublishedContree.Named("Accumulation"));

        using var response = await session.Get(path);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("application/x-shockwave-flash", response.Content.Headers.ContentType?.MediaType);
        Assert.Equal(await File.ReadAllBytesAsync(Path.Combine(AppContext.BaseDirectory, "flash", bundled), TestContext.Current.CancellationToken), await response.Content.ReadAsByteArrayAsync(TestContext.Current.CancellationToken));
    }

    /// @spec backend::accepts-long-loader-urls
    [Fact]
    public async Task Serves_the_loader_to_a_url_carrying_the_whole_run()
    {
        await using var session = await Play(PublishedContree.Named("Accumulation"));

        using var response = await session.Get($"/assets/loader.swf?run={new string('x', 100_000)}");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    /// @spec backend::serves-cached-blobs
    [Fact]
    public async Task Serves_the_cached_blobs_of_the_contree()
    {
        var contree = PublishedContree.Named("Accumulation");
        await using var session = await Play(contree);
        var engine = contree.BlobOf("engine");

        using var response = await session.Get($"/api/v1/blobs/{engine}/raw");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("application/octet-stream", response.Content.Headers.ContentType?.MediaType);
        Assert.Equal(contree.Blobs[engine], await response.Content.ReadAsByteArrayAsync(TestContext.Current.CancellationToken));
    }

    /// @spec backend::serves-cached-blobs
    [Fact]
    public async Task Doesnt_know_blobs_outside_the_contree()
    {
        await using var session = await Play(PublishedContree.Named("Accumulation"));

        using var response = await session.Get($"/api/v1/blobs/{Guid.NewGuid()}/raw");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    /// @spec backend::serves-game-full-options
    [Fact]
    public async Task Serves_the_contree_with_every_visible_option_enabled()
    {
        var contree = PublishedContree.Named("Accumulation");
        await using var session = await Play(contree);

        var game = await GetJson(session, $"/api/v1/games/{contree.Id}");

        var solo = game["channels"]!["active"]!["build"]!["modes"]!["solo"]!["options"]!;
        Assert.True(solo["ninja"]!["is_enabled"]!.GetValue<bool>());
        Assert.False(solo["debug"]!["is_visible"]!.GetValue<bool>());
        Assert.False(solo["debug"]!["is_enabled"]!.GetValue<bool>());
    }

    /// @spec backend::serves-game-full-options
    [Fact]
    public async Task Doesnt_know_other_contrees()
    {
        await using var session = await Play(PublishedContree.Named("Accumulation"));

        using var response = await session.Get($"/api/v1/games/{Guid.NewGuid()}");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    /// @spec backend::serves-game-full-options
    [Fact]
    public async Task Serves_the_contree_unlocked_for_the_player()
    {
        var contree = PublishedContree.Named("Accumulation");
        await using var session = await Play(contree, unlock: build => build with
        {
            Families = "0,108",
            Modes = build.Modes.Select(mode => mode.Key == "multicoop"
                ? mode with { IsVisible = false }
                : mode with { Options = mode.Options.Select(option => option.Key == "debug" ? option with { IsVisible = true } : option).ToList() }).ToList(),
        });

        var build = (await GetJson(session, $"/api/v1/games/{contree.Id}"))["channels"]!["active"]!["build"]!;

        Assert.Equal("0,108", build["families"]!.GetValue<string>());
        Assert.False(build["modes"]!["multicoop"]!["is_visible"]!.GetValue<bool>());
        Assert.True(build["modes"]!["solo"]!["options"]!["debug"]!["is_visible"]!.GetValue<bool>());
        Assert.True(build["modes"]!["solo"]!["options"]!["debug"]!["is_enabled"]!.GetValue<bool>());
    }

    /// @spec backend::starts-run-player-inventory
    [Fact]
    public async Task Starts_the_run_with_the_unlocked_families_and_the_player_inventory()
    {
        var runId = RunId.New();
        await using var session = await Play(PublishedContree.Named("Accumulation"), runId,
            build => build with { Families = "0,108,1000" },
            new Inventory(new Dictionary<int, int> { [102] = 9999, [1000] = 9999 }));

        using var response = await session.PostForm($"/api/v1/runs/{runId}/start", ("key", "0000"));

        var start = JsonNode.Parse(await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken))!;
        Assert.Equal("0,108,1000", start["families"]!.GetValue<string>());
        Assert.Equal("""{"102":9999,"1000":9999}""", start["items"]!.ToJsonString());
    }

    /// @spec backend::starts-run-player-inventory
    [Fact]
    public async Task Starts_a_new_player_run_with_the_published_families_and_an_empty_inventory()
    {
        var contree = PublishedContree.Named("Accumulation").WithFamilies("0,1,2,1000");
        var runId = RunId.New();
        await using var session = await Play(contree, runId);

        using var response = await session.PostForm($"/api/v1/runs/{runId}/start", ("key", "0000"));

        var start = JsonNode.Parse(await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken))!;
        Assert.Equal(runId.ToString(), start["run"]!["id"]!.GetValue<string>());
        Assert.Equal("0,1,2,1000", start["families"]!.GetValue<string>());
        Assert.Empty(start["items"]!.AsObject());
        Assert.NotEmpty(start["key"]!.GetValue<string>());
    }

    /// @spec backend::starts-run-player-inventory
    [Fact]
    public async Task Doesnt_start_another_run()
    {
        await using var session = await Play(PublishedContree.Named("Accumulation"));

        using var response = await session.PostForm($"/api/v1/runs/{Guid.NewGuid()}/start", ("key", "0000"));

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    /// @spec backend::discards-results
    [Fact]
    public async Task Accepts_the_result_and_echoes_it_without_keeping_it()
    {
        var runId = RunId.New();
        await using var session = await Play(PublishedContree.Named("Accumulation"), runId);
        var cacheBefore = CacheFiles();

        using var response = await session.PostForm($"/api/v1/runs/{runId}/result",
            ("is_victory", "true"), ("max_level", "103"), ("scores", "[123456]"), ("items", """{"1000":2}"""), ("stats", "{}"));

        var run = JsonNode.Parse(await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken))!;
        Assert.Equal(runId.ToString(), run["id"]!.GetValue<string>());
        Assert.True(run["result"]!["is_victory"]!.GetValue<bool>());
        Assert.Equal(103, run["result"]!["max_level"]!.GetValue<int>());
        Assert.Equal(123456, run["result"]!["scores"]![0]!.GetValue<int>());
        Assert.Equal(cacheBefore, CacheFiles());
    }

    /// @spec backend::discards-results
    /// @spec play::closes-on-game-end
    [Fact]
    public async Task Doesnt_accept_the_result_of_another_run()
    {
        await using var session = await Play(PublishedContree.Named("Accumulation"));

        using var response = await session.PostForm($"/api/v1/runs/{Guid.NewGuid()}/result",
            ("is_victory", "true"), ("max_level", "103"), ("scores", "[123456]"), ("items", "{}"), ("stats", "{}"));

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.False(session.GameEnded.IsCompleted);
    }

    /// @spec backend::discards-results
    /// @spec play::closes-on-game-end
    [Fact]
    public async Task Reports_the_game_end_with_its_result()
    {
        var runId = RunId.New();
        await using var session = await Play(PublishedContree.Named("Accumulation"), runId);

        using var response = await session.PostForm($"/api/v1/runs/{runId}/result",
            ("is_victory", "false"), ("max_level", "11"), ("scores", "[12345,678]"), ("items", "{}"), ("stats", "{}"));

        var result = await session.GameEnded.WaitAsync(TimeSpan.FromSeconds(5), TestContext.Current.CancellationToken);
        Assert.False(result.IsVictory);
        Assert.Equal(11, result.HighestLevel);
        Assert.Equal([12345, 678], result.Scores);
    }

    /// @spec play::closes-on-game-end
    [Fact]
    public async Task Doesnt_report_the_game_end_before_the_result()
    {
        var runId = RunId.New();
        await using var session = await Play(PublishedContree.Named("Accumulation"), runId);

        using var response = await session.PostForm($"/api/v1/runs/{runId}/start", ("key", "0000"));

        Assert.False(session.GameEnded.IsCompleted);
    }

    /// @spec backend::rejects-unknown-routes
    [Theory]
    [InlineData("/api/v1/auth/self")]
    [InlineData("/api/v1/games/00000000-0000-0000-0000-000000000000/leaderboard")]
    [InlineData("/favicon.ico")]
    public async Task Rejects_anything_else(string path)
    {
        await using var session = await Play(PublishedContree.Named("Accumulation"));

        using var response = await session.Get(path);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Listens_on_the_loopback_only_on_a_free_port()
    {
        var contree = PublishedContree.Named("Accumulation");
        await using var first = await Play(contree);
        await using var second = await Play(contree);

        Assert.Equal("127.0.0.1", first.Origin.Host);
        Assert.NotEqual(first.Origin.Port, second.Origin.Port);
    }

    [Fact]
    public async Task Stops_serving_once_disposed()
    {
        var session = await Play(PublishedContree.Named("Accumulation"));
        var origin = session.Origin;

        await session.DisposeAsync();

        using var http = new HttpClient { BaseAddress = origin };
        await Assert.ThrowsAsync<HttpRequestException>(() => http.GetAsync("/assets/loader.swf", TestContext.Current.CancellationToken));
    }

    private async Task<OfflineSession> Play(PublishedContree contree, RunId? runId = null, Func<GameBuild, GameBuild>? unlock = null, Inventory? inventory = null)
    {
        _launcher.Eternalfest.Publishing(contree);
        var game = await _launcher.Download(contree);
        var unlocked = game with { Build = (unlock ?? (build => build))(game.Build) };
        return await OfflineSession.Start(_launcher.Store, unlocked, OfflineSession.RunOf(unlocked, runId), inventory ?? Inventory.Empty);
    }

    private static async Task<JsonNode> GetJson(OfflineSession session, string path)
    {
        using var response = await session.Get(path);
        response.EnsureSuccessStatusCode();
        return JsonNode.Parse(await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken))!;
    }

    private string[] CacheFiles() =>
        Directory.EnumerateFiles(_launcher.CacheFolder.FullName, "*", SearchOption.AllDirectories).Order(StringComparer.Ordinal).ToArray();
}
