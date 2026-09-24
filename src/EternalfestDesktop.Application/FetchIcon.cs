using EternalfestDesktop.Domain;

namespace EternalfestDesktop.Application;

/// <summary>Gets a contrée's icon, downloading it once into the cache like any other blob.</summary>
public sealed class FetchIcon(BlobSource blobs, GameStore store)
{
    /// <returns>The icon's bytes, or null when it can't be downloaded now.</returns>
    public async Task<Stream?> Execute(Blob icon, CancellationToken cancellationToken)
    {
        if (!store.HasBlob(icon.Id))
        {
            try
            {
                await using var remote = await blobs.Open(icon.Id, cancellationToken);
                await using var verified = new VerifyingStream(remote, icon, _ => { });
                await store.SaveBlob(icon.Id, verified, cancellationToken);
            }
            catch (Exception exception) when (exception is EternalfestUnreachableException or CorruptedBlobException)
            {
                return null;
            }
        }
        return store.OpenBlob(icon.Id);
    }
}
