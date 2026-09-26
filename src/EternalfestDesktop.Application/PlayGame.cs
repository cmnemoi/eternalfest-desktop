using EternalfestDesktop.Domain;

namespace EternalfestDesktop.Application;

/// <summary>
/// Plays a contrée offline: downloads it if needed, unlocks it for the chosen player profile,
/// serves it locally, and opens it in the Flash player.
/// </summary>
public sealed class PlayGame(
    DownloadGame downloadGame,
    QuestBook quests,
    ContreeItems contreeItems,
    OfflineBackend backend,
    FlashPlayer player,
    TimeProvider clock)
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
            var progression = quests.ProgressionOf(game.Key);
            var offlinePlayer = await PlayerFor(choices.Profile, progression, game.Build, cancellationToken);
            var unlocked = offlinePlayer.Unlocks(game, progression);
            var run = unlocked.NewRun(choices, clock.GetUtcNow());
            // @spec play::tears-down-on-exit
            await using var running = await backend.Start(unlocked, run, offlinePlayer.Inventory, cancellationToken);
            await player.Play(new FlashGame(running.Origin, unlocked, run, choices.Fullscreen), cancellationToken);
        }
        finally
        {
            Volatile.Write(ref _playing, 0);
        }
    }

    /// <summary>A new player owns nothing: their contrée's content isn't even read.</summary>
    private async Task<Player> PlayerFor(PlayerProfile profile, Progression progression, GameBuild build, CancellationToken cancellationToken) =>
        profile == PlayerProfile.NewPlayer
            ? Player.NewPlayer
            : Player.For(profile, progression, await contreeItems.ListedIn(build, cancellationToken));

    public bool IsPlaying => Volatile.Read(ref _playing) == 1;
}
