using EternalfestDesktop.Domain;

namespace EternalfestDesktop.Application;

/// <summary>Pretends to be Eternalfest for the loader, on this computer, during one game.</summary>
public interface OfflineBackend
{
    /// <summary>Serves a downloaded contrée, unlocked for the player, and its run until the returned backend is disposed.</summary>
    Task<RunningBackend> Start(Game game, Run run, Inventory inventory, CancellationToken cancellationToken);
}

public interface RunningBackend : IAsyncDisposable
{
    /// <summary>Where the loader must be loaded from: the loader, the API and blobs all share this origin.</summary>
    Uri Origin { get; }

    /// <summary>Completes when the loader reports the run result: the game is over.</summary>
    Task<RunResult> GameEnded { get; }
}
