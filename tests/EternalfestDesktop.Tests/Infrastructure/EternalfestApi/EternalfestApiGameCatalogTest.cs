using System.Text.Json.Nodes;
using EternalfestDesktop.Application;
using EternalfestDesktop.Domain;
using EternalfestDesktop.Infrastructure.EternalfestApi;
using EternalfestDesktop.Tests.Support;
using Microsoft.Extensions.Logging.Abstractions;

namespace EternalfestDesktop.Tests.Infrastructure.EternalfestApi;

public sealed class EternalfestApiGameCatalogTest : IDisposable
{
    private static readonly GameId Hammerfest = GameId.Parse("0dc0d559-de83-4e0c-982d-fc56100dfdd5");

    private readonly FakeEternalfestServer _server = new();
    private readonly EternalfestApiGameCatalog _catalog;

    public EternalfestApiGameCatalogTest()
    {
        _catalog = new EternalfestApiGameCatalog(_server.CreateClient(), NullLogger<EternalfestApiGameCatalog>.Instance);
    }

    public void Dispose() => _server.Dispose();

    /// @spec catalog::lists-public-contrees
    [Fact]
    public async Task Lists_every_public_contree_across_pages()
    {
        _server.ListingGames(123);

        var entries = await _catalog.ListPublicGames(TestContext.Current.CancellationToken);

        Assert.Equal(123, entries.Count);
        Assert.Equal("Contrée 0", entries[0].DisplayName.Default);
        Assert.Equal("Contrée 122", entries[^1].DisplayName.Default);
    }

    /// @spec catalog::lists-public-contrees
    [Fact]
    public async Task Lists_nothing_when_no_contree_is_public()
    {
        var entries = await _catalog.ListPublicGames(TestContext.Current.CancellationToken);

        Assert.Empty(entries);
    }

    /// @spec catalog::lists-public-contrees
    [Fact]
    public async Task Reads_a_listed_contree()
    {
        _server.ListingGame(Fixture.ReadObject("EternalfestApi/games-listing-item-hammerfest.json"));

        var entry = Assert.Single(await _catalog.ListPublicGames(TestContext.Current.CancellationToken));

        Assert.Equal(Hammerfest, entry.Id);
        Assert.Equal("hammerfest", entry.Key);
        Assert.Equal("2.5.1", entry.Version);
        Assert.Equal("Les Cavernes de Hammerfest", entry.DisplayName.Default);
        Assert.Equal("The Caverns of Hammerfest", entry.DisplayName.In("en-US"));
        Assert.StartsWith("Le jeu officiel Hammerfest", entry.Description.Default, StringComparison.Ordinal);
        Assert.Equal(55707, entry.Icon!.ByteSize);
    }

    /// @spec catalog::lists-public-contrees
    [Fact]
    public async Task Skips_hidden_and_malformed_contrees()
    {
        var malformed = FakeEternalfestServer.ListingItem("Broken");
        malformed["channels"]!["items"] = new JsonArray();
        _server.ListingGame(FakeEternalfestServer.ListingItem("Before")).ListingGame(null).ListingGame(malformed).ListingGame(FakeEternalfestServer.ListingItem("After"));

        var entries = await _catalog.ListPublicGames(TestContext.Current.CancellationToken);

        Assert.Equal(["Before", "After"], entries.Select(entry => entry.DisplayName.Default));
    }

    /// @spec catalog::reads-active-build
    [Fact]
    public async Task Reads_the_active_build_of_a_contree()
    {
        _server.PublishingGame(Hammerfest.ToString(), Fixture.Read("EternalfestApi/game-hammerfest.json"));

        var game = await _catalog.GetGame(Hammerfest, TestContext.Current.CancellationToken);

        var build = game.Build;
        Assert.Equal("main", game.ChannelKey);
        Assert.Equal("2.5.1", build.Version);
        Assert.Equal("5.1.2", build.LoaderVersion);
        Assert.Equal("fr-FR", build.MainLocale);
        var engine = Assert.IsType<CustomEngine>(build.Engine);
        Assert.Equal(BlobId.Parse("e34d9b27-3fd2-4694-aab3-5a84efb9fb94"), engine.Blob.Id);
        Assert.Equal("3bb99e6c3ba35d8e3d1ffd10548b4751c66a2b584372e4386f1cfe77b10dccba", engine.Blob.Sha256);
        Assert.Equal(BlobId.Parse("ddc74a65-4b18-418f-83c9-892f95526eca"), build.Patcher!.Id);
        Assert.Equal(BlobId.Parse("b1377dc1-0ba5-40b1-a973-deb8aa23e302"), build.Content!.Id);
        Assert.Equal(BlobId.Parse("34740aaa-a6ed-4768-8e41-5ac28d1d7372"), build.ContentI18n!.Id);
        Assert.Equal(6, build.Musics.Count);
        Assert.Equal(["solo", "multicoop", "deluxe"], build.Modes.Select(mode => mode.Key).Take(3));
        var ninja = build.Modes[0].Options.Single(option => option.Key == "ninja");
        Assert.Equal(new GameOption("ninja", "Ninjutsu", IsVisible: true, IsEnabled: false, DefaultValue: false), ninja);
        Assert.StartsWith("0,1,2,3,4,10", build.Families, StringComparison.Ordinal);
        Assert.Equal(BlobId.Parse("48469ca3-e36a-4d45-b282-385e4ed7d025"), build.LocalizedContent["en-US"].Id);
    }

    /// @spec catalog::reads-active-build
    [Fact]
    public async Task Reads_a_build_running_on_the_base_engine()
    {
        var document = Fixture.ReadObject("EternalfestApi/game-hammerfest.json");
        document["channels"]!["active"]!["build"]!["engine"] = new JsonObject { ["type"] = "V96" };
        _server.PublishingGame(Hammerfest.ToString(), document.ToJsonString());

        var game = await _catalog.GetGame(Hammerfest, TestContext.Current.CancellationToken);

        Assert.IsType<BaseEngine>(game.Build.Engine);
    }

    /// @spec catalog::reads-active-build
    [Fact]
    public async Task Keeps_the_published_document_of_a_contree()
    {
        var published = Fixture.Read("EternalfestApi/game-hammerfest.json");
        _server.PublishingGame(Hammerfest.ToString(), published);

        var game = await _catalog.GetGame(Hammerfest, TestContext.Current.CancellationToken);

        Assert.True(JsonNode.DeepEquals(JsonNode.Parse(published), JsonNode.Parse(game.Document.Json)));
    }

    /// @spec catalog::reads-active-build
    [Fact]
    public async Task Fails_explicitly_on_an_unknown_contree()
    {
        await Assert.ThrowsAsync<GameNotFoundException>(() => _catalog.GetGame(Hammerfest, TestContext.Current.CancellationToken));
    }

    /// @spec catalog::read-only-anonymous
    [Fact]
    public async Task Only_sends_anonymous_reads_that_identify_the_app()
    {
        _server.ListingGames(3).PublishingGame(Hammerfest.ToString(), Fixture.Read("EternalfestApi/game-hammerfest.json"));

        await _catalog.ListPublicGames(TestContext.Current.CancellationToken);
        await _catalog.GetGame(Hammerfest, TestContext.Current.CancellationToken);

        Assert.All(_server.ReceivedRequests, request =>
        {
            Assert.Equal(HttpMethod.Get, request.Method);
            Assert.False(request.Headers.Contains("Cookie"));
            Assert.False(request.Headers.Contains("Authorization"));
            Assert.StartsWith("EternalfestDesktop/", request.Headers.UserAgent.ToString(), StringComparison.Ordinal);
        });
    }

    /// @spec catalog::last-known-catalog-offline
    [Fact]
    public async Task Fails_explicitly_when_eternalfest_is_unreachable()
    {
        _server.IsUnreachable = true;

        await Assert.ThrowsAsync<EternalfestUnreachableException>(() => _catalog.ListPublicGames(TestContext.Current.CancellationToken));
    }

    /// @spec catalog::last-known-catalog-offline
    [Fact]
    public async Task Fails_explicitly_when_eternalfest_answers_with_an_error()
    {
        _server.IsFailing = true;

        var failure = await Assert.ThrowsAsync<EternalfestUnreachableException>(() => _catalog.ListPublicGames(TestContext.Current.CancellationToken));

        Assert.Contains("500", failure.Message, StringComparison.Ordinal);
    }

    /// @spec catalog::last-known-catalog-offline
    [Fact]
    public async Task Fails_explicitly_when_eternalfest_takes_too_long_to_answer()
    {
        _server.IsHanging = true;
        using var http = _server.CreateClient();
        http.Timeout = TimeSpan.FromMilliseconds(50);
        var catalog = new EternalfestApiGameCatalog(http, NullLogger<EternalfestApiGameCatalog>.Instance);

        await Assert.ThrowsAsync<EternalfestUnreachableException>(() => catalog.ListPublicGames(TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task Stops_without_blaming_eternalfest_when_the_player_cancels()
    {
        _server.IsHanging = true;
        using var cancellation = new CancellationTokenSource(TimeSpan.FromMilliseconds(50));

        var stop = await Assert.ThrowsAnyAsync<OperationCanceledException>(() => _catalog.ListPublicGames(cancellation.Token));

        Assert.Equal(cancellation.Token, stop.CancellationToken);
    }
}
