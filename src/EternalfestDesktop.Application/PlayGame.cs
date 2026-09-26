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
    /// <returns>How the game ended, or <c>null</c> when the window was closed before the loader reported it.</returns>
    /// <exception cref="GameAlreadyRunningException" />
    /// <exception cref="InvalidRunChoiceException" />
    /// <exception cref="EternalfestUnreachableException">The contrée isn't downloaded and Eternalfest can't be reached.</exception>
    /// <exception cref="FlashPlayerMissingException" />
    public async Task<RunResult?> Execute(GameId id, RunChoices choices, IProgress<DownloadProgress>? progress, CancellationToken cancellationToken)
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
            return await PlayUntilTheGameEnds(new FlashGame(running.Origin, unlocked, run, choices.Fullscreen), running, cancellationToken);
        }
        finally
        {
            Volatile.Write(ref _playing, 0);
        }
    }

    /// @spec play::closes-on-game-end
    private async Task<RunResult?> PlayUntilTheGameEnds(FlashGame game, RunningBackend running, CancellationToken cancellationToken)
    {
        using var window = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        var playing = player.Play(game, window.Token);
        await Task.WhenAny(playing, running.GameEnded);
        if (!running.GameEnded.IsCompletedSuccessfully)
        {
            await playing;
            return null;
        }
        await window.CancelAsync();
        try
        {
            await playing;
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            // The launcher closed the window itself
        }
        return await running.GameEnded;
    }

    /// <summary>A new player owns nothing: their contrée's content isn't even read.</summary>
    private async Task<Player> PlayerFor(PlayerProfile profile, Progression progression, GameBuild build, CancellationToken cancellationToken) =>
        profile == PlayerProfile.NewPlayer
            ? Player.NewPlayer
            : Player.For(profile, progression, await contreeItems.ListedIn(build, cancellationToken));

    public bool IsPlaying => Volatile.Read(ref _playing) == 1;
}
