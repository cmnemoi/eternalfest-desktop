using EternalfestDesktop.Domain;

namespace EternalfestDesktop.Application;

/// <summary>Plays a contrée offline: downloads it if needed, serves it locally, and opens it in the Flash player.</summary>
public sealed class PlayGame(DownloadGame downloadGame, OfflineBackend backend, FlashPlayer player, TimeProvider clock)
{
    private int _playing;

    /// <summary>Returns once the game window is closed.</summary>
    /// <exception cref="GameAlreadyRunningException" />
    /// <exception cref="InvalidRunChoiceException" />
    /// <exception cref="EternalfestUnreachableException">The contrée isn't downloaded and Eternalfest can't be reached.</exception>
    /// <exception cref="FlashPlayerMissingException" />
    public async Task Execute(GameId id, RunChoices choices, IProgress<DownloadProgress>? progress, CancellationToken cancellationToken)
    {
        // @spec play::one-game-at-a-time
        if (Interlocked.Exchange(ref _playing, 1) == 1)
            throw new GameAlreadyRunningException();
        try
        {
            // @spec play::requires-downloaded-contree
            var game = await downloadGame.Execute(id, progress, cancellationToken);
            var run = game.NewRun(choices, clock.GetUtcNow());
            // @spec play::tears-down-on-exit
            await using var running = await backend.Start(game, run, cancellationToken);
            await player.Play(new FlashGame(running.Origin, game, run, choices.Fullscreen), cancellationToken);
        }
        finally
        {
            Volatile.Write(ref _playing, 0);
        }
    }

    public bool IsPlaying => Volatile.Read(ref _playing) == 1;
}
