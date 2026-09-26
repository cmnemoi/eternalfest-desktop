using EternalfestDesktop.Domain;

namespace EternalfestDesktop.Tests.Domain;

public sealed class ProgressionTest
{
    /// @spec profile::unlocks-like-eternalfest
    [Theory]
    [InlineData(150, "0,1007")]
    [InlineData(149, "0")]
    public void Completes_a_quest_once_the_inventory_holds_the_required_quantity(int owned, string families)
    {
        var progression = Quests(Quest(Requires(1051, 150), new GiveFamily(1007)));

        var unlocked = progression.Unlock(Build("0"), Owning((1051, owned)));

        Assert.Equal(families, unlocked.Families);
    }

    /// @spec profile::unlocks-like-eternalfest
    [Fact]
    public void Needs_every_required_item_to_complete_a_quest()
    {
        var progression = Quests(Quest(Requires((4, 1), (7, 1)), new GiveFamily(1)));

        var unlocked = progression.Unlock(Build("0"), Owning((4, 9999)));

        Assert.Equal("0", unlocked.Families);
    }

    /// @spec profile::unlocks-like-eternalfest
    [Fact]
    public void Always_completes_a_quest_requiring_nothing()
    {
        var progression = Quests(Quest(Requires(), new RemoveFamily(7)));

        var unlocked = progression.Unlock(Build("0,7"), Inventory.Empty);

        Assert.Equal("0", unlocked.Families);
    }

    /// @spec profile::unlocks-like-eternalfest
    [Fact]
    public void Lists_a_family_given_by_two_quests_once()
    {
        var progression = Quests(
            Quest(Requires(102, 1), new GiveFamily(108)),
            Quest(Requires(103, 1), new GiveFamily(108)));

        var unlocked = progression.Unlock(Build("0"), Owning((102, 1), (103, 1)));

        Assert.Equal("0,108", unlocked.Families);
    }

    /// @spec profile::unlocks-like-eternalfest
    [Fact]
    public void Applies_quests_in_their_order()
    {
        var progression = Quests(
            Quest(Requires(1209, 20), new GiveFamily(1021)),
            Quest(Requires(1193, 1), new GiveFamily(5003), new RemoveFamily(1021)));

        var unlocked = progression.Unlock(Build("0"), Owning((1209, 20), (1193, 1)));

        Assert.Equal("0,5003", unlocked.Families);
    }

    /// @spec profile::unlocks-like-eternalfest
    [Theory]
    [InlineData("1000,2,0,2", "0,2,1000")]
    [InlineData("", "")]
    public void Lists_families_in_increasing_order_once_each(string published, string unlocked)
    {
        Assert.Equal(unlocked, Progression.None.Unlock(Build(published), Inventory.Empty).Families);
    }

    /// @spec profile::unlocks-like-eternalfest
    [Fact]
    public void Shows_and_hides_modes()
    {
        var progression = Quests(Quest(Requires(1236, 100), new GiveMode("timeattack"), new RemoveMode("solo")));

        var unlocked = progression.Unlock(
            Build("0", Mode("solo", visible: true), Mode("timeattack", visible: false)),
            Owning((1236, 100)));

        Assert.Equal([("solo", false), ("timeattack", true)], unlocked.Modes.Select(mode => (mode.Key, mode.IsVisible)));
    }

    /// @spec profile::unlocks-like-eternalfest
    [Fact]
    public void Gives_an_option_in_every_mode_that_has_it()
    {
        var progression = Quests(Quest(Requires(102, 1), new GiveOption("insight")));

        var unlocked = progression.Unlock(
            Build("0",
                Mode("solo", visible: true, Option("mirror", visible: true, enabled: false), Option("insight", visible: false, enabled: false)),
                Mode("deluxe", visible: false, Option("insight", visible: true, enabled: false))),
            Owning((102, 1)));

        Assert.Equal(
            [("solo", "mirror", true, false), ("solo", "insight", true, true), ("deluxe", "insight", true, true)],
            Options(unlocked));
    }

    /// @spec profile::unlocks-like-eternalfest
    [Fact]
    public void Disables_an_option_without_hiding_it()
    {
        var progression = Quests(Quest(Requires(), new RemoveOption("nightmare")));

        var unlocked = progression.Unlock(Build("0", Mode("solo", visible: true, Option("nightmare", visible: true, enabled: true))), Inventory.Empty);

        Assert.Equal([("solo", "nightmare", true, false)], Options(unlocked));
    }

    /// @spec profile::unlocks-like-eternalfest
    [Fact]
    public void Ignores_rewards_naming_what_the_build_doesnt_have()
    {
        var progression = Quests(Quest(Requires(), new GiveMode("multicoop"), new RemoveMode("bossrush"), new GiveOption("insight"), new RemoveOption("boost")));
        var build = Build("0", Mode("solo", visible: true, Option("mirror", visible: true, enabled: false)));

        var unlocked = progression.Unlock(build, Inventory.Empty);

        Assert.Equal(["solo"], unlocked.Modes.Select(mode => mode.Key));
        Assert.Equal(Options(build), Options(unlocked));
    }

    /// @spec profile::complete-inventory
    [Fact]
    public void Lists_each_item_its_quests_require_once()
    {
        var progression = Quests(
            Quest(Requires((102, 1), (1219, 1)), new GiveMode("deluxemulti")),
            Quest(Requires((102, 1), (1236, 100)), new GiveMode("multitime")));

        Assert.Equal([102, 1219, 1236], progression.RequiredItems.Order());
    }

    internal static Progression Quests(params Quest[] quests) => new(quests);

    internal static Quest Quest(IReadOnlyDictionary<int, int> requires, params QuestReward[] rewards) => new(requires, rewards);

    internal static Dictionary<int, int> Requires(int item, int quantity) => new() { [item] = quantity };

    internal static Dictionary<int, int> Requires(params (int Item, int Quantity)[] items) =>
        items.ToDictionary(item => item.Item, item => item.Quantity);

    internal static Inventory Owning(params (int Item, int Quantity)[] items) => new(Requires(items));

    internal static GameBuild Build(string families, params GameMode[] modes) =>
        new("1.0.0", "5.1.2", "fr-FR", new BaseEngine(), null, null, null, new Dictionary<string, Blob>(), [], null, modes, families);

    internal static GameMode Mode(string key, bool visible, params GameOption[] options) => new(key, key, visible, options);

    internal static GameOption Option(string key, bool visible, bool enabled) => new(key, key, visible, enabled, DefaultValue: false);

    private static List<(string Mode, string Option, bool Visible, bool Enabled)> Options(GameBuild build) =>
        build.Modes.SelectMany(mode => mode.Options.Select(option => (mode.Key, option.Key, option.IsVisible, option.IsEnabled))).ToList();
}
