namespace EternalfestDesktop.Domain;

/// <summary>A contrée with its active build, as published on Eternalfest.</summary>
public sealed record Game(
    GameId Id,
    string? Key,
    string ChannelKey,
    LocalizedText DisplayName,
    LocalizedText Description,
    GameBuild Build,
    PublishedGameDocument Document);

/// <summary>The contrée exactly as Eternalfest publishes it, which the offline backend serves back to the loader.</summary>
public sealed record PublishedGameDocument(string Json);
