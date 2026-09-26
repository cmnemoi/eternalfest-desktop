// Ports the quests eternalfest.net hardcodes (crates/core/src/inventory/quest_db.rs in the Eternalfest server,
// AGPL-3.0-or-later) to src/EternalfestDesktop.Infrastructure/Quests/quests.json. See ADR 0007.
// Usage: dotnet run eng/port-quests.cs <path to quest_db.rs>
using System.Globalization;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;

var root = Path.GetFullPath(Path.Combine(AppContext.GetData("EntryPointFileDirectoryPath") as string ?? "eng", ".."));
if (args.Length != 1)
{
    Console.Error.WriteLine("Usage: dotnet run eng/port-quests.cs <path to quest_db.rs>");
    return 1;
}

var source = File.ReadAllText(args[0]);
var functions = new Dictionary<string, string>
{
    ["hammerfest"] = "quests_hammerfest",
    ["otherworldly_well"] = "quests_otherwordly_well",
    ["hackfest"] = "quests_hackfest",
};
var kinds = new Dictionary<string, string>
{
    ["GiveFamily"] = "give-family",
    ["RemoveFamily"] = "remove-family",
    ["GiveMode"] = "give-mode",
    ["RemoveMode"] = "remove-mode",
    ["GiveOption"] = "give-option",
    ["RemoveOption"] = "remove-option",
};

var book = new JsonObject();
foreach (var (key, function) in functions)
{
    var body = source[source.IndexOf($"fn {function}()", StringComparison.Ordinal)..];
    body = body[..body.IndexOf("\n}\n", StringComparison.Ordinal)];
    body = Regex.Replace(body, @"/\*.*?\*/", "", RegexOptions.Singleline);
    body = Regex.Replace(body, @"//[^\n]*", "");
    var quests = new JsonArray();
    foreach (Match quest in Regex.Matches(body, @"Quest \{\s*id: (?:None|Some\(id\(""(\d+)""\)\)),\s*require: (vec!\[.*?\]|Vec::new\(\)),\s*rewards: vec!\[(.*?)\],\s*\}", RegexOptions.Singleline))
    {
        var requires = new JsonObject();
        foreach (Match item in Regex.Matches(quest.Groups[2].Value, @"\(id\(""(\d+)""\),\s*(\d+)\)"))
            requires[item.Groups[1].Value] = int.Parse(item.Groups[2].Value, CultureInfo.InvariantCulture);
        var rewards = new JsonArray();
        foreach (Match reward in Regex.Matches(quest.Groups[3].Value, @"(\w+)\(id\(""([^""]+)""\)\)"))
            rewards.Add((JsonNode)new JsonObject { ["kind"] = kinds[reward.Groups[1].Value], ["key"] = reward.Groups[2].Value });
        var node = new JsonObject();
        if (quest.Groups[1].Success)
            node["id"] = int.Parse(quest.Groups[1].Value, CultureInfo.InvariantCulture);
        node["requires"] = requires;
        node["rewards"] = rewards;
        quests.Add((JsonNode)node);
    }

    var declared = Regex.Count(body, @"Quest \{");
    if (quests.Count != declared)
    {
        Console.Error.WriteLine($"Ported {quests.Count} of the {declared} quests of {key}: the source format changed.");
        return 1;
    }
    book[key] = quests;
}

var destination = Path.Combine(root, "src", "EternalfestDesktop.Infrastructure", "Quests", "quests.json");
File.WriteAllText(destination, book.ToJsonString(new JsonSerializerOptions { WriteIndented = true }) + "\n");
Console.WriteLine($"Ported {string.Join(", ", book.Select(entry => $"{entry.Value!.AsArray().Count} quests of {entry.Key}"))} to {destination}.");
return 0;
