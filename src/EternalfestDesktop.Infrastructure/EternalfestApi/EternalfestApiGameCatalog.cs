using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using EternalfestDesktop.Application;
using EternalfestDesktop.Domain;
using Microsoft.Extensions.Logging;

namespace EternalfestDesktop.Infrastructure.EternalfestApi;

/// <summary>Reads the catalog from the public Eternalfest API, anonymously and read-only.</summary>
public sealed partial class EternalfestApiGameCatalog(HttpClient http, ILogger<EternalfestApiGameCatalog> logger) : GameCatalog
{
    private const int PageSize = 50;

    /// @spec catalog::lists-public-contrees
    public async Task<IReadOnlyList<CatalogEntry>> ListPublicGames(CancellationToken cancellationToken)
    {
        var entries = new List<CatalogEntry>();
        for (var offset = 0; ; offset += PageSize)
        {
            var page = await Get<ListingDto<JsonElement>>($"api/v1/games?offset={offset}&limit={PageSize}", cancellationToken);
            entries.AddRange(page.Items.Select(ToCatalogEntry).OfType<CatalogEntry>());
            if (page.Items.Count < PageSize)
                return entries;
        }
    }

    /// @spec catalog::reads-active-build
    public async Task<Game> GetGame(GameId id, CancellationToken cancellationToken)
    {
        using var response = await Send($"api/v1/games/{id}", cancellationToken);
        if (response.StatusCode == HttpStatusCode.NotFound)
            throw new GameNotFoundException(id);
        await EnsureSuccess(response);
        var json = await response.Content.ReadAsStringAsync(cancellationToken);
        var game = JsonSerializer.Deserialize<GameDto>(json, EternalfestJson.Options)
            ?? throw new EternalfestUnreachableException($"Eternalfest returned an empty contrée {id}.");
        return game.ToDomain(new PublishedGameDocument(json));
    }

    private CatalogEntry? ToCatalogEntry(JsonElement item)
    {
        // Contrées hidden since they were listed come back as null
        if (item.ValueKind == JsonValueKind.Null)
            return null;
        try
        {
            return item.Deserialize<ShortGameDto>(EternalfestJson.Options)!.ToDomain();
        }
        catch (Exception exception) when (exception is JsonException or InvalidOperationException or ArgumentException or FormatException or NullReferenceException)
        {
            LogMalformedContree(exception, item.GetRawText());
            return null;
        }
    }

    private async Task<T> Get<T>(string path, CancellationToken cancellationToken)
    {
        using var response = await Send(path, cancellationToken);
        await EnsureSuccess(response);
        return await response.Content.ReadFromJsonAsync<T>(EternalfestJson.Options, cancellationToken)
            ?? throw new EternalfestUnreachableException($"Eternalfest returned an empty answer to {path}.");
    }

    /// @spec catalog::read-only-anonymous
    private async Task<HttpResponseMessage> Send(string path, CancellationToken cancellationToken)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, path);
        request.Headers.UserAgent.ParseAdd(EternalfestJson.UserAgent);
        try
        {
            return await http.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
        }
        catch (HttpRequestException exception)
        {
            throw new EternalfestUnreachableException("Eternalfest can't be reached.", exception);
        }
        catch (TaskCanceledException exception) when (!cancellationToken.IsCancellationRequested)
        {
            throw new EternalfestUnreachableException("Eternalfest took too long to answer.", exception);
        }
    }

    private static async Task EnsureSuccess(HttpResponseMessage response)
    {
        if (!response.IsSuccessStatusCode)
            throw new EternalfestUnreachableException(
                $"Eternalfest answered {(int)response.StatusCode} to {response.RequestMessage?.RequestUri}: {await response.Content.ReadAsStringAsync()}");
    }

    [LoggerMessage(Level = LogLevel.Warning, Message = "Skipping a malformed contrée from the Eternalfest catalog: {Item}")]
    private partial void LogMalformedContree(Exception exception, string item);
}
