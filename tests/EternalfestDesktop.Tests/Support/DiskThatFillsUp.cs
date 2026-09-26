using EternalfestDesktop.Application;
using EternalfestDesktop.Domain;

namespace EternalfestDesktop.Tests.Support;

/// <summary>The real cache folder, on a disk that runs out of space halfway through writing a chosen blob.</summary>
internal sealed class DiskThatFillsUp(GameStore store) : GameStore
{
    public BlobId? FillsUpAtBlob { get; set; }

    public Task SaveBlob(BlobId id, Stream content, CancellationToken cancellationToken) =>
        store.SaveBlob(id, id == FillsUpAtBlob ? new FillingUpStream(content) : content, cancellationToken);

    public Task<Game?> FindGame(GameId id, CancellationToken cancellationToken) => store.FindGame(id, cancellationToken);
    public Task<IReadOnlyList<Game>> ListGames(CancellationToken cancellationToken) => store.ListGames(cancellationToken);
    public Task SaveGame(Game game, CancellationToken cancellationToken) => store.SaveGame(game, cancellationToken);
    public bool HasBlob(BlobId id) => store.HasBlob(id);
    public Stream OpenBlob(BlobId id) => store.OpenBlob(id);
    public Task Clear(CancellationToken cancellationToken) => store.Clear(cancellationToken);

    /// <summary>Lets the first bytes through to the file, then fails as the operating system does on a full disk.</summary>
    private sealed class FillingUpStream(Stream content) : Stream
    {
        private bool _hasWrittenSome;

        public override bool CanRead => true;
        public override bool CanSeek => false;
        public override bool CanWrite => false;
        public override long Length => content.Length;
        public override long Position { get => content.Position; set => throw new NotSupportedException(); }

        public override int Read(byte[] buffer, int offset, int count) => ReadAsync(buffer.AsMemory(offset, count)).AsTask().GetAwaiter().GetResult();

        public override async ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken = default)
        {
            if (_hasWrittenSome)
                throw new IOException("No space left on device");
            _hasWrittenSome = true;
            return await content.ReadAsync(buffer[..Math.Max(1, buffer.Length / 2)], cancellationToken);
        }

        public override Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken) =>
            ReadAsync(buffer.AsMemory(offset, count), cancellationToken).AsTask();

        public override void Flush() { }
        public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();
        public override void SetLength(long value) => throw new NotSupportedException();
        public override void Write(byte[] buffer, int offset, int count) => throw new NotSupportedException();
    }
}
