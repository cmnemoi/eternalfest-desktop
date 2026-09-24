namespace EternalfestDesktop.Domain;

/// <summary>What the player picks before playing. Anything left out takes the contrée's default.</summary>
public sealed record RunChoices(
    string? Mode = null,
    IReadOnlyList<string>? Options = null,
    string? Locale = null,
    int Volume = 100,
    bool Fullscreen = false);

public sealed class InvalidRunChoiceException(string message) : Exception(message);
