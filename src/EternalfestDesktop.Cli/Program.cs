using EternalfestDesktop.Domain;
using EternalfestDesktop.Infrastructure.EternalfestApi;
using Microsoft.Extensions.Logging;

using var loggerFactory = LoggerFactory.Create(logging => logging.AddSimpleConsole(console => console.SingleLine = true));
using var http = new HttpClient { BaseAddress = new Uri("https://eternalfest.net/") };
var catalog = new EternalfestApiGameCatalog(http, loggerFactory.CreateLogger<EternalfestApiGameCatalog>());

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
    default:
        Console.Error.WriteLine("""
            Eternalfest Desktop (unofficial) — developer console

            Usage:
              catalog      List the public contrées
              game <id>    Show a contrée's active build
            """);
        return 1;
}
