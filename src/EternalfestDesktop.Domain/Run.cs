namespace EternalfestDesktop.Domain;

/// <summary>One offline game of a contrée. It lives only on this computer and is never sent to Eternalfest.</summary>
public sealed record Run(
    RunId Id,
    DateTimeOffset CreatedAt,
    GameId GameId,
    string ChannelKey,
    string Mode,
    IReadOnlyList<string> Options,
    RunSettings Settings);

/// <summary>The in-game settings Eternalfest gives the loader.</summary>
public sealed record RunSettings(
    string Locale,
    int Volume,
    bool Detail = true,
    bool Shake = true,
    bool Sound = true,
    bool Music = true);
