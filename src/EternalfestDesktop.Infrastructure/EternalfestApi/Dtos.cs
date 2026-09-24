using System.Text.Json.Serialization;
using EternalfestDesktop.Domain;

namespace EternalfestDesktop.Infrastructure.EternalfestApi;

// Shapes of the public Eternalfest API (eternalfest/crates/core/src/game.rs), limited to what the launcher reads.
// Nullable parameters are optional: the API omits them (RespectRequiredConstructorParameters requires the others).

internal sealed record ListingDto<T>(int Offset, int Limit, IReadOnlyList<T> Items);

internal sealed record BlobDto(Guid Id, string MediaType, long ByteSize, DigestDto Digest)
{
    public Blob ToDomain() => new(new BlobId(Id), MediaType, ByteSize, Digest.Sha2256);
}

internal sealed record DigestDto([property: JsonPropertyName("sha2_256")] string Sha2256);

internal sealed record ShortGameDto(Guid Id, ListingDto<ShortChannelDto> Channels, string? Key = null)
{
    public CatalogEntry ToDomain()
    {
        var build = Channels.Items[0].Build;
        return new CatalogEntry(
            new GameId(Id),
            Key,
            build.Version,
            Localized(build.DisplayName, build.I18n, i18n => i18n.DisplayName),
            Localized(build.Description, build.I18n, i18n => i18n.Description),
            build.Icon?.ToDomain());
    }

    internal static LocalizedText Localized<TI18n>(string text, IReadOnlyDictionary<string, TI18n>? i18n, Func<TI18n, string?> select) =>
        new(text, (i18n ?? new Dictionary<string, TI18n>())
            .Select(pair => (pair.Key, Value: select(pair.Value)))
            .Where(pair => pair.Value is not null)
            .ToDictionary(pair => pair.Key, pair => pair.Value!));
}

internal sealed record ShortChannelDto(ShortBuildDto Build);

internal sealed record ShortBuildDto(
    string Version,
    string DisplayName,
    string Description,
    BlobDto? Icon = null,
    IReadOnlyDictionary<string, TextI18nDto>? I18n = null);

internal sealed record TextI18nDto(string? DisplayName = null, string? Description = null);

internal sealed record GameDto(Guid Id, ActiveChannelsDto Channels, string? Key = null)
{
    public Game ToDomain(PublishedGameDocument document)
    {
        var channel = Channels.Active;
        var build = channel.Build;
        return new Game(
            new GameId(Id),
            Key,
            channel.Key,
            ShortGameDto.Localized(build.DisplayName, build.I18n, i18n => i18n.DisplayName),
            ShortGameDto.Localized(build.Description, build.I18n, i18n => i18n.Description),
            build.ToDomain(),
            document);
    }
}

internal sealed record ActiveChannelsDto(ActiveChannelDto Active);

internal sealed record ActiveChannelDto(string Key, BuildDto Build);

internal sealed record BuildDto(
    string Version,
    string Loader,
    string MainLocale,
    string DisplayName,
    string Description,
    EngineDto Engine,
    BlobDto? Icon = null,
    PatcherDto? Patcher = null,
    BlobDto? Content = null,
    BlobDto? ContentI18n = null,
    IReadOnlyList<MusicDto>? Musics = null,
    IReadOnlyDictionary<string, ModeDto>? Modes = null,
    string? Families = null,
    IReadOnlyDictionary<string, BuildI18nDto>? I18n = null)
{
    public GameBuild ToDomain() => new(
        Version,
        Loader,
        MainLocale,
        Engine.ToDomain(),
        Patcher?.Blob.ToDomain(),
        Content?.ToDomain(),
        ContentI18n?.ToDomain(),
        (I18n ?? new Dictionary<string, BuildI18nDto>())
            .Where(pair => pair.Value.ContentI18n is not null)
            .ToDictionary(pair => pair.Key, pair => pair.Value.ContentI18n!.ToDomain()),
        (Musics ?? []).Select(music => new Music(music.DisplayName, music.Blob.ToDomain())).ToList(),
        Icon?.ToDomain(),
        (Modes ?? new Dictionary<string, ModeDto>()).Select(pair => pair.Value.ToDomain(pair.Key)).ToList(),
        Families ?? "");
}

internal sealed record EngineDto(string Type, BlobDto? Blob = null)
{
    public GameEngine ToDomain() => Type switch
    {
        "V96" => new BaseEngine(),
        "Custom" => new CustomEngine((Blob ?? throw new InvalidOperationException("A custom engine has no blob.")).ToDomain()),
        _ => throw new InvalidOperationException($"Unknown game engine type {Type}."),
    };
}

internal sealed record PatcherDto(BlobDto Blob);

internal sealed record MusicDto(BlobDto Blob, string DisplayName);

internal sealed record ModeDto(string DisplayName, bool IsVisible, IReadOnlyDictionary<string, OptionDto>? Options = null)
{
    public GameMode ToDomain(string key) => new(
        key,
        DisplayName,
        IsVisible,
        (Options ?? new Dictionary<string, OptionDto>())
            .Select(pair => new GameOption(pair.Key, pair.Value.DisplayName, pair.Value.IsVisible, pair.Value.IsEnabled, pair.Value.DefaultValue))
            .ToList());
}

internal sealed record OptionDto(string DisplayName, bool IsVisible, bool IsEnabled, bool DefaultValue);

internal sealed record BuildI18nDto(string? DisplayName = null, string? Description = null, BlobDto? ContentI18n = null);
