using EternalfestDesktop.Domain;
using static EternalfestDesktop.Tests.Domain.ProgressionTest;

namespace EternalfestDesktop.Tests.Domain;

public sealed class PlayerTest
{
    private static readonly Progression CarrotQuest = Quests(
        Quest(Requires(), new RemoveFamily(7)),
        Quest(Requires(102, 1), new GiveFamily(108), new GiveOption("insight")));

    /// @spec profile::complete-by-default
    [Fact]
    public void Plays_the_complete_profile_unless_told_otherwise()
    {
        Assert.Equal(PlayerProfile.Complete, new RunChoices().Profile);
    }

    /// @spec profile::complete-by-default
    [Fact]
    public void Plays_a_profile_it_doesnt_know_as_the_complete_profile()
    {
        var player = Player.For((PlayerProfile)42, CarrotQuest, contentItems: []);

        Assert.Equal(PlayerProfile.Complete, player.Profile);
        Assert.Equal(9999, player.Inventory.QuantityOf(102));
    }

    /// @spec profile::new-player-as-published
    [Fact]
    public void Gives_a_new_player_the_contree_as_published()
    {
        var game = Contree(Build("0,7,1000", Mode("solo", visible: true, Option("insight", visible: false, enabled: false))));

        var player = Player.For(PlayerProfile.NewPlayer, CarrotQuest, contentItems: [102, 1000]);

        Assert.Empty(player.Inventory.Items);
        Assert.Same(game, player.Unlocks(game, CarrotQuest));
    }

    /// @spec profile::complete-inventory
    [Fact]
    public void Gives_the_complete_profile_9999_of_every_item_of_the_content_and_the_quests()
    {
        var player = Player.For(PlayerProfile.Complete, CarrotQuest, contentItems: [0, 1000, 102, 1000]);

        Assert.Equal(new Dictionary<int, int> { [0] = 9999, [102] = 9999, [1000] = 9999 }, player.Inventory.Items);
    }

    /// @spec profile::complete-inventory
    [Fact]
    public void Gives_an_empty_inventory_for_a_contree_without_items_nor_quests()
    {
        Assert.Empty(Player.For(PlayerProfile.Complete, Progression.None, contentItems: []).Inventory.Items);
    }

    /// @spec profile::unlocks-like-eternalfest
    [Fact]
    public void Unlocks_every_quest_for_the_complete_profile()
    {
        var game = Contree(Build("0,7,1000", Mode("solo", visible: true, Option("insight", visible: false, enabled: false))));
        var player = Player.For(PlayerProfile.Complete, CarrotQuest, contentItems: []);

        var unlocked = player.Unlocks(game, CarrotQuest);

        Assert.Equal("0,108,1000", unlocked.Build.Families);
        Assert.True(unlocked.Build.Modes.Single().Options.Single().IsVisible);
    }

    /// @spec profile::unlocks-like-eternalfest
    [Fact]
    public void Keeps_the_build_families_in_order_for_a_contree_without_quests()
    {
        var game = Contree(Build("1000,0,1"));
        var player = Player.For(PlayerProfile.Complete, Progression.None, contentItems: [1000]);

        Assert.Equal("0,1,1000", player.Unlocks(game, Progression.None).Build.Families);
    }

    /// @spec play::valid-mode-and-options
    [Theory]
    [InlineData(PlayerProfile.Complete, true)]
    [InlineData(PlayerProfile.NewPlayer, false)]
    public void Offers_the_options_the_profile_unlocks(PlayerProfile profile, bool offered)
    {
        var game = Contree(Build("0", Mode("solo", visible: true, Option("insight", visible: false, enabled: false))));
        var unlocked = Player.For(profile, CarrotQuest, contentItems: []).Unlocks(game, CarrotQuest);

        var play = () => unlocked.NewRun(new RunChoices("solo", ["insight"], Profile: profile), DateTimeOffset.UnixEpoch);

        if (offered)
            Assert.Equal(["insight"], play().Options);
        else
            Assert.Throws<InvalidRunChoiceException>(play);
    }

    private static Game Contree(GameBuild build) =>
        new(new GameId(Guid.NewGuid()), "hammerfest", "main", LocalizedText.Untranslated("Les Cavernes de Hammerfest"), LocalizedText.Untranslated(""), build, new PublishedGameDocument("{}"));
}
