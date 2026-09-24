using EternalfestDesktop.Domain;

namespace EternalfestDesktop.Application;

/// <summary>The contrées published on Eternalfest.</summary>
public interface GameCatalog
{
    /// <exception cref="EternalfestUnreachableException" />
    Task<IReadOnlyList<CatalogEntry>> ListPublicGames(CancellationToken cancellationToken);

    /// <exception cref="EternalfestUnreachableException" />
    /// <exception cref="GameNotFoundException" />
    Task<Game> GetGame(GameId id, CancellationToken cancellationToken);
}
