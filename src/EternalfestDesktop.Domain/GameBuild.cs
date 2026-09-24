namespace EternalfestDesktop.Domain;

public sealed record GameBuild(
    string Version,
    string LoaderVersion,
    string MainLocale,
    GameEngine Engine,
    Blob? Patcher,
    Blob? Content,
    Blob? ContentI18n,
    IReadOnlyDictionary<string, Blob> LocalizedContent,
    IReadOnlyList<Music> Musics,
    Blob? Icon,
    IReadOnlyList<GameMode> Modes,
    string Families)
{
    /// <summary>Every file needed to play this build, each listed once.</summary>
    public IReadOnlyList<Blob> Blobs()
    {
        IEnumerable<Blob?> blobs =
        [
            (Engine as CustomEngine)?.Blob,
            Patcher,
            Content,
            ContentI18n,
            .. LocalizedContent.Values,
            .. Musics.Select(music => music.Blob),
            Icon,
        ];
        return blobs.OfType<Blob>().DistinctBy(blob => blob.Id).ToList();
    }

    public long ByteSize() => Blobs().Sum(blob => blob.ByteSize);

    /// @spec play::warns-newer-loader
    public bool RequiresNewerLoaderThan(Version bundledLoader) =>
        System.Version.TryParse(LoaderVersion, out var required) && required > bundledLoader;

    /// <summary>The build as if every progression were done: options the author shows are all enabled.</summary>
    /// @spec backend::serves-game-full-options
    public GameBuild WithFullOptions() => this with
    {
        Modes = Modes.Select(mode => mode with
        {
            Options = mode.Options.Select(option => option.IsVisible ? option with { IsEnabled = true } : option).ToList(),
        }).ToList(),
    };
}

public abstract record GameEngine;

/// <summary>Motion-Twin's latest Hammerfest engine (version 96), bundled with the app.</summary>
public sealed record BaseEngine : GameEngine;

public sealed record CustomEngine(Blob Blob) : GameEngine;

public sealed record Music(string Name, Blob Blob);

public sealed record GameMode(string Key, string DisplayName, bool IsVisible, IReadOnlyList<GameOption> Options);

public sealed record GameOption(string Key, string DisplayName, bool IsVisible, bool IsEnabled, bool DefaultValue);
