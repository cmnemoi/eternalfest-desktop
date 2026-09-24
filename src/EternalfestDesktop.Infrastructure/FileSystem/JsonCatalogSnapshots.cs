using System.Text.Json;
using EternalfestDesktop.Application;
using EternalfestDesktop.Domain;

namespace EternalfestDesktop.Infrastructure.FileSystem;

/// <summary>Keeps the last catalog in a JSON file.</summary>
public sealed class JsonCatalogSnapshots(string path) : CatalogSnapshots
{
    public async Task Save(IReadOnlyList<CatalogEntry> catalog, CancellationToken cancellationToken)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        var temporary = $"{path}.{Guid.NewGuid():N}.part";
        await using (var file = File.Create(temporary))
            await JsonSerializer.SerializeAsync(file, catalog, cancellationToken: cancellationToken);
        File.Move(temporary, path, overwrite: true);
    }

    public async Task<IReadOnlyList<CatalogEntry>?> Load(CancellationToken cancellationToken)
    {
        if (!File.Exists(path))
            return null;
        try
        {
            await using var file = File.OpenRead(path);
            return await JsonSerializer.DeserializeAsync<List<CatalogEntry>>(file, cancellationToken: cancellationToken);
        }
        catch (JsonException)
        {
            // A catalog saved by an older version: the next refresh replaces it
            return null;
        }
    }
}
