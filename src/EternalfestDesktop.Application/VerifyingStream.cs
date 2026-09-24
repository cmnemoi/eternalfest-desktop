using System.Security.Cryptography;
using EternalfestDesktop.Domain;

namespace EternalfestDesktop.Application;

/// <summary>Reads a blob and throws once its bytes turn out not to match its published size and SHA-256 digest.</summary>
/// @spec store::verifies-blob-digest
internal sealed class VerifyingStream(Stream inner, Blob expected, Action<long> onRead) : Stream
{
    private readonly IncrementalHash _hash = IncrementalHash.CreateHash(HashAlgorithmName.SHA256);
    private long _read;

    public override bool CanRead => true;
    public override bool CanSeek => false;
    public override bool CanWrite => false;
    public override long Length => expected.ByteSize;
    public override long Position { get => _read; set => throw new NotSupportedException(); }

    public override int Read(byte[] buffer, int offset, int count) => Verify(inner.Read(buffer, offset, count), buffer.AsSpan(offset));

    public override async ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken = default) =>
        Verify(await inner.ReadAsync(buffer, cancellationToken), buffer.Span);

    public override Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken) =>
        ReadAsync(buffer.AsMemory(offset, count), cancellationToken).AsTask();

    private int Verify(int count, ReadOnlySpan<byte> buffer)
    {
        _hash.AppendData(buffer[..count]);
        _read += count;
        if (_read > expected.ByteSize)
            throw new CorruptedBlobException(expected.Id, $"more than the {expected.ByteSize} published bytes");
        if (count == 0)
        {
            if (_read != expected.ByteSize)
                throw new CorruptedBlobException(expected.Id, $"{_read} bytes instead of {expected.ByteSize}");
            var digest = Convert.ToHexStringLower(_hash.GetHashAndReset());
            if (!string.Equals(digest, expected.Sha256, StringComparison.OrdinalIgnoreCase))
                throw new CorruptedBlobException(expected.Id, $"SHA-256 {digest} instead of {expected.Sha256}");
        }
        else
        {
            onRead(_read);
        }
        return count;
    }

    public override void Flush() { }
    public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();
    public override void SetLength(long value) => throw new NotSupportedException();
    public override void Write(byte[] buffer, int offset, int count) => throw new NotSupportedException();

    protected override void Dispose(bool disposing)
    {
        if (disposing)
            _hash.Dispose();
        base.Dispose(disposing);
    }
}
