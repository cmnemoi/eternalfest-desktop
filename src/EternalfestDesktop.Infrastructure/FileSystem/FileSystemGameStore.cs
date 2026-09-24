using EternalfestDesktop.Application;
using EternalfestDesktop.Domain;
using EternalfestDesktop.Infrastructure.EternalfestApi;

namespace EternalfestDesktop.Infrastructure.FileSystem;

/// <summary>
/// Keeps downloaded contrées in a folder: <c>blobs/{id}</c> for each blob, and <c>games/{id}.json</c> for each
/// downloaded contrée, holding the document Eternalfest published for it.
/// Files are written next to their destination then moved, so an interrupted write never leaves a partial file.
/// </summary>
public sealed class FileSystemGameStore(string root) : GameStore
{
    private string BlobsFolder => Path.Combine(root, "blobs");
    private string GamesFolder => Path.Combine(root, "games");

    /// @spec store::downloaded-only-when-complete
    public async Task<Game?> FindGame(GameId id, CancellationToken cancellationToken)
    {
        var path = GamePath(id);
        return File.Exists(path) ? EternalfestJson.ParseGame(await File.ReadAllTextAsync(path, cancellationToken)) : null;
    }

    public async Task<IReadOnlyList<Game>> ListGames(CancellationToken cancellationToken)
    {
        if (!Directory.Exists(GamesFolder))
            return [];
        var games = new List<Game>();
        foreach (var path in Directory.EnumerateFiles(GamesFolder, "*.json"))
            games.Add(EternalfestJson.ParseGame(await File.ReadAllTextAsync(path, cancellationToken)));
        return games;
    }

    public Task SaveGame(Game game, CancellationToken cancellationToken) =>
        WriteAtomically(GamePath(game.Id), async file =>
        {
            await using var writer = new StreamWriter(file);
            await writer.WriteAsync(game.Document.Json.AsMemory(), cancellationToken);
        });

    public bool HasBlob(BlobId id) => File.Exists(BlobPath(id));

    public Task SaveBlob(BlobId id, Stream content, CancellationToken cancellationToken) =>
        WriteAtomically(BlobPath(id), file => content.CopyToAsync(file, cancellationToken));

    public Stream OpenBlob(BlobId id) => File.OpenRead(BlobPath(id));

    /// @spec store::clears-cache
    public Task Clear(CancellationToken cancellationToken)
    {
        // Games first: a contrée must never look downloaded once one of its blobs is gone
        foreach (var folder in new[] { GamesFolder, BlobsFolder })
            if (Directory.Exists(folder))
                Directory.Delete(folder, recursive: true);
        return Task.CompletedTask;
    }

    private string GamePath(GameId id) => Path.Combine(GamesFolder, $"{id}.json");
    private string BlobPath(BlobId id) => Path.Combine(BlobsFolder, id.ToString());

    private static async Task WriteAtomically(string path, Func<Stream, Task> write)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        var temporary = $"{path}.{Guid.NewGuid():N}.part";
        try
        {
            await using (var file = new FileStream(temporary, FileMode.CreateNew, FileAccess.Write, FileShare.None, 81920, useAsync: true))
                await write(file);
            File.Move(temporary, path, overwrite: true);
        }
        finally
        {
            File.Delete(temporary);
        }
    }
}
