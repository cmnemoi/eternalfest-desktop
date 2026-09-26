using EternalfestDesktop.Application;
using EternalfestDesktop.Domain;
using EternalfestDesktop.Infrastructure.EternalfestApi;
using EternalfestDesktop.Infrastructure.LocalServer;
using EternalfestDesktop.Tests.Support;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace EternalfestDesktop.Tests.Infrastructure.LocalServer;

public sealed class FallbackFlashPlayerTest
{
    private readonly ShippedFlashPlayer _projector = new();
    private readonly ShippedFlashPlayer _ruffle = new();

    /// @spec play::flash-projector-first
    [Fact]
    public async Task Plays_in_the_projector_when_it_is_shipped()
    {
        var player = new FallbackFlashPlayer(_projector, _ruffle, NullLogger<FallbackFlashPlayer>.Instance);

        await player.Play(AGame(), TestContext.Current.CancellationToken);

        Assert.Single(_projector.Played);
        Assert.Empty(_ruffle.Played);
    }

    /// @spec play::flash-projector-first
    [Fact]
    public async Task Plays_in_ruffle_when_the_projector_is_not_shipped()
    {
        var game = AGame();
        var player = new FallbackFlashPlayer(new MissingFlashPlayer(), _ruffle, NullLogger<FallbackFlashPlayer>.Instance);

        await player.Play(game, TestContext.Current.CancellationToken);

        Assert.Equal([game], _ruffle.Played);
    }

    /// @spec play::flash-projector-first
    [Fact]
    public async Task Logs_why_it_plays_in_ruffle()
    {
        var log = new CapturingLogger<FallbackFlashPlayer>();
        var player = new FallbackFlashPlayer(new MissingFlashPlayer(), _ruffle, log);

        await player.Play(AGame(), TestContext.Current.CancellationToken);

        var (level, message) = Assert.Single(log.Entries);
        Assert.Equal(LogLevel.Warning, level);
        Assert.Contains("/opt/eternalfest-desktop/missing", message, StringComparison.Ordinal);
    }

    /// @spec play::tears-down-on-exit
    [Fact]
    public async Task Fails_explicitly_when_neither_is_shipped()
    {
        var player = new FallbackFlashPlayer(new MissingFlashPlayer(), new MissingFlashPlayer(), NullLogger<FallbackFlashPlayer>.Instance);

        await Assert.ThrowsAsync<FlashPlayerMissingException>(() => player.Play(AGame(), TestContext.Current.CancellationToken));
    }

    private static FlashGame AGame()
    {
        var game = EternalfestJson.ParseGame(PublishedContree.Named("Accumulation").Document());
        return new FlashGame(new Uri("http://127.0.0.1:50317"), game, game.NewRun(new RunChoices(), DateTimeOffset.UnixEpoch), Fullscreen: false);
    }

    private sealed class ShippedFlashPlayer : FlashPlayer
    {
        public List<FlashGame> Played { get; } = [];

        public Task Play(FlashGame game, CancellationToken cancellationToken)
        {
            Played.Add(game);
            return Task.CompletedTask;
        }
    }

    private sealed class MissingFlashPlayer : FlashPlayer
    {
        public Task Play(FlashGame game, CancellationToken cancellationToken) =>
            throw new FlashPlayerMissingException("/opt/eternalfest-desktop/missing");
    }
}
