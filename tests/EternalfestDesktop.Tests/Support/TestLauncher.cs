using EternalfestDesktop.Application;
using EternalfestDesktop.Domain;
using EternalfestDesktop.Infrastructure.EternalfestApi;
using EternalfestDesktop.Infrastructure.FileSystem;
using EternalfestDesktop.Infrastructure.LocalServer;
using EternalfestDesktop.Ui;
using EternalfestDesktop.Ui.ViewModels;
using Microsoft.Extensions.Logging.Abstractions;

namespace EternalfestDesktop.Tests.Support;

/// <summary>The launcher wired on its real adapters, against a fake eternalfest.net and a temporary cache folder.</summary>
internal sealed class TestLauncher : IDisposable
{
    private readonly HttpClient _http;

    public TestLauncher()
    {
        CacheFolder = Directory.CreateTempSubdirectory("eternalfest-desktop-tests-");
        _http = Eternalfest.CreateClient();
        Catalog = new EternalfestApiGameCatalog(_http, NullLogger<EternalfestApiGameCatalog>.Instance);
        Store = new FileSystemGameStore(CacheFolder.FullName);
        DownloadGame = new DownloadGame(Catalog, new EternalfestApiBlobSource(_http), Store);
        PlayGame = new PlayGame(DownloadGame, new KestrelOfflineBackend(Store, BundledFlashFiles.NextToApp(), NullLoggerFactory.Instance), FlashPlayer, TimeProvider.System);
    }

    public FakeEternalfestServer Eternalfest { get; } = new();
    public DirectoryInfo CacheFolder { get; }
    public GameCatalog Catalog { get; }
    public GameStore Store { get; }
    public DownloadGame DownloadGame { get; }
    public PlayGame PlayGame { get; }
    public FakeFlashPlayer FlashPlayer { get; } = new();
    public List<DownloadProgress> ReportedProgress { get; } = [];

    public Task<Game> Download(PublishedContree contree) =>
        DownloadGame.Execute(contree.Id, new SynchronousProgress<DownloadProgress>(ReportedProgress.Add), TestContext.Current.CancellationToken);

    public Task Play(PublishedContree contree, RunChoices? choices = null) =>
        PlayGame.Execute(contree.Id, choices ?? new RunChoices(), progress: null, TestContext.Current.CancellationToken);

    public BrowseCatalog BrowseCatalog => new(Catalog, new JsonCatalogSnapshots(Path.Combine(CacheFolder.FullName, "catalog.json")), Store);

    public PreferencesFile Preferences => _preferences ??= new PreferencesFile(Path.Combine(CacheFolder.FullName, "preferences.json"));
    private PreferencesFile? _preferences;

    public MainWindowViewModel MainWindow() => new(
        BrowseCatalog,
        new NoIcons(),
        Preferences,
        ContreePage,
        Settings);

    public ContreePageViewModel ContreePage(ContreeCardViewModel contree, Action back) =>
        new(contree, Catalog, Store, DownloadGame, PlayGame, BundledFlashFiles.LoaderVersion, Preferences, back);

    public SettingsViewModel Settings(Action back) =>
        new(Preferences, new ClearCache(Store, PlayGame), CacheFolder.FullName, Path.Combine(CacheFolder.FullName, "logs"), back);

    /// <summary>A launcher started again, on the same cache and preferences.</summary>
    public void Restart() => _preferences = null;

    public Task<Game?> FindDownloaded(PublishedContree contree) =>
        Store.FindGame(contree.Id, TestContext.Current.CancellationToken);

    public void Dispose()
    {
        _http.Dispose();
        Eternalfest.Dispose();
        CacheFolder.Delete(recursive: true);
    }

    private sealed class NoIcons : ContreeIcons
    {
        public Task<Avalonia.Media.IImage?> Load(Blob icon, CancellationToken cancellationToken) => Task.FromResult<Avalonia.Media.IImage?>(null);
    }

    private sealed class SynchronousProgress<T>(Action<T> report) : IProgress<T>
    {
        public void Report(T value) => report(value);
    }
}
