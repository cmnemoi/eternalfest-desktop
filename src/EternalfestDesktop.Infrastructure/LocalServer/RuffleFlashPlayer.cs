using System.Diagnostics;
using System.Globalization;
using EternalfestDesktop.Application;
using Microsoft.Extensions.Logging;

namespace EternalfestDesktop.Infrastructure.LocalServer;

/// <summary>Plays the loader in Ruffle desktop, started as a child process (see ADR 0002).</summary>
/// <param name="screen">Where the launcher is, when the game window should fill its height.</param>
public sealed partial class RuffleFlashPlayer(string executable, Func<AvailableScreenArea?> screen, ILogger<RuffleFlashPlayer> logger) : FlashPlayer
{
    /// <summary>The Ruffle shipped next to the app, in <c>ruffle/</c>.</summary>
    public static string NextToApp() =>
        Path.Combine(AppContext.BaseDirectory, "ruffle", OperatingSystem.IsWindows() ? "ruffle.exe" : "ruffle");

    /// @spec play::launches-ruffle
    /// @spec play::tears-down-on-exit
    public async Task Play(FlashGame game, CancellationToken cancellationToken)
    {
        if (!File.Exists(executable))
            throw new FlashPlayerMissingException(executable);

        var start = new ProcessStartInfo(executable)
        {
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
        };
        foreach (var argument in Arguments(game, screen()))
            start.ArgumentList.Add(argument);
        // Ruffle warns a lot about Eternalfest's AVM1 code: keep errors, and the game's own traces
        start.Environment["RUST_LOG"] = "error,avm_trace=info";
        start.Environment["NO_COLOR"] = "1";

        using var ruffle = Process.Start(start) ?? throw new FlashPlayerMissingException(executable);
        LogStarted(ruffle.Id, game.Game.DisplayName.Default);
        ruffle.OutputDataReceived += (_, line) => LogOutput(line.Data);
        ruffle.ErrorDataReceived += (_, line) => LogOutput(line.Data);
        ruffle.BeginOutputReadLine();
        ruffle.BeginErrorReadLine();
        try
        {
            await ruffle.WaitForExitAsync(cancellationToken);
        }
        catch (OperationCanceledException)
        {
            ruffle.Kill(entireProcessTree: true);
            throw;
        }
        LogExited(ruffle.ExitCode);
    }

    /// <summary>Loads the loader the way the Eternalfest website embeds it, from the offline backend.</summary>
    public static IReadOnlyList<string> Arguments(FlashGame game, AvailableScreenArea? screen)
    {
        var origin = game.Origin.GetLeftPart(UriPartial.Authority);
        var loader = $"{origin}/assets/loader.swf";
        List<string> arguments =
        [
            "--base", $"{origin}/",
            "--spoof-url", loader,
            "--referer", $"{origin}/runs/{game.Run.Id}",
            "--dummy-external-interface",
            // @spec play::never-opens-websites
            // At the end of a game, the loader opens /runs/{run id} like on eternalfest.net
            "--open-url-mode", "deny",
            // @spec play::fills-screen-height
            "--no-gui",
        ];
        foreach (var (name, value) in LoaderFlashVars.For(game))
            arguments.AddRange(["-P", $"{name}={value}"]);
        if (game.Fullscreen)
            arguments.Add("--fullscreen");
        else if (screen is not null)
            // Given only the height, in physical pixels, Ruffle keeps the loader's proportions
            arguments.AddRange(["--height", screen.HeightUnderTitleBar.ToString(CultureInfo.InvariantCulture)]);
        arguments.Add(loader);
        return arguments;
    }

    [LoggerMessage(Level = LogLevel.Information, Message = "Ruffle {ProcessId} plays {Contree}")]
    private partial void LogStarted(int processId, string contree);

    private void LogOutput(string? line)
    {
        if (!string.IsNullOrWhiteSpace(line))
            LogRuffleLine(line);
    }

    [LoggerMessage(Level = LogLevel.Information, Message = "Ruffle: {Line}")]
    private partial void LogRuffleLine(string line);

    [LoggerMessage(Level = LogLevel.Information, Message = "Ruffle exited with code {ExitCode}")]
    private partial void LogExited(int exitCode);
}
