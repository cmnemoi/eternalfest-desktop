using EternalfestDesktop.Application;
using EternalfestDesktop.Domain;
using EternalfestDesktop.Infrastructure.FileSystem;
using EternalfestDesktop.Infrastructure.LocalServer;
using Microsoft.Extensions.Logging.Abstractions;

namespace EternalfestDesktop.Tests.Support;

/// <summary>A game session served by the real offline backend, and an HTTP client speaking to it like the loader.</summary>
internal sealed class OfflineSession : IAsyncDisposable
{
    private readonly RunningBackend _backend;

    private OfflineSession(RunningBackend backend)
    {
        _backend = backend;
        Loader = new HttpClient { BaseAddress = backend.Origin };
    }

    public Uri Origin => _backend.Origin;
    public HttpClient Loader { get; }
    public Task<RunResult> GameEnded => _backend.GameEnded;

    public static async Task<OfflineSession> Start(GameStore store, Game game, Run run, Inventory? inventory = null)
    {
        var backend = new KestrelOfflineBackend(store, BundledFlashFiles.NextToApp(), NullLoggerFactory.Instance);
        return new OfflineSession(await backend.Start(game, run, inventory ?? Inventory.Empty, TestContext.Current.CancellationToken));
    }

    public static Run RunOf(Game game, RunId? id = null, string mode = "solo", params string[] options) =>
        new(id ?? RunId.New(), DateTimeOffset.UtcNow, game.Id, game.ChannelKey, mode, options, new RunSettings("fr-FR", Volume: 80));

    public Task<HttpResponseMessage> Get(string path) => Loader.GetAsync(path, TestContext.Current.CancellationToken);

    /// <summary>Posts a form like the Flash 8 loader: every field JSON-encoded, plus <c>flash=true</c>.</summary>
    public Task<HttpResponseMessage> PostForm(string path, params (string Key, string Value)[] fields) =>
        Loader.PostAsync(path, new FormUrlEncodedContent([new("flash", "true"), .. fields.Select(field => new KeyValuePair<string, string>(field.Key, field.Value))]), TestContext.Current.CancellationToken);

    public async ValueTask DisposeAsync()
    {
        Loader.Dispose();
        await _backend.DisposeAsync();
    }
}
