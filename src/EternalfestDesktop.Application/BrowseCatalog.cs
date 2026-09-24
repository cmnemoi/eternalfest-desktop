using EternalfestDesktop.Domain;

namespace EternalfestDesktop.Application;

/// <summary>A contrée in the library, and what the cache knows about it.</summary>
public sealed record LibraryEntry(CatalogEntry Contree, string? DownloadedVersion)
{
    public bool IsDownloaded => DownloadedVersion is not null;

    /// @spec store::detects-newer-build
    public bool IsUpdateAvailable => IsDownloaded && DownloadedVersion != Contree.Version;
}

/// <param name="IsOffline">Eternalfest couldn't be reached: the catalog is the last one saved.</param>
public sealed record Library(IReadOnlyList<LibraryEntry> Entries, bool IsOffline);

/// <summary>Lists the contrées to play: the live catalog, or the last one saved when Eternalfest can't be reached.</summary>
public sealed class BrowseCatalog(GameCatalog catalog, CatalogSnapshots snapshots, GameStore store)
{
    /// @spec catalog::last-known-catalog-offline
    /// @spec catalog::first-launch-without-network
    public async Task<Library> Execute(CancellationToken cancellationToken)
    {
        var downloaded = (await store.ListGames(cancellationToken)).ToDictionary(game => game.Id);
        IReadOnlyList<CatalogEntry> entries;
        bool isOffline;
        try
        {
            entries = await catalog.ListPublicGames(cancellationToken);
            await snapshots.Save(entries, cancellationToken);
            isOffline = false;
        }
        catch (EternalfestUnreachableException)
        {
            entries = await snapshots.Load(cancellationToken) ?? [];
            isOffline = true;
        }

        var listed = entries.Select(entry => entry.Id).ToHashSet();
        var downloadedOnly = downloaded.Values
            .Where(game => !listed.Contains(game.Id))
            .Select(game => new CatalogEntry(game.Id, game.Key, game.Build.Version, game.DisplayName, game.Description, game.Build.Icon));
        return new Library(
            entries.Concat(downloadedOnly)
                .Select(entry => new LibraryEntry(entry, downloaded.GetValueOrDefault(entry.Id)?.Build.Version))
                .ToList(),
            isOffline);
    }
}
