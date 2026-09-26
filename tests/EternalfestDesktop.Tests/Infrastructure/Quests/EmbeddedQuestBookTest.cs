using EternalfestDesktop.Domain;
using EternalfestDesktop.Infrastructure.Quests;
using EternalfestDesktop.Tests.Domain;
using EternalfestDesktop.Tests.Support;

namespace EternalfestDesktop.Tests.Infrastructure.Quests;

public sealed class EmbeddedQuestBookTest
{
    private readonly EmbeddedQuestBook _book = new();

    /// @spec profile::bundled-quests
    [Theory]
    [InlineData("hammerfest", 80)]
    [InlineData("otherworldly_well", 3)]
    [InlineData("hackfest", 3)]
    public void Knows_the_quests_eternalfest_hardcodes(string key, int quests)
    {
        Assert.Equal(quests, _book.ProgressionOf(key).Quests.Count);
    }

    /// @spec profile::bundled-quests
    [Fact]
    public void Lists_each_contree_once()
    {
        Assert.Equal(["hackfest", "hammerfest", "otherworldly_well"], _book.Keys.Order(StringComparer.Ordinal));
    }

    /// @spec profile::bundled-quests
    [Theory]
    [InlineData("accumulation")]
    [InlineData(null)]
    public void Knows_no_quest_for_other_contrees(string? key)
    {
        Assert.Empty(_book.ProgressionOf(key).Quests);
    }

    /// @spec profile::unlocks-like-eternalfest
    [Fact]
    public void Unlocks_the_families_eternalfest_gave_a_recorded_player()
    {
        var unlocked = _book.ProgressionOf("hammerfest").Unlock(RecordedCavernes.AsPublished().Build, RecordedCavernes.PlayerInventory());

        Assert.Equal(RecordedCavernes.PlayerFamilies(), unlocked.Families);
    }

    /// @spec profile::unlocks-like-eternalfest
    [Fact]
    public void Unlocks_the_modes_and_options_eternalfest_showed_a_recorded_player()
    {
        var unlocked = _book.ProgressionOf("hammerfest").Unlock(RecordedCavernes.AsPublished().Build, RecordedCavernes.PlayerInventory());

        Assert.Equal(Visibility(RecordedCavernes.AsThePlayer().Build), Visibility(unlocked));
    }

    /// @spec profile::unlocks-like-eternalfest
    /// @spec profile::complete-inventory
    [Fact]
    public void Gives_the_complete_profile_every_life_and_bomb_of_the_cavernes()
    {
        var cavernes = RecordedCavernes.AsPublished();
        var progression = _book.ProgressionOf(cavernes.Key);

        var unlocked = Player.For(PlayerProfile.Complete, progression, contentItems: []).Unlocks(cavernes, progression).Build;

        var families = unlocked.Families.Split(',').Select(int.Parse).ToList();
        Assert.Superset(new HashSet<int> { 100, 102, 103, 104, 105, 108 }, families.ToHashSet());
        Assert.Empty(families.Intersect([5, 6, 7, 8, 9, 13, 14, 15, 16, 17, 18, 1021, 1022, 1028]));
        Assert.Contains(unlocked.Modes.Single(mode => mode.Key == "solo").Options, option => option is { Key: "insight", IsVisible: true });
        Assert.True(unlocked.Modes.Single(mode => mode.Key == "timeattack").IsVisible);
    }

    /// @spec profile::new-player-as-published
    [Fact]
    public void Gives_a_new_player_nothing_of_the_cavernes()
    {
        var cavernes = RecordedCavernes.AsPublished();
        var progression = _book.ProgressionOf(cavernes.Key);
        var player = Player.For(PlayerProfile.NewPlayer, progression, contentItems: [0, 102, 1000]);

        var unlocked = player.Unlocks(cavernes, progression).Build;

        Assert.Equal("0,7,13,15,18,1000,1028,5015", unlocked.Families);
        Assert.Empty(player.Inventory.Items);
        Assert.Contains(unlocked.Modes.Single(mode => mode.Key == "solo").Options, option => option is { Key: "insight", IsVisible: false });
    }

    /// @spec profile::unlocks-like-eternalfest
    [Fact]
    public void Gives_back_the_nightmare_option_of_the_otherworldly_well()
    {
        var progression = _book.ProgressionOf("otherworldly_well");
        var build = ProgressionTest.Build("0", ProgressionTest.Mode("solo", visible: true, ProgressionTest.Option("nightmare", visible: true, enabled: true)));

        var unlocked = progression.Unlock(build, Player.For(PlayerProfile.Complete, progression, contentItems: []).Inventory);

        Assert.Contains(unlocked.Modes.Single().Options, option => option is { Key: "nightmare", IsVisible: true, IsEnabled: true });
    }

    private static List<string> Visibility(GameBuild build) =>
    [
        .. build.Modes.Select(mode => $"{mode.Key} visible={mode.IsVisible}"),
        .. build.Modes.SelectMany(mode => mode.Options.Select(option => $"{mode.Key}/{option.Key} visible={option.IsVisible} enabled={option.IsEnabled}")),
    ];
}
