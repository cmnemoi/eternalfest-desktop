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

    /// <summary>
    /// The published contrée, with the families, modes and options of its build as unlocked for the player,
    /// and every visible option enabled.
    /// </summary>
    /// @spec backend::serves-game-full-options
    public static JsonObject UnlockedGame(Game unlocked)
    {
        var build = unlocked.Build.WithFullOptions();
        var game = JsonNode.Parse(unlocked.Document.Json)!.AsObject();
        if (game["channels"]?["active"]?["build"] is not JsonObject published)
            return game;
        published["families"] = build.Families;
        var modes = published["modes"]?.AsObject() ?? [];
        foreach (var mode in build.Modes)
        {
            if (modes[mode.Key] is not JsonObject publishedMode)
                continue;
            publishedMode["is_visible"] = mode.IsVisible;
            foreach (var option in mode.Options)
                if (publishedMode["options"]?[option.Key] is JsonObject publishedOption)
                {
                    publishedOption["is_visible"] = option.IsVisible;
                    publishedOption["is_enabled"] = option.IsEnabled;
                }
        }
        return game;
    }

    /// <summary>An inventory as eternalfest.net sends it to the loader: item id to quantity, by increasing id.</summary>
    public static JsonObject Items(Inventory inventory)
    {
        var items = new JsonObject();
        foreach (var (item, quantity) in inventory.Items.OrderBy(owned => owned.Key))
            items[item.ToString(CultureInfo.InvariantCulture)] = quantity;
        return items;
    }

    public static JsonObject Run(Run run, JsonObject unlockedGame, DateTimeOffset? startedAt = null, JsonObject? result = null) => new()
    {
        ["type"] = "Run",
        ["id"] = run.Id.ToString(),
        ["created_at"] = Timestamp(run.CreatedAt),
        ["started_at"] = startedAt is { } started ? Timestamp(started) : null,
        ["result"] = result,
        ["game"] = new JsonObject { ["type"] = "Game", ["id"] = run.GameId.ToString() },
        ["channel"] = new JsonObject { ["type"] = "GameChannel", ["key"] = run.ChannelKey },
        ["build"] = unlockedGame["channels"]!["active"]!["build"]!.DeepClone(),
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
