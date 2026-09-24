namespace EternalfestDesktop.Domain;

/// <summary>An immutable file published by Eternalfest: an engine, a patcher, level content, a music, an icon.</summary>
public sealed record Blob(BlobId Id, string MediaType, long ByteSize, string Sha256);
