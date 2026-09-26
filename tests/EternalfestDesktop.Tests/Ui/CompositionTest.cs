using EternalfestDesktop.Infrastructure.FileSystem;
using EternalfestDesktop.Tests.Support;
using EternalfestDesktop.Ui;
using EternalfestDesktop.Ui.Resources;

namespace EternalfestDesktop.Tests.Ui;

/// <summary>The app as it starts: wired on its real adapters, with its data in a temporary folder and a fake eternalfest.net.</summary>
public sealed class CompositionTest : IDisposable
{
    private readonly FakeEternalfestServer _eternalfest = new();
    private readonly DirectoryInfo _dataFolder = Directory.CreateTempSubdirectory("eternalfest-desktop-app-");
    private Composition? _app;

    private AppFolders Folders => new(_dataFolder.FullName);

    public void Dispose()
    {
        _app?.Dispose();
        _eternalfest.Dispose();
        _dataFolder.Delete(recursive: true);
    }

    [Fact]
    public Task Starts_on_the_library_of_eternalfest() => HeadlessApp.Run(async () =>
    {
        _eternalfest.Publishing(PublishedContree.Named("Dojo")).Publishing(PublishedContree.Named("Himmelen"));

        var window = await Start();

        Assert.Equal(["Dojo", "Himmelen"], window.Cards);
        Assert.False(window.Shows(Strings.OfflineNotice));
    });

    /// @spec ui::library-icons
    [Fact]
    public Task Shows_the_icons_it_can_decode_even_after_one_it_cant() => HeadlessApp.Run(async () =>
    {
        _eternalfest.Publishing(PublishedContree.Named("Himmelen")).Publishing(PublishedContree.Named("Dojo").WithPngIcon());

        var window = await Start();

        await window.WaitUntil(() => window.ShowsIconOn("Dojo"), "the icon of Dojo");
        Assert.False(window.ShowsIconOn("Himmelen"));
        Assert.Equal(["Himmelen", "Dojo"], window.Cards);
    });

    /// @spec catalog::last-known-catalog-offline
    /// @spec packaging::data-outside-install
    [Fact]
    public Task Shows_the_catalog_saved_by_its_last_start_when_offline() => HeadlessApp.Run(async () =>
    {
        _eternalfest.Publishing(PublishedContree.Named("Dojo"));
        await Start();
        _eternalfest.IsUnreachable = true;

        var window = await Restart();

        await window.WaitFor(Strings.OfflineNotice);
        Assert.Equal(["Dojo"], window.Cards);
        Assert.True(File.Exists(Folders.Catalog));
    });

    /// @spec packaging::data-outside-install
    [Fact]
    public Task Logs_in_its_own_logs_folder() => HeadlessApp.Run(async () =>
    {
        await Start();

        Assert.NotEmpty(Directory.EnumerateFiles(Folders.Logs, "eternalfest-desktop-*.log"));
    });

    /// @spec ui::settings
    [Fact]
    public Task Keeps_what_it_downloads_in_the_cache_folder_chosen_in_the_settings() => HeadlessApp.Run(async () =>
    {
        var chosenFolder = Path.Combine(_dataFolder.FullName, "chosen");
        var contree = PublishedContree.Named("Dojo").WithPngIcon();
        _eternalfest.Publishing(contree);
        new PreferencesFile(Folders.Preferences) { Current = { CacheFolder = chosenFolder } }.Save();

        var window = await Start();

        await window.WaitUntil(() => window.ShowsIconOn("Dojo"), "the icon of Dojo");
        Assert.True(new FileSystemGameStore(chosenFolder).HasBlob(contree.BlobOf("icon")));
        Assert.False(Directory.Exists(Folders.Cache));
    });

    private async Task<LauncherWindow> Start()
    {
        _app = new Composition(Folders, _eternalfest, launcherScreen: () => null);
        var window = LauncherWindow.Opening(_app.MainWindow());
        await window.WaitUntil(() => !window.Main.Library.IsLoading && window.Buttons.Contains(Strings.Settings), "the library to load");
        return window;
    }

    private Task<LauncherWindow> Restart()
    {
        _app?.Dispose();
        return Start();
    }
}
