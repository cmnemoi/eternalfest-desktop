using System.Text.Json;
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

    private static readonly JsonSerializerOptions Json = new() { WriteIndented = true };

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
