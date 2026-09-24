using EternalfestDesktop.Domain;

namespace EternalfestDesktop.Application;

/// <summary>The local cache of downloaded contrées and their blobs.</summary>
public interface GameStore
{
    /// <summary>A contrée counts as downloaded only once it was saved, after all its blobs.</summary>
    Task<Game?> FindGame(GameId id, CancellationToken cancellationToken);

    Task<IReadOnlyList<Game>> ListGames(CancellationToken cancellationToken);

    Task SaveGame(Game game, CancellationToken cancellationToken);

    bool HasBlob(BlobId id);

    /// <summary>Keeps the blob only if <paramref name="content" /> is read to its end without throwing.</summary>
    Task SaveBlob(BlobId id, Stream content, CancellationToken cancellationToken);

    /// <exception cref="FileNotFoundException">The blob isn't in the cache.</exception>
    Stream OpenBlob(BlobId id);

    Task Clear(CancellationToken cancellationToken);
}
