using System.Text.Json;
using System.Text.Json.Serialization;
using EternalfestDesktop.Domain;

namespace EternalfestDesktop.Ui;

/// <summary>What the player chose in the launcher, kept between sessions.</summary>
public sealed class Preferences
{
    /// <summary>"en" or "fr"; null follows the system language.</summary>
    public string? UiLanguage { get; set; }

    /// <summary>null keeps contrées in the app data folder.</summary>
    public string? CacheFolder { get; set; }

    public Guid? LastPlayed { get; set; }

    /// @spec ui::contree-page
    public Dictionary<Guid, RunChoices> Choices { get; init; } = [];
}

/// <summary>Keeps the preferences in a JSON file. A missing or unreadable file gives the defaults.</summary>
public sealed class PreferencesFile(string path)
{
    private Preferences? _current;

    public Preferences Current => _current ??= Load();

    public void Save() => Save(Current);

    private static readonly JsonSerializerOptions Json = new() { WriteIndented = true, Converters = { new ProfileByName() } };

    public Preferences Load()
    {
        try
        {
            return File.Exists(path) ? JsonSerializer.Deserialize<Preferences>(File.ReadAllText(path), Json) ?? new() : new();
        }
        catch (JsonException)
        {
            return new();
        }
    }

    public void Save(Preferences preferences)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        var temporary = $"{path}.{Guid.NewGuid():N}.part";
        File.WriteAllText(temporary, JsonSerializer.Serialize(preferences, Json));
        File.Move(temporary, path, overwrite: true);
    }
}

/// <summary>Writes a player profile by its name, and reads a missing or unknown one as the complete profile.</summary>
/// <remarks>A version of the launcher may read the choices a newer one remembered.</remarks>
/// @spec profile::complete-by-default
internal sealed class ProfileByName : JsonConverter<PlayerProfile>
{
    public override bool HandleNull => true;

    public override PlayerProfile Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType is JsonTokenType.StartObject or JsonTokenType.StartArray)
            reader.Skip();
        return reader.TokenType == JsonTokenType.String && Enum.TryParse<PlayerProfile>(reader.GetString(), out var profile) && Enum.IsDefined(profile)
            ? profile
            : PlayerProfile.Complete;
    }

    public override void Write(Utf8JsonWriter writer, PlayerProfile value, JsonSerializerOptions options) =>
        writer.WriteStringValue(value.ToString());
}
