namespace EternalfestDesktop.Application;

/// <summary>Forgets every downloaded contrée.</summary>
public sealed class ClearCache(GameStore store, PlayGame playGame)
{
    /// @spec store::clears-cache
    /// <exception cref="GameAlreadyRunningException">A contrée is running: its files are in use.</exception>
    public Task Execute(CancellationToken cancellationToken) =>
        playGame.IsPlaying ? throw new GameAlreadyRunningException() : store.Clear(cancellationToken);
}
