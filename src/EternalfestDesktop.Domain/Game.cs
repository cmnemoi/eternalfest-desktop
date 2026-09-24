namespace EternalfestDesktop.Domain;

/// <summary>A contrée with its active build, as published on Eternalfest.</summary>
public sealed record Game(
    GameId Id,
    string? Key,
    string ChannelKey,
    LocalizedText DisplayName,
    LocalizedText Description,
    GameBuild Build,
    PublishedGameDocument Document)
{
    /// <summary>Starts an offline game of this contrée, with every visible option available.</summary>
    /// <exception cref="InvalidRunChoiceException">The contrée doesn't offer the chosen mode or options.</exception>
    /// @spec play::valid-mode-and-options
    /// @spec play::creates-local-run
    public Run NewRun(RunChoices choices, DateTimeOffset now)
    {
        var visibleModes = Build.Modes.Where(mode => mode.IsVisible).ToList();
        var mode = choices.Mode is null
            ? visibleModes.FirstOrDefault() ?? throw new InvalidRunChoiceException($"{DisplayName.Default} offers no mode to play.")
            : visibleModes.SingleOrDefault(mode => mode.Key == choices.Mode)
              ?? throw new InvalidRunChoiceException($"{DisplayName.Default} has no mode {choices.Mode}.");
        var available = mode.Options.Where(option => option.IsVisible).ToList();
        var options = choices.Options
            ?? available.Where(option => option.DefaultValue).Select(option => option.Key).ToList();
        var unknown = options.Except(available.Select(option => option.Key)).ToList();
        if (unknown.Count > 0)
            throw new InvalidRunChoiceException($"Mode {mode.Key} of {DisplayName.Default} has no option {string.Join(", ", unknown)}.");
        return new Run(
            RunId.New(),
            now,
            Id,
            ChannelKey,
            mode.Key,
            options,
            new RunSettings(choices.Locale ?? Build.MainLocale, Math.Clamp(choices.Volume, 0, 100)));
    }
}

/// <summary>The contrée exactly as Eternalfest publishes it, which the offline backend serves back to the loader.</summary>
public sealed record PublishedGameDocument(string Json);
