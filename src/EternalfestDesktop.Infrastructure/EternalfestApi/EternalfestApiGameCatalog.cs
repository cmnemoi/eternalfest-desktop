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
        using var response = await http.Get($"api/v1/games/{id}", cancellationToken);
        if (response.StatusCode == HttpStatusCode.NotFound)
            throw new GameNotFoundException(id);
        await response.EnsureSuccess(cancellationToken);
        return EternalfestJson.ParseGame(await response.Content.ReadAsStringAsync(cancellationToken));
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
        using var response = await http.Get(path, cancellationToken);
        await response.EnsureSuccess(cancellationToken);
        return await response.Content.ReadFromJsonAsync<T>(EternalfestJson.Options, cancellationToken)
            ?? throw new EternalfestUnreachableException($"Eternalfest returned an empty answer to {path}.");
    }

    [LoggerMessage(Level = LogLevel.Warning, Message = "Skipping a malformed contrée from the Eternalfest catalog: {Item}")]
    private partial void LogMalformedContree(Exception exception, string item);
}
