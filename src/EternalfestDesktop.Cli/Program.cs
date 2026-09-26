using EternalfestDesktop.Application;
using EternalfestDesktop.Domain;
using EternalfestDesktop.Infrastructure.EternalfestApi;
using EternalfestDesktop.Infrastructure.FileSystem;
using EternalfestDesktop.Infrastructure.LocalServer;
using EternalfestDesktop.Infrastructure.Quests;
using Microsoft.Extensions.Logging;

using var loggerFactory = LoggerFactory.Create(logging => logging.AddSimpleConsole(console => console.SingleLine = true));
using var http = new HttpClient { BaseAddress = new Uri("https://eternalfest.net/") };
var catalog = new EternalfestApiGameCatalog(http, loggerFactory.CreateLogger<EternalfestApiGameCatalog>());
var store = new FileSystemGameStore(AppFolders.ForThisUser.Cache);
var downloadGame = new DownloadGame(catalog, new EternalfestApiBlobSource(http), store);
var playGame = new PlayGame(
    downloadGame,
    new EmbeddedQuestBook(),
    new XmlContreeItems(store, loggerFactory.CreateLogger<XmlContreeItems>()),
    new KestrelOfflineBackend(store, BundledFlashFiles.NextToApp(), loggerFactory),
    new FallbackFlashPlayer(
        new FlashProjectorPlayer(FlashProjectorPlayer.NextToApp(), () => null, loggerFactory.CreateLogger<FlashProjectorPlayer>()),
        new RuffleFlashPlayer(RuffleFlashPlayer.NextToApp(), () => null, loggerFactory.CreateLogger<RuffleFlashPlayer>()),
        loggerFactory.CreateLogger<FallbackFlashPlayer>()),
    TimeProvider.System);

switch (args)
{
    case ["catalog"]:
        foreach (var entry in await catalog.ListPublicGames(CancellationToken.None))
            Console.WriteLine($"{entry.Id}  {entry.Key,-24} {entry.Version,-10} {entry.DisplayName.Default}");
        return 0;
    case ["game", var id]:
        var game = await catalog.GetGame(GameId.Parse(id), CancellationToken.None);
        Console.WriteLine($"{game.DisplayName.Default} {game.Build.Version} (loader {game.Build.LoaderVersion}, engine {game.Build.Engine.GetType().Name})");
        Console.WriteLine($"Modes: {string.Join(", ", game.Build.Modes.Select(mode => mode.Key))}");
        Console.WriteLine($"{game.Build.Blobs().Count} files, {game.Build.ByteSize() / 1024} KiB");
        return 0;
    case ["download", var id]:
        var lastPercent = -1L;
        var downloaded = await downloadGame.Execute(GameId.Parse(id), new ConsoleProgress<DownloadProgress>(progress =>
        {
            var percent = progress.TotalBytes == 0 ? 100 : progress.DownloadedBytes * 100 / progress.TotalBytes;
            if (percent / 10 != lastPercent / 10)
                Console.WriteLine($"{percent}%");
            lastPercent = percent;
        }), CancellationToken.None);
        Console.WriteLine($"{downloaded.DisplayName.Default} {downloaded.Build.Version} is playable offline ({AppFolders.ForThisUser.Cache}).");
        return 0;
    case ["play", var id, .. var rest]:
        var mode = rest.FirstOrDefault(argument => !argument.StartsWith("--", StringComparison.Ordinal));
        var options = rest.Skip(1).Where(argument => !argument.StartsWith("--", StringComparison.Ordinal)).ToList();
        var toPlay = await downloadGame.Execute(GameId.Parse(id), null, CancellationToken.None);
        if (toPlay.Build.RequiresNewerLoaderThan(BundledFlashFiles.LoaderVersion))
            Console.WriteLine($"Warning: {toPlay.DisplayName.Default} requires loader {toPlay.Build.LoaderVersion}, newer than the bundled {BundledFlashFiles.LoaderVersion}. It may not work.");
        var result = await playGame.Execute(
            toPlay.Id,
            new RunChoices(
                mode,
                mode is null ? null : options,
                Fullscreen: rest.Contains("--fullscreen"),
                Profile: rest.Contains("--new-player") ? PlayerProfile.NewPlayer : PlayerProfile.Complete),
            null,
            CancellationToken.None);
        if (result is not null)
            Console.WriteLine($"{(result.IsVictory ? "Victory" : "Game over")}: level {result.HighestLevel}, score {string.Join(" / ", result.Scores)}. Scores aren't saved offline: play on https://eternalfest.net to keep them.");
        return 0;
    case ["downloaded"]:
        foreach (var local in await store.ListGames(CancellationToken.None))
            Console.WriteLine($"{local.Id}  {local.Build.Version,-10} {local.DisplayName.Default}");
        return 0;
    default:
        Console.Error.WriteLine("""
            Eternalfest Desktop (unofficial) — developer console

            Usage:
              catalog          List the public contrées
              game <id>        Show a contrée's active build
              download <id>    Download a contrée to play it offline
              downloaded       List the downloaded contrées
              play <id> [mode [options...]] [--fullscreen] [--new-player]
                               Play a contrée offline, with every quest completed
                               unless --new-player
            """);
        return 1;
}

/// <summary>Reports progress on the caller's thread, so lines print in order.</summary>
internal sealed class ConsoleProgress<T>(Action<T> report) : IProgress<T>
{
    public void Report(T value) => report(value);
}
