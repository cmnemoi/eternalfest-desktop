using System.Globalization;
using System.Text.Json.Nodes;
using EternalfestDesktop.Domain;
using EternalfestDesktop.Infrastructure.EternalfestApi;

namespace EternalfestDesktop.Tests.Support;

/// <summary>"Les Cavernes de Hammerfest" as a logged-in player saw it on eternalfest.net (recorded by hammerfest-tas).</summary>
internal static class RecordedCavernes
{
    /// <summary>The contrée as the player saw it, unlocked by their quests.</summary>
    public static Game AsThePlayer() => EternalfestJson.ParseGame(Fixture.Read("HammerfestTasMirror/eb50d9601341dfe6.body"));

    /// <summary>The contrée as published, before any quest: the build a recorded run carries.</summary>
    public static Game AsPublished()
    {
        var game = Fixture.ReadObject("HammerfestTasMirror/eb50d9601341dfe6.body");
        game["channels"]!["active"]!["build"] = Fixture.ReadObject("HammerfestTasMirror/run.json")["build"]!.DeepClone();
        return EternalfestJson.ParseGame(game.ToJsonString());
    }

    private static JsonObject RunStart() => Fixture.ReadObject("HammerfestTasMirror/2394c13adae79ae3.body");

    /// <summary>The families eternalfest.net started the recorded run with.</summary>
    public static string PlayerFamilies() => RunStart()["families"]!.GetValue<string>();

    /// <summary>
    /// The inventory the recorded run started with, anonymized: only the items of the quests the player completed,
    /// at the quantity those quests require. It completes the same quests as the full inventory.
    /// </summary>
    public static Inventory PlayerInventory() => new(RunStart()["items"]!.AsObject()
        .ToDictionary(item => int.Parse(item.Key, CultureInfo.InvariantCulture), item => item.Value!.GetValue<int>()));
}
