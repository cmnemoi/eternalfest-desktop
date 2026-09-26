namespace EternalfestDesktop.Domain;

/// <summary>Who the offline player pretends to be on eternalfest.net.</summary>
public enum PlayerProfile
{
    /// <summary>Every quest of the contrée completed.</summary>
    Complete,

    /// <summary>A brand new account: the contrée as published, and nothing in the inventory.</summary>
    NewPlayer,
}

/// <summary>The offline player of one game, and what they own.</summary>
public sealed record Player(PlayerProfile Profile, Inventory Inventory)
{
    public static readonly Player NewPlayer = new(PlayerProfile.NewPlayer, Inventory.Empty);

    /// <param name="contentItems">The items the contrée's content lists.</param>
    /// @spec profile::complete-by-default
    /// @spec profile::complete-inventory
    public static Player For(PlayerProfile profile, Progression progression, IEnumerable<int> contentItems) =>
        profile == PlayerProfile.NewPlayer
            ? NewPlayer
            : new Player(PlayerProfile.Complete, Inventory.Owning(contentItems.Concat(progression.RequiredItems)));

    /// <summary>The contrée as eternalfest.net would show it to this player.</summary>
    /// @spec profile::new-player-as-published
    /// @spec profile::unlocks-like-eternalfest
    public Game Unlocks(Game game, Progression progression) =>
        Profile == PlayerProfile.NewPlayer ? game : game with { Build = progression.Unlock(game.Build, Inventory) };
}
