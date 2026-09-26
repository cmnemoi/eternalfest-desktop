using System.Text;
using Microsoft.Extensions.Logging;

namespace EternalfestDesktop.Infrastructure.FileSystem;

/// <summary>
/// Adds the app to the Linux applications menu: a desktop entry that runs <paramref name="app" /> from where it was
/// extracted, and the <c>icon.png</c> next to it in the user's icon theme (freedesktop.org desktop entry specification).
/// </summary>
/// <param name="dataHome"><c>$XDG_DATA_HOME</c>, usually <c>~/.local/share</c>.</param>
/// <param name="app">The app's executable.</param>
/// <param name="appImage">The AppImage the app runs from, if it does: the entry runs it, since the app itself lives in a temporary folder.</param>
/// @spec packaging::linux-desktop-entry
public sealed partial class XdgDesktopEntry(string dataHome, string app, ILogger<XdgDesktopEntry> logger, string? appImage = null)
{
    /// <summary>The name of the entry, of its icon, and the class of the app's windows, which docks match to the entry.</summary>
    public const string WindowClass = "eternalfest-desktop";

    public static XdgDesktopEntry ForThisApp(ILogger<XdgDesktopEntry> logger) => new(
        Environment.GetEnvironmentVariable("XDG_DATA_HOME") is { Length: > 0 } dataHome
            ? dataHome
            : Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".local", "share"),
        Environment.ProcessPath ?? throw new InvalidOperationException("The app's executable is unknown."),
        logger,
        // @spec packaging::linux-appimage
        Environment.GetEnvironmentVariable("APPIMAGE") is { Length: > 0 } appImage ? appImage : null);

    private string Launched => appImage ?? app;

    /// <summary>Writes the entry and its icon only when they changed. Never throws: the app starts without them.</summary>
    public void Register()
    {
        try
        {
            WriteIfChanged(Path.Combine(dataHome, "icons", "hicolor", "256x256", "apps", $"{WindowClass}.png"), File.ReadAllBytes(Path.Combine(Path.GetDirectoryName(app)!, "icon.png")));
            WriteIfChanged(Path.Combine(dataHome, "applications", $"{WindowClass}.desktop"), Encoding.UTF8.GetBytes(Entry()));
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            LogNotRegistered(exception);
        }
    }

    private string Entry() => $"""
        [Desktop Entry]
        Type=Application
        Name=Eternalfest Desktop
        Comment=Play Eternalfest contrées offline (unofficial)
        Comment[fr]=Jouer aux contrées Eternalfest hors ligne (non officiel)
        Exec={Quoted(Launched)}
        TryExec={Escaped(Launched)}
        Icon={WindowClass}
        Terminal=false
        Categories=Game;
        StartupWMClass={WindowClass}

        """;

    private void WriteIfChanged(string path, byte[] content)
    {
        if (File.Exists(path) && File.ReadAllBytes(path).AsSpan().SequenceEqual(content))
            return;
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        File.WriteAllBytes(path, content);
        LogWritten(path);
    }

    /// <summary>An <c>Exec</c> argument: quoted, since the desktop would otherwise split, expand or substitute parts of it.</summary>
    private static string Quoted(string argument)
    {
        var quoted = new StringBuilder("\"");
        foreach (var character in argument)
            quoted.Append(character switch
            {
                '"' or '`' or '$' or '\\' => $"\\{character}",
                '%' => "%%",
                _ => character.ToString(),
            });
        return Escaped(quoted.Append('"').ToString());
    }

    /// <summary>A string value, where a backslash starts an escape sequence.</summary>
    private static string Escaped(string value) => value.Replace("\\", "\\\\", StringComparison.Ordinal);

    [LoggerMessage(Level = LogLevel.Information, Message = "Updated {Path} for the applications menu")]
    private partial void LogWritten(string path);

    [LoggerMessage(Level = LogLevel.Warning, Message = "Can't add the app to the applications menu")]
    private partial void LogNotRegistered(Exception exception);
}
