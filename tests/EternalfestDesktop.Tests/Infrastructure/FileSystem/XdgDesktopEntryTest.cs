using EternalfestDesktop.Infrastructure.FileSystem;
using EternalfestDesktop.Tests.Support;
using Microsoft.Extensions.Logging;

namespace EternalfestDesktop.Tests.Infrastructure.FileSystem;

public sealed class XdgDesktopEntryTest : IDisposable
{
    private readonly DirectoryInfo _home = Directory.CreateTempSubdirectory("eternalfest-desktop-tests-");
    private readonly CapturingLogger<XdgDesktopEntry> _log = new();

    public void Dispose() => _home.Delete(recursive: true);

    private string DataHome => Path.Combine(_home.FullName, ".local", "share");
    private string EntryPath => Path.Combine(DataHome, "applications", "eternalfest-desktop.desktop");

    /// @spec packaging::linux-desktop-entry
    [Fact]
    public void Adds_the_app_to_the_applications_menu()
    {
        var app = ExtractedApp("Jeux", "Eternalfest Desktop");

        Entry(app).Register();

        var lines = File.ReadAllLines(EntryPath);
        Assert.Equal("[Desktop Entry]", lines[0]);
        Assert.Contains("Type=Application", lines);
        Assert.Contains("Name=Eternalfest Desktop", lines);
        Assert.Contains($"Exec=\"{app}\"", lines);
        Assert.Contains($"TryExec={app}", lines);
        Assert.Contains("Icon=eternalfest-desktop", lines);
        Assert.Contains($"StartupWMClass={XdgDesktopEntry.WindowClass}", lines);
        Assert.Contains("Categories=Game;", lines);
        Assert.DoesNotContain(_log.Entries, entry => entry.Level >= LogLevel.Warning);
    }

    /// @spec packaging::linux-desktop-entry
    [Fact]
    public void Installs_the_app_icon_in_the_user_icon_theme()
    {
        var app = ExtractedApp("Eternalfest Desktop");

        Entry(app).Register();

        Assert.Equal(
            File.ReadAllBytes(Path.Combine(Path.GetDirectoryName(app)!, "icon.png")),
            File.ReadAllBytes(Path.Combine(DataHome, "icons", "hicolor", "256x256", "apps", "eternalfest-desktop.png")));
    }

    /// @spec packaging::linux-desktop-entry
    [Fact]
    public void Quotes_a_path_the_desktop_would_otherwise_expand()
    {
        var app = ExtractedApp("Jeux", "100% \"$HOME\"");

        Entry(app).Register();

        var games = Path.Combine(_home.FullName, "Jeux");
        Assert.Contains($"Exec=\"{games}/100%% \\\\\"\\\\$HOME\\\\\"/EternalfestDesktop\"", File.ReadAllLines(EntryPath));
    }

    /// @spec packaging::linux-desktop-entry
    [Fact]
    public void Leaves_an_up_to_date_entry_alone()
    {
        var app = ExtractedApp("Eternalfest Desktop");
        Entry(app).Register();
        var written = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        File.SetLastWriteTimeUtc(EntryPath, written);

        Entry(app).Register();

        Assert.Equal(written, File.GetLastWriteTimeUtc(EntryPath));
    }

    /// @spec packaging::linux-desktop-entry
    [Fact]
    public void Follows_the_app_when_its_folder_moves()
    {
        Entry(ExtractedApp("Téléchargements", "Eternalfest Desktop")).Register();
        var moved = ExtractedApp("Jeux", "Eternalfest Desktop");

        Entry(moved).Register();

        Assert.Contains($"TryExec={moved}", File.ReadAllLines(EntryPath));
    }

    /// @spec packaging::linux-desktop-entry
    [Fact]
    public void Warns_instead_of_failing_when_the_menu_cant_be_written()
    {
        Directory.CreateDirectory(DataHome);
        File.WriteAllText(Path.Combine(DataHome, "applications"), "not a folder");

        Entry(ExtractedApp("Eternalfest Desktop")).Register();

        Assert.Contains(_log.Entries, entry => entry.Level == LogLevel.Warning);
    }

    private XdgDesktopEntry Entry(string app) => new(DataHome, app, _log);

    /// <summary>The app as a player extracts it: its executable next to its icon.</summary>
    private string ExtractedApp(params string[] folders)
    {
        var folder = Directory.CreateDirectory(Path.Combine([_home.FullName, .. folders])).FullName;
        File.WriteAllBytes(Path.Combine(folder, "icon.png"), PublishedContree.Bytes(32));
        var app = Path.Combine(folder, "EternalfestDesktop");
        File.WriteAllText(app, "");
        return app;
    }
}
