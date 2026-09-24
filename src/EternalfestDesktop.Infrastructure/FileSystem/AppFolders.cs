namespace EternalfestDesktop.Infrastructure.FileSystem;

/// <summary>Where the app keeps its data: never inside its own folder, which may be read-only.</summary>
/// @spec packaging::data-outside-install
public static class AppFolders
{
    /// <summary><c>%LOCALAPPDATA%\EternalfestDesktop</c> on Windows, <c>$XDG_DATA_HOME/eternalfest-desktop</c> elsewhere.</summary>
    public static string Data { get; } = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData, Environment.SpecialFolderOption.Create),
        OperatingSystem.IsWindows() ? "EternalfestDesktop" : "eternalfest-desktop");

    public static string Cache => Path.Combine(Data, "cache");
    public static string Logs => Path.Combine(Data, "logs");
}
