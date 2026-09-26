using System.Globalization;
using System.Text.Json;
using EternalfestDesktop.Application;
using EternalfestDesktop.Domain;

namespace EternalfestDesktop.Infrastructure.Quests;

/// <summary>
/// The quests eternalfest.net hardcodes, ported from its server by <c>eng/port-quests.cs</c>
/// and bundled as <c>quests.json</c> (see ADR 0007).
/// </summary>
/// @spec profile::bundled-quests
public sealed class EmbeddedQuestBook : QuestBook
{
    private readonly Dictionary<string, Progression> _progressions;

    /// <exception cref="InvalidOperationException">The bundled quests can't be read, or list a contrée twice.</exception>
    public EmbeddedQuestBook()
    {
        using var resource = typeof(EmbeddedQuestBook).Assembly.GetManifestResourceStream("EternalfestDesktop.Infrastructure.Quests.quests.json")
            ?? throw new InvalidOperationException("quests.json isn't bundled.");
        using var book = JsonDocument.Parse(resource);
        _progressions = [];
        foreach (var contree in book.RootElement.EnumerateObject())
            if (!_progressions.TryAdd(contree.Name, new Progression(contree.Value.EnumerateArray().Select(ReadQuest).ToList())))
                throw new InvalidOperationException($"quests.json lists {contree.Name} twice.");
    }

    public IEnumerable<string> Keys => _progressions.Keys;

    public Progression ProgressionOf(string? gameKey) =>
        gameKey is not null && _progressions.TryGetValue(gameKey, out var progression) ? progression : Progression.None;

    private static Quest ReadQuest(JsonElement quest) => new(
        quest.GetProperty("requires").EnumerateObject().ToDictionary(item => int.Parse(item.Name, CultureInfo.InvariantCulture), item => item.Value.GetInt32()),
        quest.GetProperty("rewards").EnumerateArray().Select(ReadReward).ToList());

    private static QuestReward ReadReward(JsonElement reward)
    {
        var key = reward.GetProperty("key").GetString()!;
        return reward.GetProperty("kind").GetString() switch
        {
            "give-family" => new GiveFamily(int.Parse(key, CultureInfo.InvariantCulture)),
            "remove-family" => new RemoveFamily(int.Parse(key, CultureInfo.InvariantCulture)),
            "give-mode" => new GiveMode(key),
            "remove-mode" => new RemoveMode(key),
            "give-option" => new GiveOption(key),
            "remove-option" => new RemoveOption(key),
            var kind => throw new InvalidOperationException($"quests.json has an unknown reward {kind}."),
        };
    }
}
