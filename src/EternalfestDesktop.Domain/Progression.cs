using System.Globalization;

namespace EternalfestDesktop.Domain;

/// <summary>The items a player owns, by item id. Eternalfest unlocks families, modes and options from them through quests.</summary>
public sealed record Inventory(IReadOnlyDictionary<int, int> Items)
{
    /// <summary>More than any quest requires.</summary>
    public const int CompleteQuantity = 9999;

    public static readonly Inventory Empty = new(new Dictionary<int, int>());

    public static Inventory Owning(IEnumerable<int> items) =>
        new(items.Distinct().ToDictionary(item => item, _ => CompleteQuantity));

    public int QuantityOf(int item) => Items.GetValueOrDefault(item);
}

/// <summary>A quest eternalfest.net completes for a player owning enough of each required item.</summary>
public sealed record Quest(IReadOnlyDictionary<int, int> Requires, IReadOnlyList<QuestReward> Rewards)
{
    public bool IsCompleteWith(Inventory inventory) =>
        Requires.All(required => inventory.QuantityOf(required.Key) >= required.Value);
}

public abstract record QuestReward;

public sealed record GiveFamily(int Family) : QuestReward;

public sealed record RemoveFamily(int Family) : QuestReward;

public sealed record GiveMode(string Mode) : QuestReward;

public sealed record RemoveMode(string Mode) : QuestReward;

public sealed record GiveOption(string Option) : QuestReward;

public sealed record RemoveOption(string Option) : QuestReward;

/// <summary>The quests of a contrée, in the order eternalfest.net applies them.</summary>
public sealed record Progression(IReadOnlyList<Quest> Quests)
{
    public static readonly Progression None = new([]);

    public IEnumerable<int> RequiredItems => Quests.SelectMany(quest => quest.Requires.Keys).Distinct();

    /// <summary>The build as eternalfest.net shows it to a player owning <paramref name="inventory" />.</summary>
    /// @spec profile::unlocks-like-eternalfest
    public GameBuild Unlock(GameBuild build, Inventory inventory)
    {
        var families = new SortedSet<int>(build.Families
            .Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries)
            .Select(family => int.TryParse(family, CultureInfo.InvariantCulture, out var id) ? id : (int?)null)
            .OfType<int>());
        var modes = build.Modes.ToList();
        foreach (var reward in Quests.Where(quest => quest.IsCompleteWith(inventory)).SelectMany(quest => quest.Rewards))
        {
            switch (reward)
            {
                case GiveFamily give:
                    families.Add(give.Family);
                    break;
                case RemoveFamily remove:
                    families.Remove(remove.Family);
                    break;
                case GiveMode give:
                    modes = modes.Select(mode => mode.Key == give.Mode ? mode with { IsVisible = true } : mode).ToList();
                    break;
                case RemoveMode remove:
                    modes = modes.Select(mode => mode.Key == remove.Mode ? mode with { IsVisible = false } : mode).ToList();
                    break;
                case GiveOption give:
                    modes = WithOption(modes, give.Option, option => option with { IsVisible = true, IsEnabled = true });
                    break;
                case RemoveOption remove:
                    modes = WithOption(modes, remove.Option, option => option with { IsEnabled = false });
                    break;
            }
        }
        return build with { Families = string.Join(',', families), Modes = modes };
    }

    private static List<GameMode> WithOption(List<GameMode> modes, string key, Func<GameOption, GameOption> change) =>
        modes.Select(mode => mode with
        {
            Options = mode.Options.Select(option => option.Key == key ? change(option) : option).ToList(),
        }).ToList();
}
