using System.Text.Json;
using EternalfestDesktop.Domain;

namespace EternalfestDesktop.Infrastructure.EternalfestApi;

internal static class EternalfestJson
{
    public static readonly JsonSerializerOptions Options = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
        RespectNullableAnnotations = true,
        RespectRequiredConstructorParameters = true,
    };

    /// <summary>Reads a contrée from the document Eternalfest publishes for it.</summary>
    /// <exception cref="JsonException" />
    /// <exception cref="InvalidOperationException" />
    public static Game ParseGame(string json) =>
        (JsonSerializer.Deserialize<GameDto>(json, Options) ?? throw new JsonException("The contrée document is empty."))
            .ToDomain(new PublishedGameDocument(json));
}
