using System.Text.Json.Nodes;
using EternalfestDesktop.Application;
using EternalfestDesktop.Domain;
using EternalfestDesktop.Infrastructure.EternalfestApi;
using EternalfestDesktop.Infrastructure.LocalServer;
using EternalfestDesktop.Tests.Support;
using Microsoft.Extensions.Logging.Abstractions;

namespace EternalfestDesktop.Tests.Infrastructure.LocalServer;

public sealed class RuffleFlashPlayerTest
{
    private static readonly Uri Origin = new("http://127.0.0.1:50317");

    /// @spec play::launches-ruffle
    [Fact]
    public void Loads_the_loader_from_the_offline_backend_like_the_website_embeds_it()
    {
        var game = Game();
        var run = game.NewRun(new RunChoices("solo", ["mirror"], Locale: "en-US", Volume: 40), DateTimeOffset.UnixEpoch);

        var arguments = RuffleFlashPlayer.Arguments(new FlashGame(Origin, game, run, Fullscreen: false), screen: null);

        Assert.Equal("http://127.0.0.1:50317/", ValueOf(arguments, "--base"));
        Assert.Equal("http://127.0.0.1:50317/assets/loader.swf", ValueOf(arguments, "--spoof-url"));
        Assert.Equal($"http://127.0.0.1:50317/runs/{run.Id}", ValueOf(arguments, "--referer"));
        Assert.Contains("--dummy-external-interface", arguments);
        Assert.DoesNotContain("--fullscreen", arguments);
        Assert.Equal("http://127.0.0.1:50317/assets/loader.swf", arguments[^1]);

        var flashVars = FlashVars(arguments);
        Assert.Equal(["object_id", "run", "game", "options"], flashVars.Keys);
        Assert.Equal("swf1234", flashVars["object_id"]);
        Assert.Equal(game.Id.ToString(), flashVars["game"]);
        var runVar = JsonNode.Parse(flashVars["run"])!;
        Assert.Equal(run.Id.ToString(), runVar["id"]!.GetValue<string>());
        Assert.Equal("solo", runVar["game_mode"]!.GetValue<string>());
        Assert.Equal(40, runVar["settings"]!["volume"]!.GetValue<int>());
        Assert.True(runVar["build"]!["modes"]!["solo"]!["options"]!["ninja"]!["is_enabled"]!.GetValue<bool>());
        var options = JsonNode.Parse(flashVars["options"])!;
        Assert.Equal("solo", options["mode"]!.GetValue<string>());
        Assert.Equal("mirror", options["options"]![0]!.GetValue<string>());
        Assert.Equal("en-US", options["locale"]!.GetValue<string>());
        Assert.Equal(40, options["settings"]!["volume"]!.GetValue<int>());
        Assert.Equal(["detail", "shake", "sound", "music", "volume", "locale"], options["settings"]!.AsObject().Select(setting => setting.Key));
        Assert.Equal("en-US", options["settings"]!["locale"]!.GetValue<string>());
    }

    /// @spec play::launches-ruffle
    [Fact]
    public void Starts_fullscreen_when_chosen()
    {
        var game = Game();

        var arguments = RuffleFlashPlayer.Arguments(new FlashGame(Origin, game, game.NewRun(new RunChoices(Fullscreen: true), DateTimeOffset.UnixEpoch), Fullscreen: true), screen: null);

        Assert.Contains("--fullscreen", arguments);
    }

    /// @spec play::fills-screen-height
    [Theory]
    [InlineData(1040, 1.0, "968")]
    [InlineData(1560, 1.5, "1452")]
    public void Fills_the_screen_height_under_the_title_bar(int availableHeight, double scaling, string gameHeight)
    {
        var screen = new AvailableScreenArea(HeightInPixels: availableHeight, Scaling: scaling);

        var arguments = RuffleFlashPlayer.Arguments(Windowed(), screen);

        Assert.Equal(gameHeight, ValueOf(arguments, "--height"));
        Assert.DoesNotContain("--width", arguments);
        Assert.Contains("--no-gui", arguments);
    }

    /// @spec play::fills-screen-height
    [Fact]
    public void Lets_ruffle_size_the_window_when_the_screen_is_unknown()
    {
        var arguments = RuffleFlashPlayer.Arguments(Windowed(), screen: null);

        Assert.DoesNotContain("--height", arguments);
        Assert.Contains("--no-gui", arguments);
    }

    /// @spec play::fills-screen-height
    [Fact]
    public void Leaves_a_fullscreen_game_the_whole_screen()
    {
        var game = Game();
        var screen = new AvailableScreenArea(HeightInPixels: 1040, Scaling: 1.0);

        var arguments = RuffleFlashPlayer.Arguments(new FlashGame(Origin, game, game.NewRun(new RunChoices(Fullscreen: true), DateTimeOffset.UnixEpoch), Fullscreen: true), screen);

        Assert.DoesNotContain("--height", arguments);
    }

    public static bool IsMacOS => OperatingSystem.IsMacOS();

    /// @spec packaging::macos-app
    [Fact(Skip = "Ruffle is an app bundle on macOS only", SkipUnless = nameof(IsMacOS))]
    public void Runs_ruffle_from_its_app_bundle_on_macos()
    {
        Assert.EndsWith(Path.Combine("ruffle", "Ruffle.app", "Contents", "MacOS", "ruffle"), RuffleFlashPlayer.NextToApp(), StringComparison.Ordinal);
    }

    /// @spec play::never-opens-websites
    [Fact]
    public void Denies_opening_websites()
    {
        var arguments = RuffleFlashPlayer.Arguments(Windowed(), screen: null);

        Assert.Equal("deny", ValueOf(arguments, "--open-url-mode"));
    }

    /// @spec play::tears-down-on-exit
    [Fact]
    public async Task Fails_explicitly_when_ruffle_is_missing()
    {
        var game = Game();
        var player = new RuffleFlashPlayer(Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString(), "ruffle"), () => null, NullLogger<RuffleFlashPlayer>.Instance);

        await Assert.ThrowsAsync<FlashPlayerMissingException>(() =>
            player.Play(new FlashGame(Origin, game, game.NewRun(new RunChoices(), DateTimeOffset.UnixEpoch), false), TestContext.Current.CancellationToken));
    }

    private static FlashGame Windowed()
    {
        var game = Game();
        return new FlashGame(Origin, game, game.NewRun(new RunChoices(), DateTimeOffset.UnixEpoch), Fullscreen: false);
    }

    private static Game Game() => EternalfestJson.ParseGame(PublishedContree.Named("Accumulation").Document());

    private static string ValueOf(IReadOnlyList<string> arguments, string option) =>
        arguments[arguments.ToList().IndexOf(option) + 1];

    private static Dictionary<string, string> FlashVars(IReadOnlyList<string> arguments) =>
        arguments.Select((argument, index) => (argument, index))
            .Where(pair => pair.argument == "-P")
            .Select(pair => arguments[pair.index + 1].Split('=', 2))
            .ToDictionary(pair => pair[0], pair => pair[1]);
}
