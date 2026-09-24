using EternalfestDesktop.Application;
using EternalfestDesktop.Domain;
using EternalfestDesktop.Infrastructure.EternalfestApi;
using EternalfestDesktop.Infrastructure.FileSystem;
using Microsoft.Extensions.Logging;

using var loggerFactory = LoggerFactory.Create(logging => logging.AddSimpleConsole(console => console.SingleLine = true));
using var http = new HttpClient { BaseAddress = new Uri("https://eternalfest.net/") };
var catalog = new EternalfestApiGameCatalog(http, loggerFactory.CreateLogger<EternalfestApiGameCatalog>());
var store = new FileSystemGameStore(AppFolders.Cache);
var downloadGame = new DownloadGame(catalog, new EternalfestApiBlobSource(http), store);

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
        Console.WriteLine($"{downloaded.DisplayName.Default} {downloaded.Build.Version} is playable offline ({AppFolders.Cache}).");
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
            """);
        return 1;
}

/// <summary>Reports progress on the caller's thread, so lines print in order.</summary>
internal sealed class ConsoleProgress<T>(Action<T> report) : IProgress<T>
{
    public void Report(T value) => report(value);
}
