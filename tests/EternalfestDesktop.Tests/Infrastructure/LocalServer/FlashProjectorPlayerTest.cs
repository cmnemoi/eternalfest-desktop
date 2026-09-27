using System.Runtime.Versioning;
using System.Web;
using EternalfestDesktop.Application;
using EternalfestDesktop.Domain;
using EternalfestDesktop.Infrastructure.EternalfestApi;
using EternalfestDesktop.Infrastructure.LocalServer;
using EternalfestDesktop.Tests.Support;
using Microsoft.Extensions.Logging.Abstractions;

namespace EternalfestDesktop.Tests.Infrastructure.LocalServer;

public sealed class FlashProjectorPlayerTest
{
    private static readonly Uri Origin = new("http://127.0.0.1:50317");

    /// @spec play::launches-flash-projector
    [Fact]
    public void Opens_the_loader_from_the_offline_backend_with_the_flash_vars_in_its_url()
    {
        var game = Windowed();

        var loader = FlashProjectorPlayer.LoaderUrl(game);

        Assert.Equal("http://127.0.0.1:50317/assets/loader.swf", loader.GetLeftPart(UriPartial.Path));
        var query = HttpUtility.ParseQueryString(loader.Query);
        Assert.Equal(LoaderFlashVars.For(game), query.AllKeys.Select(name => KeyValuePair.Create(name!, query[name]!)));
    }

    /// @spec play::tears-down-on-exit
    [Fact]
    public async Task Fails_explicitly_when_the_projector_is_missing()
    {
        var missing = new LinuxFlashProjector(Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString()));
        var player = new FlashProjectorPlayer(missing, () => null, NullLogger<FlashProjectorPlayer>.Instance);

        await Assert.ThrowsAsync<FlashPlayerMissingException>(() => player.Play(Windowed(), TestContext.Current.CancellationToken));
    }

    /// @spec play::flash-projector-first
    [Fact]
    public async Task Counts_as_missing_when_the_system_cannot_run_the_projector()
    {
        Assert.SkipWhen(OperatingSystem.IsWindows(), "Like a Mac without Rosetta 2: a projector file that isn't executable");
        using var folder = new TemporaryFolder();
        File.WriteAllText(System.IO.Path.Combine(folder.Path, "flashplayer"), "not a program");
        var player = new FlashProjectorPlayer(new LinuxFlashProjector(folder.Path), () => null, NullLogger<FlashProjectorPlayer>.Instance);

        await Assert.ThrowsAsync<FlashPlayerMissingException>(() => player.Play(Windowed(), TestContext.Current.CancellationToken));
    }

    /// @spec play::tears-down-on-exit
    [Fact]
    [SupportedOSPlatform("linux")]
    public async Task Returns_once_the_projector_exits_with_its_output_in_the_log()
    {
        Assert.SkipUnless(OperatingSystem.IsLinux(), "The fake projector is a shell script");
        using var folder = FakeProjector("echo \"Loading $1\"; echo 'Gtk-Message: Failed to load module' >&2");
        var log = new CapturingLogger<FlashProjectorPlayer>();
        var player = new FlashProjectorPlayer(new LinuxFlashProjector(folder.Path), () => null, log);

        await player.Play(Windowed(), TestContext.Current.CancellationToken);

        Assert.Contains(log.Entries, entry => entry.Message.StartsWith("Flash projector ", StringComparison.Ordinal) && entry.Message.EndsWith(" plays Accumulation", StringComparison.Ordinal));
        Assert.Contains(log.Entries, entry => entry.Message.StartsWith("Flash projector: Loading http://127.0.0.1:50317/assets/loader.swf?object_id=swf1234", StringComparison.Ordinal));
        Assert.Contains(log.Entries, entry => entry.Message == "Flash projector: Gtk-Message: Failed to load module");
        Assert.Contains(log.Entries, entry => entry.Message == "Flash projector exited with code 0");
    }

    /// @spec play::closes-on-game-end
    [Fact]
    [SupportedOSPlatform("linux")]
    public async Task Closes_the_projector_and_what_it_started_when_the_launcher_stops_the_game()
    {
        Assert.SkipUnless(OperatingSystem.IsLinux(), "The fake projector is a shell script");
        // Like the projector looking for a browser with ps and grep
        using var folder = FakeProjector("sleep 60 & echo $! > \"$(dirname \"$0\")/pid\"; wait");
        var player = new FlashProjectorPlayer(new LinuxFlashProjector(folder.Path), () => null, NullLogger<FlashProjectorPlayer>.Instance);
        using var launcher = CancellationTokenSource.CreateLinkedTokenSource(TestContext.Current.CancellationToken);

        var playing = player.Play(Windowed(), launcher.Token);
        var pidFile = System.IO.Path.Combine(folder.Path, "pid");
        while (!File.Exists(pidFile) || new FileInfo(pidFile).Length == 0)
            await Task.Delay(20, TestContext.Current.CancellationToken);
        await launcher.CancelAsync();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => playing);
        var childId = int.Parse((await File.ReadAllTextAsync(pidFile, TestContext.Current.CancellationToken)).Trim(), System.Globalization.CultureInfo.InvariantCulture);
        Assert.True(await Disappears(childId), $"Process {childId} survived the game");
    }

    /// <summary>A killed process lingers until its parent, or init, collects its exit status.</summary>
    private static async Task<bool> Disappears(int processId)
    {
        for (var attempt = 0; attempt < 100 && Directory.Exists($"/proc/{processId}"); attempt++)
            await Task.Delay(50, TestContext.Current.CancellationToken);
        return !Directory.Exists($"/proc/{processId}");
    }

    [Fact]
    public void Plays_in_the_projector_shipped_next_to_the_app_for_this_system()
    {
        var projector = FlashProjectorPlayer.NextToApp();

        var start = projector.StartInfo(new Uri("http://127.0.0.1:50317/assets/loader.swf"), fullscreen: false, screen: null);

        Assert.IsType(
            OperatingSystem.IsWindows() ? typeof(WindowsFlashProjector)
            : OperatingSystem.IsMacOS() ? typeof(MacFlashProjector)
            : typeof(LinuxFlashProjector),
            projector);
        Assert.StartsWith(System.IO.Path.Combine(AppContext.BaseDirectory, "flash-player"), start.FileName, StringComparison.Ordinal);
    }

    /// <summary>A folder holding a <c>flashplayer</c> shell script that runs <paramref name="script" />.</summary>
    [SupportedOSPlatform("linux")]
    private static TemporaryFolder FakeProjector(string script)
    {
        var folder = new TemporaryFolder();
        var executable = System.IO.Path.Combine(folder.Path, "flashplayer");
        File.WriteAllText(executable, $"#!/bin/sh\n{script}\n");
        File.SetUnixFileMode(executable, UnixFileMode.UserRead | UnixFileMode.UserWrite | UnixFileMode.UserExecute);
        return folder;
    }

    private sealed class TemporaryFolder : IDisposable
    {
        public string Path { get; } = Directory.CreateTempSubdirectory("eternalfest-desktop-projector-").FullName;

        public void Dispose() => Directory.Delete(Path, recursive: true);
    }

    private static FlashGame Windowed()
    {
        var game = EternalfestJson.ParseGame(PublishedContree.Named("Accumulation").Document());
        return new FlashGame(Origin, game, game.NewRun(new RunChoices("solo", ["mirror"], Locale: "en-US", Volume: 40), DateTimeOffset.UnixEpoch), Fullscreen: false);
    }
}
