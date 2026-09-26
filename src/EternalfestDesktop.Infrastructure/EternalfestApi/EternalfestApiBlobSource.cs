using EternalfestDesktop.Application;
using EternalfestDesktop.Domain;

namespace EternalfestDesktop.Infrastructure.EternalfestApi;

/// <summary>Downloads blobs from the public Eternalfest API.</summary>
public sealed class EternalfestApiBlobSource(HttpClient http) : BlobSource
{
    public async Task<Stream> Open(BlobId id, CancellationToken cancellationToken)
    {
        var response = await http.Get($"api/v1/blobs/{id}/raw", cancellationToken);
        try
        {
            await response.EnsureSuccess(cancellationToken);
            return new ResponseStream(response, await response.Content.ReadAsStreamAsync(cancellationToken));
        }
        catch
        {
            response.Dispose();
            throw;
        }
    }

    /// <summary>Releases the HTTP response along with its body.</summary>
    private sealed class ResponseStream(HttpResponseMessage response, Stream body) : Stream
    {
        public override bool CanRead => true;
        public override bool CanSeek => false;
        public override bool CanWrite => false;
        public override long Length => body.Length;
        public override long Position { get => body.Position; set => throw new NotSupportedException(); }

        public override int Read(byte[] buffer, int offset, int count) => Wrap(() => body.Read(buffer, offset, count));

        public override async ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken = default)
        {
            try
            {
                return await body.ReadAsync(buffer, cancellationToken);
            }
            catch (IOException exception)
            {
                throw new EternalfestUnreachableException("The connection to Eternalfest dropped.", exception);
            }
        }

        public override Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken) =>
            ReadAsync(buffer.AsMemory(offset, count), cancellationToken).AsTask();

        private static int Wrap(Func<int> read)
        {
            try
            {
                return read();
            }
            catch (IOException exception)
            {
                throw new EternalfestUnreachableException("The connection to Eternalfest dropped.", exception);
            }
        }

        public override void Flush() { }
        public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();
        public override void SetLength(long value) => throw new NotSupportedException();
        public override void Write(byte[] buffer, int offset, int count) => throw new NotSupportedException();

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                body.Dispose();
                response.Dispose();
            }
            base.Dispose(disposing);
        }
    }
}
