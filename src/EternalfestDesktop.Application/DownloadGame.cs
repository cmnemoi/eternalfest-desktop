using EternalfestDesktop.Domain;

namespace EternalfestDesktop.Application;

public sealed record DownloadProgress(long DownloadedBytes, long TotalBytes);

/// <summary>Makes a contrée playable offline: downloads its active build once, and never again.</summary>
public sealed class DownloadGame(GameCatalog catalog, BlobSource blobs, GameStore store)
{
    /// @spec store::downloads-complete-build
    /// @spec store::downloaded-only-when-complete
    public async Task<Game> Execute(GameId id, IProgress<DownloadProgress>? progress, CancellationToken cancellationToken)
    {
        // @spec store::never-downloads-twice
        if (await store.FindGame(id, cancellationToken) is { } downloaded)
            return downloaded;

        var game = await catalog.GetGame(id, cancellationToken);
        var totalBytes = game.Build.ByteSize();
        var downloadedBytes = 0L;
        progress?.Report(new DownloadProgress(downloadedBytes, totalBytes));
        foreach (var blob in game.Build.Blobs())
        {
            if (!store.HasBlob(blob.Id))
            {
                await using var remote = await blobs.Open(blob.Id, cancellationToken);
                await using var verified = new VerifyingStream(remote, blob, read =>
                    progress?.Report(new DownloadProgress(downloadedBytes + read, totalBytes)));
                await store.SaveBlob(blob.Id, verified, cancellationToken);
            }
            downloadedBytes += blob.ByteSize;
            progress?.Report(new DownloadProgress(downloadedBytes, totalBytes));
        }

        await store.SaveGame(game, cancellationToken);
        return game;
    }
}
