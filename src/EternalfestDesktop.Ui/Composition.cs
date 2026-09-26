using System.Globalization;
using System.Reflection;
using EternalfestDesktop.Application;
using EternalfestDesktop.Infrastructure.EternalfestApi;
using EternalfestDesktop.Infrastructure.FileSystem;
using EternalfestDesktop.Infrastructure.LocalServer;
using EternalfestDesktop.Infrastructure.Quests;
using EternalfestDesktop.Ui.ViewModels;
using Microsoft.Extensions.Logging;
using Serilog;
using Serilog.Extensions.Logging;

namespace EternalfestDesktop.Ui;

/// <summary>Wires the app on its real adapters.</summary>
internal sealed class Composition : IDisposable
{
    private readonly PreferencesFile _preferences = new(Path.Combine(AppFolders.Data, "preferences.json"));
    private readonly Serilog.Core.Logger _log = new LoggerConfiguration()
        .MinimumLevel.Information()
        .WriteTo.File(Path.Combine(AppFolders.Logs, "eternalfest-desktop-.log"), formatProvider: CultureInfo.InvariantCulture, rollingInterval: RollingInterval.Day, retainedFileCountLimit: 7)
        .CreateLogger();
    private readonly SerilogLoggerFactory _loggers;
    private readonly HttpClient _http = new() { BaseAddress = new Uri("https://eternalfest.net/"), Timeout = TimeSpan.FromSeconds(30) };

    public Composition()
    {
        _loggers = new SerilogLoggerFactory(_log);
        _log.Information(
            "Eternalfest Desktop {Version} started on {Os} ({Runtime}), data in {Data}",
            typeof(Composition).Assembly.GetName().Version?.ToString(3),
            System.Runtime.InteropServices.RuntimeInformation.OSDescription,
            System.Runtime.InteropServices.RuntimeInformation.RuntimeIdentifier,
            AppFolders.Data);
    }

    /// <summary>On Linux, adds the app to the applications menu. Development builds leave the menu alone.</summary>
    /// @spec packaging::linux-desktop-entry
    public void AddToApplicationsMenu()
    {
        if (OperatingSystem.IsLinux() && !IsDevelopmentBuild)
            XdgDesktopEntry.ForThisApp(_loggers.CreateLogger<XdgDesktopEntry>()).Register();
    }

    private static readonly bool IsDevelopmentBuild =
        typeof(Composition).Assembly.GetCustomAttribute<AssemblyConfigurationAttribute>()?.Configuration == "Debug";

    /// <summary>Applies the chosen launcher language: before any view is created.</summary>
    public void ApplyLanguage()
    {
        if (_preferences.Current.UiLanguage is { } language)
            CultureInfo.CurrentUICulture = CultureInfo.DefaultThreadCurrentUICulture = CultureInfo.GetCultureInfo(language);
    }

    public MainWindowViewModel MainWindow()
    {
        var cacheFolder = _preferences.Current.CacheFolder ?? AppFolders.Cache;
        var catalog = new EternalfestApiGameCatalog(_http, _loggers.CreateLogger<EternalfestApiGameCatalog>());
        var blobs = new EternalfestApiBlobSource(_http);
        var store = new FileSystemGameStore(cacheFolder);
        var downloadGame = new DownloadGame(catalog, blobs, store);
        var quests = new EmbeddedQuestBook();
        var playGame = new PlayGame(
            downloadGame,
            quests,
            new XmlContreeItems(store, _loggers.CreateLogger<XmlContreeItems>()),
            new KestrelOfflineBackend(store, BundledFlashFiles.NextToApp(), _loggers),
            new RuffleFlashPlayer(RuffleFlashPlayer.NextToApp(), _loggers.CreateLogger<RuffleFlashPlayer>()),
            TimeProvider.System);
        return new MainWindowViewModel(
            new BrowseCatalog(catalog, new JsonCatalogSnapshots(AppFolders.Catalog), store),
            new BitmapContreeIcons(new FetchIcon(blobs, store)),
            _preferences,
            (contree, back) => new ContreePageViewModel(contree, catalog, store, downloadGame, playGame, quests, BundledFlashFiles.LoaderVersion, _preferences, back),
            back => new SettingsViewModel(_preferences, new ClearCache(store, playGame), cacheFolder, AppFolders.Logs, back));
    }

    public void Dispose()
    {
        _http.Dispose();
        _loggers.Dispose();
        _log.Dispose();
    }
}
