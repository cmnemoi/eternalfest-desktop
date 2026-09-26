using System.Net;
using System.Text;
using System.Text.Json.Nodes;
using System.Web;
using EternalfestDesktop.Domain;

namespace EternalfestDesktop.Tests.Support;

/// <summary>An eternalfest.net stand-in answering like the real public API, and recording what it receives.</summary>
internal sealed class FakeEternalfestServer : HttpMessageHandler
{
    private readonly List<JsonNode?> _listedGames = [];
    private readonly Dictionary<string, string> _gameDocuments = [];
    private readonly Dictionary<string, byte[]> _blobs = [];

    public List<HttpRequestMessage> ReceivedRequests { get; } = [];
    public bool IsUnreachable { get; set; }

    /// <summary>Answers every request with a server error.</summary>
    public bool IsFailing { get; set; }

    /// <summary>Never answers, until the request is cancelled or times out.</summary>
    public bool IsHanging { get; set; }

    public HttpClient CreateClient() => new(this) { BaseAddress = new Uri("https://eternalfest.test/") };

    public FakeEternalfestServer ListingGames(int count)
    {
        for (var index = 0; index < count; index++)
            _listedGames.Add(ListingItem($"Contrée {index}"));
        return this;
    }

    public FakeEternalfestServer ListingGame(JsonNode? item)
    {
        _listedGames.Add(item);
        return this;
    }

    public FakeEternalfestServer PublishingGame(string gameId, string document)
    {
        _gameDocuments[gameId] = document;
        return this;
    }

    public FakeEternalfestServer PublishingBlob(string blobId, byte[] bytes)
    {
        _blobs[blobId] = bytes;
        return this;
    }

    /// <summary>Publishes the contrée's active build and lists it in the catalog, replacing any earlier version.</summary>
    public FakeEternalfestServer Publishing(PublishedContree contree)
    {
        var listed = contree.ListingItem();
        var index = _listedGames.FindIndex(item => item?["id"]?.GetValue<string>() == contree.Id.ToString());
        if (index >= 0)
            _listedGames[index] = listed;
        else
            _listedGames.Add(listed);
        PublishingGame(contree.Id.ToString(), contree.Document());
        foreach (var (id, bytes) in contree.Blobs)
            PublishingBlob(id.ToString(), bytes);
        return this;
    }

    /// <summary>Stops serving a blob, as when its author deletes it, while the documents still reference it.</summary>
    public FakeEternalfestServer Withdrawing(BlobId blob)
    {
        _blobs.Remove(blob.ToString());
        return this;
    }

    public FakeEternalfestServer Unlisting(PublishedContree contree)
    {
        _listedGames.RemoveAll(item => item?["id"]?.GetValue<string>() == contree.Id.ToString());
        return this;
    }

    /// <summary>Blob downloads fail from this one on, as if the connection dropped.</summary>
    public BlobId? DropsConnectionAtBlob { get; set; }

    /// <summary>This blob's download starts, then the connection drops halfway through its bytes.</summary>
    public BlobId? DropsConnectionInsideBlob { get; set; }

    public static JsonObject ListingItem(string displayName, string? id = null)
    {
        var item = Fixture.ReadObject("EternalfestApi/games-listing-item-hammerfest.json");
        item["id"] = id ?? Guid.NewGuid().ToString();
        item["channels"]!["items"]![0]!["build"]!["display_name"] = displayName;
        return item;
    }

    public IEnumerable<HttpRequestMessage> BlobDownloads =>
        ReceivedRequests.Where(request => request.RequestUri!.AbsolutePath.StartsWith("/api/v1/blobs/", StringComparison.Ordinal));

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        ReceivedRequests.Add(request);
        if (IsUnreachable)
            throw new HttpRequestException("Connection refused");
        if (IsHanging)
            await Task.Delay(Timeout.Infinite, cancellationToken);
        if (IsFailing)
            return new HttpResponseMessage(HttpStatusCode.InternalServerError) { Content = new StringContent("Internal Server Error") };
        return Answer(request);
    }

    private HttpResponseMessage Answer(HttpRequestMessage request)
    {

        var path = request.RequestUri!.AbsolutePath;
        if (request.Method == HttpMethod.Get && path == "/api/v1/games")
            return Json(Listing(request.RequestUri));

        const string gamesPrefix = "/api/v1/games/";
        if (request.Method == HttpMethod.Get && path.StartsWith(gamesPrefix, StringComparison.Ordinal))
        {
            var id = path[gamesPrefix.Length..];
            return _gameDocuments.TryGetValue(id, out var document) ? Json(document) : NotFound();
        }

        const string blobsPrefix = "/api/v1/blobs/";
        if (request.Method == HttpMethod.Get && path.StartsWith(blobsPrefix, StringComparison.Ordinal) && path.EndsWith("/raw", StringComparison.Ordinal))
        {
            var id = path[blobsPrefix.Length..^"/raw".Length];
            if (DropsConnectionAtBlob?.ToString() == id)
                throw new HttpRequestException("Connection reset");
            if (!_blobs.TryGetValue(id, out var bytes))
                return NotFound();
            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = DropsConnectionInsideBlob?.ToString() == id
                    ? new StreamContent(new DroppingStream(bytes[..(bytes.Length / 2)]))
                    : new ByteArrayContent(bytes),
            };
        }

        return NotFound();
    }

    private string Listing(Uri uri)
    {
        var query = HttpUtility.ParseQueryString(uri.Query);
        var offset = int.Parse(query["offset"] ?? "0", System.Globalization.CultureInfo.InvariantCulture);
        var limit = int.Parse(query["limit"] ?? "20", System.Globalization.CultureInfo.InvariantCulture);
        var page = new JsonArray(_listedGames.Skip(offset).Take(limit).Select(item => item?.DeepClone()).ToArray());
        return new JsonObject
        {
            ["offset"] = offset,
            ["limit"] = limit,
            ["count"] = _listedGames.Count,
            ["is_count_exact"] = false,
            ["items"] = page,
        }.ToJsonString();
    }

    private static HttpResponseMessage Json(string body) =>
        new(HttpStatusCode.OK) { Content = new StringContent(body, Encoding.UTF8, "application/json") };

    /// <summary>A response body that gives its first bytes, then fails like a dropped connection.</summary>
    private sealed class DroppingStream(byte[] firstBytes) : MemoryStream(firstBytes)
    {
        public override int Read(byte[] buffer, int offset, int count) =>
            Position < Length ? base.Read(buffer, offset, count) : throw new IOException("Connection reset by peer");

        public override ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken = default) =>
            Position < Length ? base.ReadAsync(buffer, cancellationToken) : throw new IOException("Connection reset by peer");
    }

    private static HttpResponseMessage NotFound() =>
        new(HttpStatusCode.NotFound) { Content = new StringContent("""{"error":"not found"}""", Encoding.UTF8, "application/json") };
}
