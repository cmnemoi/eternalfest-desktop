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

    public FakeEternalfestServer Publishing(PublishedContree contree)
    {
        PublishingGame(contree.Id.ToString(), contree.Document());
        foreach (var (id, bytes) in contree.Blobs)
            PublishingBlob(id.ToString(), bytes);
        return this;
    }

    /// <summary>Blob downloads fail from this one on, as if the connection dropped.</summary>
    public BlobId? DropsConnectionAtBlob { get; set; }

    public static JsonObject ListingItem(string displayName, string? id = null)
    {
        var item = Fixture.ReadObject("EternalfestApi/games-listing-item-hammerfest.json");
        item["id"] = id ?? Guid.NewGuid().ToString();
        item["channels"]!["items"]![0]!["build"]!["display_name"] = displayName;
        return item;
    }

    public IEnumerable<HttpRequestMessage> BlobDownloads =>
        ReceivedRequests.Where(request => request.RequestUri!.AbsolutePath.StartsWith("/api/v1/blobs/", StringComparison.Ordinal));

    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        ReceivedRequests.Add(request);
        if (IsUnreachable)
            throw new HttpRequestException("Connection refused");

        var path = request.RequestUri!.AbsolutePath;
        if (request.Method == HttpMethod.Get && path == "/api/v1/games")
            return Task.FromResult(Json(Listing(request.RequestUri)));

        const string gamesPrefix = "/api/v1/games/";
        if (request.Method == HttpMethod.Get && path.StartsWith(gamesPrefix, StringComparison.Ordinal))
        {
            var id = path[gamesPrefix.Length..];
            return Task.FromResult(_gameDocuments.TryGetValue(id, out var document) ? Json(document) : NotFound());
        }

        const string blobsPrefix = "/api/v1/blobs/";
        if (request.Method == HttpMethod.Get && path.StartsWith(blobsPrefix, StringComparison.Ordinal) && path.EndsWith("/raw", StringComparison.Ordinal))
        {
            var id = path[blobsPrefix.Length..^"/raw".Length];
            if (DropsConnectionAtBlob?.ToString() == id)
                throw new HttpRequestException("Connection reset");
            return Task.FromResult(_blobs.TryGetValue(id, out var bytes)
                ? new HttpResponseMessage(HttpStatusCode.OK) { Content = new ByteArrayContent(bytes) }
                : NotFound());
        }

        return Task.FromResult(NotFound());
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

    private static HttpResponseMessage NotFound() =>
        new(HttpStatusCode.NotFound) { Content = new StringContent("""{"error":"not found"}""", Encoding.UTF8, "application/json") };
}
