using System.Net;
using System.Text.Json.Nodes;
using EternalfestDesktop.Domain;
using EternalfestDesktop.Infrastructure.EternalfestApi;
using EternalfestDesktop.Infrastructure.FileSystem;
using EternalfestDesktop.Tests.Support;

namespace EternalfestDesktop.Tests.Infrastructure.LocalServer;

/// <summary>
/// Replays the requests the real loader sent to eternalfest.net while playing "Les Cavernes de Hammerfest"
/// (recorded by hammerfest-tas), and checks the offline backend answers them like eternalfest.net did.
/// </summary>
public sealed class RecordedLoaderSessionTest : IAsyncLifetime
{
    private static readonly string Recording = Path.Combine(AppContext.BaseDirectory, "Fixtures", "HammerfestTasMirror");

    private readonly DirectoryInfo _cache = Directory.CreateTempSubdirectory("eternalfest-desktop-tests-");
    private OfflineSession _session = null!;

    public async ValueTask InitializeAsync()
    {
        var store = new FileSystemGameStore(_cache.FullName);
        var game = EternalfestJson.ParseGame(await File.ReadAllTextAsync(Path.Combine(Recording, "eb50d9601341dfe6.body")));
        await store.SaveGame(game, TestContext.Current.CancellationToken);
        foreach (var blob in game.Build.Blobs())
            await store.SaveBlob(blob.Id, new MemoryStream(PublishedContree.Bytes(16)), TestContext.Current.CancellationToken);
        var recordedRun = JsonNode.Parse(await File.ReadAllTextAsync(Path.Combine(Recording, "run.json")))!;
        var run = OfflineSession.RunOf(game, new RunId(Guid.Parse(recordedRun["id"]!.GetValue<string>())), recordedRun["game_mode"]!.GetValue<string>(),
            [.. recordedRun["game_options"]!.AsArray().Select(option => option!.GetValue<string>())]);
        _session = await OfflineSession.Start(store, game, run);
    }

    public async ValueTask DisposeAsync()
    {
        await _session.DisposeAsync();
        _cache.Delete(recursive: true);
    }

    public static TheoryData<string> RecordedRequests() =>
        new(Directory.EnumerateFiles(Recording, "*.json").Where(path => Path.GetFileName(path) != "run.json").Select(Path.GetFileNameWithoutExtension).Order()!);

    /// @spec backend::serves-bundled-assets
    /// @spec backend::serves-cached-blobs
    /// @spec backend::serves-game-full-options
    /// @spec backend::starts-run-player-inventory
    /// @spec backend::discards-results
    [Theory]
    [MemberData(nameof(RecordedRequests))]
    public async Task Answers_the_recorded_request_like_eternalfest(string recording)
    {
        var recorded = JsonNode.Parse(await File.ReadAllTextAsync(Path.Combine(Recording, $"{recording}.json"), TestContext.Current.CancellationToken))!;
        var method = recorded["method"]!.GetValue<string>();
        var path = recorded["path"]!.GetValue<string>();

        using var response = method == "POST"
            ? await _session.PostForm(path, LoaderFormFor(path))
            : await _session.Get(path);

        Assert.Equal((HttpStatusCode)recorded["status"]!.GetValue<int>(), response.StatusCode);
        Assert.Equal(recorded["headers"]!["Content-Type"]!.GetValue<string>(), response.Content.Headers.ContentType?.MediaType);
        var recordedBody = Path.Combine(Recording, $"{recording}.body");
        if (File.Exists(recordedBody))
            Assert.Equal(
                JsonShape.Of(JsonNode.Parse(await File.ReadAllTextAsync(recordedBody, TestContext.Current.CancellationToken))),
                JsonShape.Of(JsonNode.Parse(await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken))));
    }

    private static (string, string)[] LoaderFormFor(string path) => path.EndsWith("/start", StringComparison.Ordinal)
        ? [("key", "0000")]
        : [("is_victory", "false"), ("max_level", "11"), ("scores", "[0]"), ("items", "{}"), ("stats", "{}")];
}
