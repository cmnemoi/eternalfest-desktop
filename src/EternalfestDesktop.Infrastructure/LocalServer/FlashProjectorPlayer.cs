using System.ComponentModel;
using System.Diagnostics;
using EternalfestDesktop.Application;
using EternalfestDesktop.Infrastructure.FileSystem;
using Microsoft.Extensions.Logging;

namespace EternalfestDesktop.Infrastructure.LocalServer;

/// <summary>Plays the loader in Adobe's Flash projector, started as a child process (see ADR 0008).</summary>
/// <param name="screen">Where the launcher is, when the game window should fill its height.</param>
public sealed partial class FlashProjectorPlayer(FlashProjector projector, Func<AvailableScreenArea?> screen, ILogger<FlashProjectorPlayer> logger) : FlashPlayer
{
    /// <summary>The projector shipped next to the app, in <c>flash-player/</c>, for this OS.</summary>
    public static FlashProjector NextToApp()
    {
        var folder = Path.Combine(AppFiles.Folder, "flash-player");
        return OperatingSystem.IsWindows() ? WindowsFlashProjector.NextTo(folder, BundledFlashFiles.NextToApp())
            : OperatingSystem.IsMacOS() ? new MacFlashProjector(folder)
            : new LinuxFlashProjector(folder);
    }

    /// @spec play::launches-flash-projector
    /// @spec play::tears-down-on-exit
    public async Task Play(FlashGame game, CancellationToken cancellationToken)
    {
        var launcherScreen = screen();
        var start = projector.StartInfo(LoaderUrl(game), game.Fullscreen, launcherScreen);
        if (!File.Exists(start.FileName))
            throw new FlashPlayerMissingException(start.FileName);
        start.UseShellExecute = false;
        start.RedirectStandardOutput = true;
        start.RedirectStandardError = true;

        using var running = Started(start);
        LogStarted(running.Id, game.Game.DisplayName.Default);
        running.OutputDataReceived += (_, line) => LogOutput(line.Data);
        running.ErrorDataReceived += (_, line) => LogOutput(line.Data);
        running.BeginOutputReadLine();
        running.BeginErrorReadLine();
        try
        {
            await Task.WhenAll(
                projector.ShapeWindow(running, game.Fullscreen, launcherScreen, cancellationToken),
                running.WaitForExitAsync(cancellationToken));
        }
        catch (OperationCanceledException)
        {
            running.Kill(entireProcessTree: true);
            throw;
        }
        LogExited(running.ExitCode);
    }

    /// @spec play::flash-projector-first
    private Process Started(ProcessStartInfo start)
    {
        try
        {
            return Process.Start(start) ?? throw new FlashPlayerMissingException(start.FileName);
        }
        catch (Win32Exception unrunnable)
        {
            // Like the Intel projector on a Mac without Rosetta 2
            LogUnrunnable(unrunnable.Message);
            throw new FlashPlayerMissingException(start.FileName);
        }
    }

    [LoggerMessage(Level = LogLevel.Warning, Message = "The Flash projector can't run on this system: {Reason}")]
    private partial void LogUnrunnable(string reason);

    /// <summary>The loader on the offline backend, its FlashVars in the query string like an embed's.</summary>
    public static Uri LoaderUrl(FlashGame game)
    {
        var query = string.Join('&', LoaderFlashVars.For(game).Select(flashVar => $"{flashVar.Key}={Uri.EscapeDataString(flashVar.Value)}"));
        return new Uri($"{game.Origin.GetLeftPart(UriPartial.Authority)}/assets/loader.swf?{query}");
    }

    [LoggerMessage(Level = LogLevel.Information, Message = "Flash projector {ProcessId} plays {Contree}")]
    private partial void LogStarted(int processId, string contree);

    private void LogOutput(string? line)
    {
        if (!string.IsNullOrWhiteSpace(line))
            LogProjectorLine(line);
    }

    [LoggerMessage(Level = LogLevel.Information, Message = "Flash projector: {Line}")]
    private partial void LogProjectorLine(string line);

    [LoggerMessage(Level = LogLevel.Information, Message = "Flash projector exited with code {ExitCode}")]
    private partial void LogExited(int exitCode);
}
