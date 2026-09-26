using System.Text.Json.Nodes;
using EternalfestDesktop.Application;
using EternalfestDesktop.Infrastructure.EternalfestApi;

namespace EternalfestDesktop.Infrastructure.LocalServer;

/// <summary>The FlashVars the Eternalfest website gives the loader, for a game served by the offline backend.</summary>
public static class LoaderFlashVars
{
    public static IReadOnlyList<KeyValuePair<string, string>> For(FlashGame game)
    {
        var unlockedGame = EternalfestDocuments.UnlockedGame(game.Game);
        var settings = game.Run.Settings;
        var options = new JsonObject
        {
            ["mode"] = game.Run.Mode,
            ["options"] = new JsonArray(game.Run.Options.Select(option => (JsonNode)option).ToArray()),
            ["settings"] = new JsonObject
            {
                ["detail"] = settings.Detail,
                ["shake"] = settings.Shake,
                ["sound"] = settings.Sound,
                ["music"] = settings.Music,
                ["volume"] = settings.Volume,
                ["locale"] = settings.Locale,
            },
            ["locale"] = settings.Locale,
        };
        return
        [
            new("object_id", "swf1234"),
            new("run", EternalfestDocuments.Run(game.Run, unlockedGame).ToJsonString()),
            new("game", game.Game.Id.ToString()),
            new("options", options.ToJsonString()),
        ];
    }
}
