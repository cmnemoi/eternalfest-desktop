using System.Globalization;
using System.Text.Json.Nodes;
using EternalfestDesktop.Domain;

namespace EternalfestDesktop.Infrastructure.EternalfestApi;

/// <summary>Documents shaped like the ones eternalfest.net gives the loader.</summary>
internal static class EternalfestDocuments
{
    /// <summary>A user standing for the offline player: runs need one, and it matches no Eternalfest account.</summary>
    private static readonly JsonObject OfflinePlayer = new()
    {
        ["type"] = "User",
        ["id"] = Guid.Empty.ToString(),
        ["display_name"] = "Offline player",
    };

    /// <summary>The published contrée, with every option its author shows enabled.</summary>
    /// @spec backend::serves-game-full-options
    public static JsonObject FullOptionsGame(PublishedGameDocument document)
    {
        var game = JsonNode.Parse(document.Json)!.AsObject();
        var modes = game["channels"]?["active"]?["build"]?["modes"]?.AsObject() ?? [];
        foreach (var (_, mode) in modes)
            foreach (var (_, option) in mode?["options"]?.AsObject() ?? [])
                if (option?["is_visible"]?.GetValue<bool>() == true)
                    option["is_enabled"] = true;
        return game;
    }

    public static JsonObject Run(Run run, JsonObject fullOptionsGame, DateTimeOffset? startedAt = null, JsonObject? result = null) => new()
    {
        ["type"] = "Run",
        ["id"] = run.Id.ToString(),
        ["created_at"] = Timestamp(run.CreatedAt),
        ["started_at"] = startedAt is { } started ? Timestamp(started) : null,
        ["result"] = result,
        ["game"] = new JsonObject { ["type"] = "Game", ["id"] = run.GameId.ToString() },
        ["channel"] = new JsonObject { ["type"] = "GameChannel", ["key"] = run.ChannelKey },
        ["build"] = fullOptionsGame["channels"]!["active"]!["build"]!.DeepClone(),
        ["user"] = OfflinePlayer.DeepClone(),
        ["game_mode"] = run.Mode,
        ["game_options"] = new JsonArray(run.Options.Select(option => (JsonNode)option).ToArray()),
        ["settings"] = new JsonObject
        {
            ["detail"] = run.Settings.Detail,
            ["shake"] = run.Settings.Shake,
            ["sound"] = run.Settings.Sound,
            ["music"] = run.Settings.Music,
            ["volume"] = run.Settings.Volume,
            ["locale"] = run.Settings.Locale,
        },
    };

    public static string Timestamp(DateTimeOffset instant) =>
        instant.UtcDateTime.ToString("yyyy-MM-dd'T'HH:mm:ss.fff'Z'", CultureInfo.InvariantCulture);
}
