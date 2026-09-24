using EternalfestDesktop.Domain;

namespace EternalfestDesktop.Application;

public sealed class EternalfestUnreachableException(string message, Exception? innerException = null)
    : Exception(message, innerException);

public sealed class GameNotFoundException(GameId id)
    : Exception($"Contrée {id} isn't published on Eternalfest.")
{
    public GameId Id { get; } = id;
}

public sealed class CorruptedBlobException(BlobId id, string reason)
    : Exception($"Blob {id} is corrupted: {reason}")
{
    public BlobId Id { get; } = id;
}
