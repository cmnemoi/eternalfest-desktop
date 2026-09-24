using EternalfestDesktop.Domain;

namespace EternalfestDesktop.Application;

/// <summary>The last catalog read from Eternalfest, kept to browse it offline.</summary>
public interface CatalogSnapshots
{
    Task Save(IReadOnlyList<CatalogEntry> catalog, CancellationToken cancellationToken);

    /// <returns>null when no catalog was ever saved.</returns>
    Task<IReadOnlyList<CatalogEntry>?> Load(CancellationToken cancellationToken);
}
