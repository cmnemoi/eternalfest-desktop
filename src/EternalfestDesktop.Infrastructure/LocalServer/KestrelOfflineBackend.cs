using System.Net;
using System.Text.Json.Nodes;
using EternalfestDesktop.Application;
using EternalfestDesktop.Domain;
using EternalfestDesktop.Infrastructure.EternalfestApi;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace EternalfestDesktop.Infrastructure.LocalServer;

/// <summary>
/// Serves the loader, the base engine, a downloaded contrée and its run on <c>http://127.0.0.1:{free port}</c>,
/// answering the loader like eternalfest.net would (see the offline-backend spec and ADR 0003).
/// </summary>
public sealed partial class KestrelOfflineBackend(GameStore store, BundledFlashFiles flash, ILoggerFactory loggerFactory) : OfflineBackend
{
    private const string Flash = "application/x-shockwave-flash";
    private const string Json = "application/json";

    /// <summary>Eternalfest checks run keys online only: any value satisfies the loader.</summary>
    private static readonly string RunKey = Guid.Empty.ToString();

    private readonly ILogger _logger = loggerFactory.CreateLogger<KestrelOfflineBackend>();

    public async Task<RunningBackend> Start(Game game, Run run, Inventory inventory, CancellationToken cancellationToken)
    {
        var builder = WebApplication.CreateSlimBuilder();
        builder.Logging.ClearProviders();
        builder.WebHost.UseKestrel(kestrel => kestrel.Listen(IPAddress.Loopback, 0));
        var app = builder.Build();
        app.Use((context, next) =>
        {
            LogRequest(context.Request.Method, context.Request.Path);
            return next(context);
        });
        Map(app, new Session(game, run, inventory));
        await app.StartAsync(cancellationToken);
        var address = app.Services.GetRequiredService<IServer>().Features.Get<IServerAddressesFeature>()!.Addresses.Single();
        var origin = new Uri(address.Replace("[::1]", "127.0.0.1", StringComparison.Ordinal));
        LogStarted(game.DisplayName.Default, origin);
        return new Running(app, origin);
    }

    private void Map(WebApplication app, Session session)
    {
        // @spec backend::serves-bundled-assets
        app.MapGet("/assets/loader.swf", () => Results.Stream(flash.OpenLoader(), Flash));
        app.MapGet("/assets/game.swf", () => Results.Stream(flash.OpenBaseEngine(), Flash));

        // @spec backend::serves-cached-blobs
        app.MapGet("/api/v1/blobs/{id:guid}/raw", (Guid id) =>
            session.Blobs.TryGetValue(new BlobId(id), out var blob) && store.HasBlob(blob.Id)
                ? Results.Stream(store.OpenBlob(blob.Id), blob.MediaType)
                : NotFound($"blob {id}"));

        // @spec backend::serves-game-full-options
        app.MapGet("/api/v1/games/{id}", (string id) =>
            session.IsGame(id) ? Results.Text(session.UnlockedGame.ToJsonString(), Json) : NotFound($"contrée {id}"));

        app.MapGet("/api/v1/runs/{id:guid}", (Guid id) =>
            session.IsRun(id) ? Results.Text(session.RunDocument().ToJsonString(), Json) : NotFound($"run {id}"));

        // @spec backend::starts-run-player-inventory
        app.MapPost("/api/v1/runs/{id:guid}/start", (Guid id) =>
        {
            if (!session.IsRun(id))
                return NotFound($"run {id}");
            session.StartedAt = DateTimeOffset.UtcNow;
            return Results.Text(new JsonObject
            {
                ["run"] = new JsonObject { ["type"] = "Run", ["id"] = session.Run.Id.ToString() },
                ["key"] = RunKey,
                ["families"] = session.Game.Build.Families,
                ["items"] = EternalfestDocuments.Items(session.Inventory),
            }.ToJsonString(), Json);
        });

        // @spec backend::discards-results
        app.MapPost("/api/v1/runs/{id:guid}/result", async (Guid id, HttpRequest request) =>
        {
            if (!session.IsRun(id))
                return NotFound($"run {id}");
            var result = ReadResult(await request.ReadFormAsync());
            // A game that ends has started, even if the loader skipped telling us
            session.StartedAt ??= DateTimeOffset.UtcNow;
            LogResult(session.Game.DisplayName.Default, result);
            return Results.Text(session.RunDocument(result).ToJsonString(), Json);
        });

        // @spec backend::rejects-unknown-routes
        app.MapFallback((HttpRequest request) => NotFound($"{request.Method} {request.Path}"));
    }

    /// <summary>The Flash 8 loader JSON-encodes every field of its form.</summary>
    private static JsonObject ReadResult(IFormCollection form)
    {
        var result = new JsonObject { ["created_at"] = EternalfestDocuments.Timestamp(DateTimeOffset.UtcNow) };
        foreach (var field in new[] { "is_victory", "max_level", "scores", "items", "stats" })
            result[field] = form.TryGetValue(field, out var value) ? JsonNode.Parse(value.ToString()) : null;
        return result;
    }

    private IResult NotFound(string what)
    {
        LogNotFound(what);
        return Results.Text("""{"error":"ResourceNotFound"}""", Json, statusCode: StatusCodes.Status404NotFound);
    }

    [LoggerMessage(Level = LogLevel.Information, Message = "Offline backend serving {Contree} on {Origin}")]
    private partial void LogStarted(string contree, Uri origin);

    [LoggerMessage(Level = LogLevel.Information, Message = "{Contree} ended, result discarded: {Result}")]
    private partial void LogResult(string contree, JsonObject result);

    [LoggerMessage(Level = LogLevel.Information, Message = "Loader requested {Method} {Path}")]
    private partial void LogRequest(string method, PathString path);

    [LoggerMessage(Level = LogLevel.Warning, Message = "Offline backend doesn't know {What}")]
    private partial void LogNotFound(string what);

    private sealed class Session(Game game, Run run, Inventory inventory)
    {
        public Game Game { get; } = game;
        public Run Run { get; } = run;
        public Inventory Inventory { get; } = inventory;
        public JsonObject UnlockedGame { get; } = EternalfestDocuments.UnlockedGame(game);
        public Dictionary<BlobId, Blob> Blobs { get; } = game.Build.Blobs().ToDictionary(blob => blob.Id);
        public DateTimeOffset? StartedAt { get; set; }

        public bool IsGame(string idOrKey) =>
            idOrKey == Game.Id.ToString() || (Game.Key is not null && idOrKey == Game.Key);

        public bool IsRun(Guid id) => id == Run.Id.Value;

        public JsonObject RunDocument(JsonObject? result = null) =>
            EternalfestDocuments.Run(Run, UnlockedGame, StartedAt, result);
    }

    private sealed class Running(WebApplication app, Uri origin) : RunningBackend
    {
        public Uri Origin { get; } = origin;

        public async ValueTask DisposeAsync()
        {
            await app.StopAsync();
            await app.DisposeAsync();
        }
    }
}
