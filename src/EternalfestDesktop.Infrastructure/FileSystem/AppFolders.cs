namespace EternalfestDesktop.Infrastructure.FileSystem;

/// <summary>Where the app keeps its data: never inside its own folder, which may be read-only.</summary>
/// @spec packaging::data-outside-install
public sealed record AppFolders(string Data)
{
    /// <summary><c>%LOCALAPPDATA%\EternalfestDesktop</c> on Windows, <c>$XDG_DATA_HOME/eternalfest-desktop</c> elsewhere.</summary>
    public static AppFolders ForThisUser { get; } = new(Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData, Environment.SpecialFolderOption.Create),
        OperatingSystem.IsWindows() ? "EternalfestDesktop" : "eternalfest-desktop"));

    public string Cache => Path.Combine(Data, "cache");
    public string Logs => Path.Combine(Data, "logs");
    public string Catalog => Path.Combine(Data, "catalog.json");
    public string Preferences => Path.Combine(Data, "preferences.json");
}
