using EternalfestDesktop.Application;
using EternalfestDesktop.Infrastructure.EternalfestApi;
using EternalfestDesktop.Infrastructure.FileSystem;
using EternalfestDesktop.Infrastructure.LocalServer;
using EternalfestDesktop.Ui.ViewModels;
using Microsoft.Extensions.Logging;

namespace EternalfestDesktop.Ui;

/// <summary>Wires the app on its real adapters.</summary>
internal sealed class Composition : IDisposable
{
    private readonly ILoggerFactory _loggers = LoggerFactory.Create(logging => logging.AddSimpleConsole(console => console.SingleLine = true));
    private readonly HttpClient _http = new() { BaseAddress = new Uri("https://eternalfest.net/"), Timeout = TimeSpan.FromSeconds(30) };

    public MainWindowViewModel MainWindow()
    {
        var catalog = new EternalfestApiGameCatalog(_http, _loggers.CreateLogger<EternalfestApiGameCatalog>());
        var blobs = new EternalfestApiBlobSource(_http);
        var store = new FileSystemGameStore(AppFolders.Cache);
        var downloadGame = new DownloadGame(catalog, blobs, store);
        var playGame = new PlayGame(
            downloadGame,
            new KestrelOfflineBackend(store, BundledFlashFiles.NextToApp(), _loggers),
            new RuffleFlashPlayer(RuffleFlashPlayer.NextToApp(), _loggers.CreateLogger<RuffleFlashPlayer>()),
            TimeProvider.System);
        return new MainWindowViewModel(
            new BrowseCatalog(catalog, new JsonCatalogSnapshots(AppFolders.Catalog), store),
            new BitmapContreeIcons(new FetchIcon(blobs, store)),
            (contree, back) => new ContreePageViewModel(contree, catalog, store, downloadGame, playGame, BundledFlashFiles.LoaderVersion, back));
    }

    public void Dispose()
    {
        _http.Dispose();
        _loggers.Dispose();
    }
}
