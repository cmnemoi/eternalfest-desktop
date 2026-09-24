using EternalfestDesktop.Domain;

namespace EternalfestDesktop.Application;

/// <summary>Where blobs are downloaded from.</summary>
public interface BlobSource
{
    /// <exception cref="EternalfestUnreachableException" />
    Task<Stream> Open(BlobId id, CancellationToken cancellationToken);
}
